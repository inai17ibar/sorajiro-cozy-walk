#!/usr/bin/env python3
"""Collect real camera/audio Play evidence; no listening, owner approval or FPS claim."""
import argparse
import csv
import json
import re
import struct
from pathlib import Path
from collect_bedside_evidence import ROOT, receipt, sha
from collect_star_play_evidence import audio


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('project', 'capture', 'motion'):
        parser.add_argument('--'+name, type=Path, required=True)
    args = parser.parse_args()
    project, capture, motion = (getattr(args, key).resolve(strict=True) for key in ('project', 'capture', 'motion'))
    overlay = 'Assets/Sorajiro/Generated/ComfortPlay_v002/L_LookDev.unity'
    assert 'Overlay='+overlay in (capture/'complete.txt').read_text()
    checks = json.loads((capture/'checks.json').read_text())
    assert checks['state'] == 'completed' and checks['assertions'] == 28
    assert checks['gpu'] == 'Apple M3 Max' and checks['api'] == 'Metal'
    assert checks['jumps'] == 1 and checks['feet'] > 0 and checks['landings'] == 1 and checks['collected'] >= 1
    receipts = {name: receipt(project/'Assets/Sorajiro/Generated'/name) for name in (
        'ComfortPlay_v002', 'FleeceLighting_v002', 'StarPlay_v003', 'BedsideBear_v002', 'BearFit_v001', 'Fleece_v005', 'Bedside_v009')}
    frames = {}
    for name in ('Before_Low_Camera', 'Overhead_Default', 'Turn_Right', 'Behind_Bear'):
        p = capture/(name+'.png'); data = p.read_bytes()
        assert data[:8] == b'\x89PNG\r\n\x1a\n' and struct.unpack('>II', data[16:24]) == (1920, 1080)
        m = json.loads(p.with_suffix('.json').read_text())
        assert m['overlay'] == overlay and m['api'] == 'Metal' and m['gpu'] == 'Apple M3 Max'
        assert m['model'] == 'Preferred_BearFit_v001_3'
        assert m['requestedPitch'] == (3 if name == 'Before_Low_Camera' else 26)
        frames[name] = {'path': str(p.relative_to(ROOT)), 'sha256': sha(p), 'metadata': m}
    a, b = (frames[name]['metadata'] for name in ('Overhead_Default', 'Turn_Right'))
    assert abs((b['requestedYaw']-a['requestedYaw']) % 360-45) < .01
    assert a['position']['y'] > .6 and a['position'] != b['position']
    sounds = {name: audio(capture/name) for name in ('Cute_Foot_And_Land_DSP.wav', 'Cute_Jump_DSP.wav')}
    rows = list(csv.DictReader((motion/'frames.csv').open()))
    times = [float(r['time_s']) for r in rows]
    assert (motion/'complete.txt').exists() and times[-1] > 29.5 and all(b > a for a, b in zip(times, times[1:]))
    assert all(r['comfort_camera'] == 'True' and r['visual_model'] == 'Preferred_BearFit_v001_3' for r in rows)
    assert all(abs(float(r['camera_pitch'])-26) < .01 for r in rows)
    assert len({r['camera_x'] for r in rows}) > 50 and int(rows[-1]['comfort_jump_sounds']) == 2
    assert any(r['grounded'] == 'False' for r in rows) and any(float(r['planar_mps']) > .4 for r in rows)
    assert (motion/'twilight-motion.mp4').exists()
    inputs = {}
    for key, angle in (('e', 29), ('q', 344), ('c', 180)):
        p = ROOT/f'Artifacts/UnityEditor/comfort-key-{key}.json'
        result = json.loads(p.read_text())['data']['result']['result']
        assert 'ready = True' in result and float(re.search(r'yaw = ([0-9.]+)', result)[1]) == angle
        inputs[key] = {'path': str(p.relative_to(ROOT)), 'sha256': sha(p), 'actual_status': result}
    source = json.loads((ROOT/'validation/unity_source_checks.json').read_text())
    assert all(c['exit_code'] == 0 for c in source['checks'])
    result = {'scope': __doc__, 'overlay': overlay, 'asset_receipts_sha256': receipts, 'play_checks': checks,
              'frames': frames, 'source_output_DSP': sounds, 'actual_key_input': inputs,
              'motion': {'path': str(motion.relative_to(ROOT)), 'frames': len(rows), 'last_timestamp_s': times[-1],
                         'jump_sounds': 2, 'sha256': sha(motion/'twilight-motion.mp4')}, 'source_checks': source,
              'not_verified': ['speaker/headphone listening and balance', 'owner visual/feel approval', 'Player build',
                               'Windows', 'FPS/GPU time/memory pressure', 'physical trackpad/mouse acceptance',
                               'every furniture occlusion case', 'complete game milestones']}
    out = ROOT/'validation/unity_comfort_play_v002_checks.json'
    out.write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n')
    print('PASS: protected assets, 28 real Play assertions, Metal frames, source DSP, Q/E/C keys, 30s motion;', out)


if __name__ == '__main__':
    main()
