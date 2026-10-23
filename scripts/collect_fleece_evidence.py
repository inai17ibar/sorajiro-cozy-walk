#!/usr/bin/env python3
"""Validate existing local fleece trial evidence; does not execute or approve rendering."""
import argparse
import csv
import json
import struct
from datetime import datetime, timezone
from pathlib import Path

from collect_bedside_evidence import ROOT, difference, receipt, sha


def captures(folder, scene, expected, room):
    assert (folder / 'complete.txt').exists(), 'Incomplete capture'
    images = {}
    for p in folder.glob('*.png'):
        data = p.read_bytes()
        assert data[:8] == b'\x89PNG\r\n\x1a\n'
        assert struct.unpack('>II', data[16:24]) == (1920, 1080)
        m = json.loads(p.with_suffix('.json').read_text())
        assert m['scene'] == scene and m['api'] == 'Metal' and m['gpu'] == 'Apple M3 Max'
        assert m['roomPreview'] == room and not m['dynamicResolution']
        images[p.stem] = {'path': str(p.relative_to(ROOT)), 'sha256': sha(p), 'metadata': m}
    assert set(images) == set(expected), 'Wrong capture inventory'
    return images


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, required=True)
    parser.add_argument('--studio', type=Path, required=True)
    parser.add_argument('--room', type=Path, required=True)
    parser.add_argument('--motion', type=Path, required=True)
    parser.add_argument('--revision', default='v005', choices=['v005'])
    args = parser.parse_args()
    project, studio, room, motion = (getattr(args, n).resolve(strict=True) for n in ('project', 'studio', 'room', 'motion'))
    scene = f'Assets/Sorajiro/Generated/Fleece_{args.revision}/L_LookDev.unity'
    room_scene = 'Assets/Sorajiro/Generated/Bedside_v009/L_LookDev.unity'
    generated = project / f'Assets/Sorajiro/Generated/Fleece_{args.revision}'
    receipts = {name: receipt(project / 'Assets/Sorajiro/Generated' / name)
                for name in ('Twilight_v010', 'Bedside_v009', f'Fleece_{args.revision}')}
    observations = json.loads((generated / 'fiber-observations.json').read_text())
    assert observations['currentFibers'] == observations['originalFibers'] * 2
    assert observations['vertices'] == observations['currentFibers'] * 6
    studio_names = [f'{camera}_{variant}' for camera in ('F01_Portrait', 'F02_FullBody', 'F03_Surface')
                    for variant in ('Baseline', 'Fabric', 'Groomed')]
    studio_names += ['F04_Patch_Patch_FabricMesh', 'F04_Patch_Patch_HairRibbons']
    room_names = [f'{camera}_{variant}' for camera in ('S01_Portrait', 'S03_ThreeQuarter')
                  for variant in ('Baseline', 'Groomed')]
    images = captures(studio, scene, studio_names, False)
    room_images = captures(room, room_scene, room_names, True)
    deltas = {}
    for prefix, folder, collection, pairs in (
        ('studio', studio, images, [(f'{c}_Baseline', f'{c}_Fabric') for c in ('F01_Portrait', 'F03_Surface')]
         + [(f'{c}_Fabric', f'{c}_Groomed') for c in ('F01_Portrait', 'F03_Surface')]
         + [('F04_Patch_Patch_FabricMesh', 'F04_Patch_Patch_HairRibbons')]),
        ('room', room, room_images, [(f'{c}_Baseline', f'{c}_Groomed') for c in ('S01_Portrait', 'S03_ThreeQuarter')])
    ):
        for first, second in pairs:
            a, b = collection[first]['metadata'], collection[second]['metadata']
            assert all(a[k] == b[k] for k in ('position', 'rotation', 'fov', 'width', 'height'))
            delta = difference(folder / (first + '.png'), folder / (second + '.png'))
            assert delta > .001, f'No visible switch effect: {first}/{second}'
            deltas[f'{prefix}:{first}->{second}'] = delta
    for collection in (images, room_images):
        for name, entry in collection.items():
            m = entry['metadata']
            if name.endswith('_Groomed'):
                assert m['pileVertices'] == observations['vertices']
                assert 'Cotton' in m['pileShader']
    patch_a = images['F04_Patch_Patch_FabricMesh']['metadata']['patchShader']
    patch_b = images['F04_Patch_Patch_HairRibbons']['metadata']['patchShader']
    assert patch_a != patch_b and 'Hair' in patch_b
    assert 'Scene=' + room_scene in (motion / 'complete.txt').read_text()
    assert (motion / 'twilight-motion.mp4').exists()
    rows = list(csv.DictReader((motion / 'frames.csv').open()))
    times = [float(r['time_s']) for r in rows]
    assert len(rows) > 20 and times[-1] > 29.5 and all(b > a for a, b in zip(times, times[1:]))
    assert all(r['fleece_variant'] == '2' and int(r['pile_vertices']) == observations['vertices'] for r in rows)
    assert any(r['grounded'] == 'False' for r in rows) and any(float(r['planar_mps']) > .4 for r in rows)
    assert len({r['animator_state_hash'] for r in rows}) >= 4
    tests = {}
    for name in ('fleece-checks.txt', 'fleece-motor-smoke.txt', 'fleece-animation-runtime.txt', 'fleece-variant-runtime.txt'):
        p = ROOT / 'Artifacts/UnityEditor' / name
        value = p.read_text()
        assert 'PASS:' in value
        if name != 'fleece-animation-runtime.txt': assert scene in value
        assert p.stat().st_mtime >= (generated / 'generation-receipt.json').stat().st_mtime
        tests[name] = value
    jobs = [json.loads(p.read_text()) for p in (ROOT / 'Artifacts/UnityEditor/PipelineJobs').glob('*_fleece-runtime.json')]
    assert any(j['scene'] == scene and j['state'] == 'completed' for j in jobs)
    result = {
        'scope': __doc__, 'collected_at_utc': datetime.now(timezone.utc).isoformat(),
        'scene': scene, 'room_scene': room_scene, 'receipts_sha256': receipts,
        'observations': observations, 'studio': images, 'room': room_images,
        'measured_luma_differences_8bit': deltas,
        'comparison_scope': 'Only within each lighting context. Signal differences prove switch effect, not quality or FPS. Hair patch is a mesh-shading experiment, not simulated strands.',
        'runtime_tests': tests, 'motion': {'path': str(motion.relative_to(ROOT)), 'frames': len(rows),
            'last_timestamp_s': times[-1], 'sha256': sha(motion / 'twilight-motion.mp4'),
            'fleece_variant': 2, 'pile_vertices_every_sample': observations['vertices']},
        'source_checks': json.loads((ROOT / 'validation/unity_source_checks.json').read_text()),
        'not_verified': ['reference quality / owner approval', 'Player build', 'FPS/GPU time/memory pressure',
                         'Windows', 'real keyboard acceptance', 'LOD/quality workload controls', 'strand simulation'],
    }
    output = ROOT / f'validation/unity_fleece_{args.revision}_checks.json'
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')
    print('PASS: protected assets, 11 studio + 4 room Metal frames, material/mesh effects, runtime tests, 30s Play with groom active;', output)


if __name__ == '__main__':
    main()
