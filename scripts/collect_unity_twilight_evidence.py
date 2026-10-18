#!/usr/bin/env python3
"""Integrity of existing local engine evidence. Does not execute or approve rendering."""
import argparse
import csv
import json
import re
import struct
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
import sys
sys.path.insert(0, str(ROOT / 'Tools/Pipeline'))
from art_contract import sha, inventory


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, required=True)
    parser.add_argument('--capture', type=Path, required=True)
    parser.add_argument('--motion', type=Path)
    parser.add_argument('--revision', default='v006')
    parser.add_argument('--bear-revision', default='twilight_v005')
    parser.add_argument('--room-revision', default='twilight_v004')
    parser.add_argument('--edit-only', action='store_true')
    args = parser.parse_args()
    args.project = args.project.resolve()
    args.capture = args.capture.resolve()
    assert re.fullmatch(r'v[0-9]{3}', args.revision)
    assert all(re.fullmatch(r'twilight_v[0-9]{3}', value) for value in (args.bear_revision, args.room_revision))
    args.motion = args.motion.resolve() if args.motion else None
    assert args.edit_only or args.motion, 'Supply motion evidence or explicitly select edit-only scope.'
    generated = args.project / f'Assets/Sorajiro/Generated/Twilight_{args.revision}'
    receipt = json.loads((generated / 'generation-receipt.json').read_text())
    expected = dict(zip(receipt['files'], receipt['hashes'], strict=True))
    actual = {p.relative_to(generated).as_posix(): sha(p) for p in generated.rglob('*')
              if p.is_file() and p.name not in ('generation-receipt.json', 'generation-receipt.json.meta')}
    assert expected == actual, 'Generated content modified; preserve edits.'
    art = {}
    for kind, revision in [('Bear', args.bear_revision), ('Room', args.room_revision)]:
        folder = ROOT / 'Artifacts/SourceArt' / kind / revision
        data = json.loads((folder / 'receipt.json').read_text())
        assert data['files'] == inventory(folder)
        assert all(p.read_bytes().startswith(b'Kaydara FBX Binary') for p in folder.glob('*.fbx'))
        art[kind] = {'receipt_sha256': sha(folder / 'receipt.json'), 'observations': data['observations']}
    images = []
    for p in sorted(args.capture.glob('*.png')):
        data = p.read_bytes()
        assert data[:8] == b'\x89PNG\r\n\x1a\n' and struct.unpack('>II', data[16:24]) == (1920, 1080)
        metadata = json.loads(p.with_suffix('.json').read_text())
        assert metadata['unity'] == '6000.6.5f1' and metadata['api'] == 'Metal'
        if args.revision>='v010':
            assert metadata['scene']==f'Assets/Sorajiro/Generated/Twilight_{args.revision}/L_LookDev.unity', 'Capture belongs to a different revision'
        assert not metadata['dynamicResolution'] and not metadata['dlss'] and not metadata['fsr2']
        images.append({'path': p.relative_to(ROOT).as_posix(), 'sha256': sha(p), 'metadata': metadata})
    assert len(images) == 8 and (args.capture / 'complete.txt').is_file()
    editor = ROOT / 'Artifacts/UnityEditor'
    names=['twilight-skin-test.txt', 'twilight-animation-clips.txt']
    if args.revision>='v007':names+=['twilight-sole-test.txt','twilight-surfaces-test.txt']
    if not args.edit_only:names+=['twilight-animation-runtime.txt','twilight-motor-smoke.txt']
    tests={name:(editor / name).read_text() for name in names}
    assert all('PASS:' in value for value in tests.values())
    assert all((editor / name).stat().st_mtime >= (generated / 'generation-receipt.json').stat().st_mtime
               for name in tests), 'Test logs predate the current generated revision.'
    runtime=None
    if not args.edit_only:
        runtime=json.loads(sorted(editor.glob('runtime-*.json'))[-1].read_text())
        assert runtime['scenePath'].endswith(f'Twilight_{args.revision}/L_LookDev.unity')
    assemblies = {}
    for scope in ('Runtime', 'Editor'):
        latest = max(p.stat().st_mtime for p in (ROOT / 'Unity/Packages/com.sorajiro.cozywalk' / scope).rglob('*.cs'))
        assembly = args.project / 'Library/ScriptAssemblies' / f'Sorajiro.{scope}.dll'
        assert assembly.stat().st_mtime >= latest, f'Stale {scope} assembly'
        assemblies[assembly.name] = sha(assembly)
    motion=None
    if args.motion:
        assert (args.motion / 'complete.txt').is_file() and (args.motion / 'twilight-motion.mp4').is_file()
        rows = list(csv.DictReader((args.motion / 'frames.csv').open()))
        times = [float(row['time_s']) for row in rows]
        assert times[-1] > 29.5 and all(b > a for a, b in zip(times, times[1:]))
        assert any(row['grounded'] == 'False' for row in rows)
        assert any(float(row['planar_mps']) > .4 for row in rows)
        assert len({row['animator_state_hash'] for row in rows}) >= 4
        assert all((args.motion / f"{int(row['frame']):05d}.jpg").is_file() for row in rows)
        motion={'folder':args.motion.relative_to(ROOT).as_posix(), 'frames':len(rows),
                'last_timestamp_s':times[-1], 'video_sha256':sha(args.motion / 'twilight-motion.mp4'),
                'scope':'actual Play, scripted inputs, timestamped variable sampling; not a performance test'}
    result = {'scope': __doc__, 'collected_at_utc': datetime.now(timezone.utc).isoformat(),
              'scene': f'Assets/Sorajiro/Generated/Twilight_{args.revision}/L_LookDev.unity',
              'generated_receipt_sha256': sha(generated / 'generation-receipt.json'),
              'art': art, 'images': images, 'tests': tests, 'runtime': runtime, 'assemblies': assemblies,
              'motion': motion, 'live_execution_checked': not args.edit_only,
              'scripted_focus_fixture': (editor/'twilight-scripted-focus.txt').read_text() if not args.edit_only and args.revision>='v010' else None,
              'source_checks': json.loads((ROOT / 'validation/unity_source_checks.json').read_text()),
              'not_verified': (['current live Animator/physics integration', 'current Play motion'] if args.edit_only else []) + ['owner visual approval', 'foot sliding and all motion artifacts',
                               'runtime bedding deformation', 'LOD and quality workload controls',
                               'Player build', 'Windows', 'performance and memory pressure']}
    destination = ROOT / 'validation' / ('unity_twilight_checks.json' if args.revision=='v006' else f'unity_twilight_{args.revision}_checks.json')
    destination.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')
    print(f'PASS: receipt integrity, 8 PNG headers, recorded engine tests and {motion['frames'] if motion else 0} motion frames (edit-only={args.edit_only}); {destination}')


if __name__ == '__main__':
    main()
