#!/usr/bin/env python3
"""Collect existing local evidence; never launches Unity or approves visual quality."""
import argparse
import hashlib
import json
import struct
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', required=True, type=Path)
    parser.add_argument('--capture', required=True, type=Path)
    args = parser.parse_args()
    generated = args.project / 'Assets/Sorajiro/Generated/M2_v003'
    receipt = json.loads((generated / 'generation-receipt.json').read_text())
    expected = dict(zip(receipt['files'], receipt['hashes'], strict=True))
    actual = {str(p.relative_to(generated)): sha(p) for p in generated.rglob('*')
              if p.is_file() and p.name not in ('generation-receipt.json', 'generation-receipt.json.meta')}
    assert expected == actual, 'Generated content differs; do not call unchanged/reusable.'
    source = ROOT / 'Artifacts/SourceArt/Bear/m2_v002'
    art = json.loads((source / 'receipt.json').read_text())
    assert all(sha(source / name) == digest for name, digest in art['files'].items())
    assert (source / 'Sorajiro_Bear_M2.fbx').read_bytes().startswith(b'Kaydara FBX Binary')
    # Blender 5 may use a compressed native blend header; recipe plus real Blender save is the provenance.
    images = []
    for path in sorted(args.capture.glob('*.png')):
        data = path.read_bytes()
        assert data[:8] == b'\x89PNG\r\n\x1a\n'
        size = struct.unpack('>II', data[16:24])
        assert size == (1920, 1080), (path, size)
        images.append({'path': str(path.relative_to(ROOT)), 'width': size[0], 'height': size[1], 'sha256': sha(path),
                       'metadata': json.loads(path.with_suffix('.json').read_text())})
    assert len(images) == 7 and (args.capture / 'complete.txt').exists()
    editor = ROOT / 'Artifacts/UnityEditor'
    skin = (editor / 'm2-skin-test.txt').read_text()
    motor = (editor / 'm2-motor-smoke.txt').read_text()
    guard = (editor / 'guard-tests.txt').read_text()
    assert all('PASS:' in text for text in (skin, motor, guard))
    runtime = json.loads(sorted(editor.glob('runtime-*.json'))[-1].read_text())
    assert runtime['scenePath'].endswith('M2_v003/L_LookDev.unity')
    assemblies = {}
    for scope in ('Runtime', 'Editor'):
        source_folder = ROOT / 'Unity/Packages/com.sorajiro.cozywalk' / scope
        latest_source = max(p.stat().st_mtime for p in source_folder.rglob('*.cs'))
        assembly = args.project / 'Library/ScriptAssemblies' / f'Sorajiro.{scope}.dll'
        assert assembly.stat().st_mtime >= latest_source, f'Stale {scope} assembly'
        assemblies[assembly.name] = {'sha256': sha(assembly), 'compiled_after_corresponding_sources': True,
                                     'mtime_unix': assembly.stat().st_mtime}
    result = {'scope': 'existing local evidence integrity; not a fresh engine execution or quality/performance approval',
              'collected_at_utc': datetime.now(timezone.utc).isoformat(), 'scene': str(generated),
              'asset_receipt_sha256': sha(generated / 'generation-receipt.json'),
              'art_observations': art['observations'], 'images': images,
              'skin_test': skin, 'motor_smoke': motor, 'guard_tests': guard, 'runtime': runtime,
              'editor_assemblies': assemblies,
              'source_checks': json.loads((ROOT / 'validation/unity_source_checks.json').read_text()),
              'not_verified': ['full-body fur', 'walk animation', 'motion clips', 'reference quality approval',
                               'Player build', 'Windows', 'performance and memory pressure']}
    destination = ROOT / 'validation/unity_m2_checks.json'
    destination.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')
    print(f'PASS: receipts and {len(images)} actual 1920x1080 PNG headers; {destination}')


if __name__ == '__main__':
    main()
