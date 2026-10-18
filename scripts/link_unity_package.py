#!/usr/bin/env python3
"""Dry-run by default; add ONLY the local Sorajiro dependency to an existing Unity project."""
import argparse
import hashlib
import json
import os
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / 'Unity/Packages/com.sorajiro.cozywalk'
NAME = 'com.sorajiro.cozywalk'

def planned_manifest(manifest, location):
    result = json.loads(json.dumps(manifest))
    dependencies = result['dependencies']
    for name, expected in [('com.unity.render-pipelines.high-definition', '17.6.0'),
                           ('com.unity.inputsystem', '1.20.0')]:
        if dependencies.get(name) != expected:
            raise ValueError(f'Review required: expected {name} {expected}')
    value = 'file:' + location.as_posix()
    if NAME in dependencies and dependencies[NAME] != value:
        raise ValueError('A different Sorajiro dependency already exists; preserving it.')
    dependencies[NAME] = value
    return result

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, required=True)
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    project = args.project.resolve(strict=True)
    version = (project / 'ProjectSettings/ProjectVersion.txt').read_text()
    if 'm_EditorVersion: 6000.6.5f1\n' not in version:
        raise ValueError('Review required: expected Unity 6000.6.5f1')
    path = project / 'Packages/manifest.json'
    if path.is_symlink() or path.parent.is_symlink():
        raise ValueError('Symlink manifest destination refused')
    original = path.read_bytes()
    planned = planned_manifest(json.loads(original), PACKAGE.resolve(strict=True))
    updated = (json.dumps(planned, ensure_ascii=False, indent=2) + '\n').encode()
    if json.loads(original) == planned:
        print('Already linked; no writes.')
        return
    print(f'Add dependency: {NAME} = {planned["dependencies"][NAME]}')
    print('Existing dependencies, scenes, assets, build settings and project settings are preserved.')
    if not args.apply:
        print('DRY-RUN only. Use --apply for the above manifest change.')
        return
    backup = ROOT / 'Artifacts/UnityLink' / (hashlib.sha256(original).hexdigest() + '.manifest.json')
    backup.parent.mkdir(parents=True, exist_ok=True)
    if not backup.exists(): backup.write_bytes(original)
    if path.read_bytes() != original: raise RuntimeError('Manifest changed during preflight; stopping.')
    fd, temporary = tempfile.mkstemp(prefix='.sorajiro-manifest-', dir=path.parent)
    try:
        with os.fdopen(fd, 'wb') as stream:
            stream.write(updated)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary): os.unlink(temporary)
    print('Linked. Backup:', backup)

if __name__ == '__main__':
    main()
