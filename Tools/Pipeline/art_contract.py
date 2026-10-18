"""Shared production protection for local DCC outputs; no Blender dependency."""
import hashlib
import json
from pathlib import Path

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def inventory(folder):
    folder = Path(folder)
    if not folder.exists(): return {}
    for p in [folder, *folder.rglob('*')]:
        if p.is_symlink(): raise ValueError('Symlink output refused')
    return {p.relative_to(folder).as_posix(): sha(p) for p in sorted(folder.rglob('*'))
            if p.is_file() and p.name != 'receipt.json'}

def preflight(folder, recipe):
    folder = Path(folder)
    for p in [folder, *folder.parents]:
        if p.is_symlink(): raise ValueError('Symlink output ancestor refused')
    contents = inventory(folder)
    receipt = folder / 'receipt.json'
    if not receipt.exists():
        if contents: raise ValueError('Unowned or partial output preserved; use a new revision')
        return False
    data = json.loads(receipt.read_text())
    if data.get('recipe') != recipe or data.get('files') != contents or not contents:
        raise ValueError('Owner edit/source change detected; no overwrite; use a new revision')
    return True

def finish(folder, recipe, observations):
    folder = Path(folder)
    path = folder / 'receipt.json'
    with path.open('x') as stream:
        json.dump({'recipe': recipe, 'files': inventory(folder), 'observations': observations}, stream, indent=2)
        stream.write('\n')
