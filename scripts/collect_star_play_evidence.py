#!/usr/bin/env python3
"""Collect real Star Play evidence; no audio hardware, visual approval or FPS claim."""
import argparse
import json
import math
import struct
import wave
from pathlib import Path
from collect_bedside_evidence import ROOT, receipt, sha


def audio(path):
    with wave.open(str(path), 'rb') as wav:
        assert wav.getsampwidth() == 2 and wav.getnchannels() in (1, 2)
        channels, rate, count = wav.getnchannels(), wav.getframerate(), wav.getnframes()
        assert rate == 48000 and count > 1000
        data = wav.readframes(count)
    samples = struct.unpack('<' + 'h' * (len(data) // 2), data)
    peak = max(abs(v) for v in samples) / 32768
    rms = math.sqrt(sum(v*v for v in samples) / len(samples)) / 32768
    assert .0001 < rms < .5 and peak < 1
    return {'sha256': sha(path), 'channels': channels, 'rate': rate, 'seconds': count/rate, 'peak': peak, 'rms': rms}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, required=True)
    parser.add_argument('--capture', type=Path, required=True)
    args = parser.parse_args()
    project, capture = args.project.resolve(strict=True), args.capture.resolve(strict=True)
    overlay = 'Assets/Sorajiro/Generated/StarPlay_v003/L_LookDev.unity'
    room = 'Assets/Sorajiro/Generated/BedsideBear_v002/L_LookDev.unity'
    assert 'Overlay='+overlay in (capture/'complete.txt').read_text()
    checks = json.loads((capture/'checks.json').read_text())
    assert checks['state'] == 'completed' and checks['assertions'] >= 28
    assert checks['collected'] == checks['chimes'] == 3 and checks['walkSteps'] > 0 and checks['landings'] > 0
    assert checks['gpu'] == 'Apple M3 Max' and checks['api'] == 'Metal' and checks['lightmaps'] == 3
    receipts = {name: receipt(project/'Assets/Sorajiro/Generated'/name) for name in ('StarPlay_v003','BedsideBear_v002','BearFit_v001','Bedside_v009','Fleece_v005')}
    frames = {}
    for name, count in (('Before',0),('Contact_Burst',1),('After',3)):
        p = capture/(name+'.png'); data = p.read_bytes()
        assert data[:8] == b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',data[16:24]) == (1920,1080)
        m = json.loads(p.with_suffix('.json').read_text())
        assert m['overlay'] == overlay and m['room'] == room and m['collected'] == count
        assert m['api'] == 'Metal' and m['model'] == 'Preferred_BearFit_v001_3'
        assert m['sparks'] == (1 if name == 'Contact_Burst' else 0)
        frames[name] = {'path':str(p.relative_to(ROOT)), 'sha256':sha(p), 'metadata':m}
    base = frames['Before']['metadata']
    assert all(all(frame['metadata'][key] == base[key] for key in ('position','rotation','fov')) for frame in frames.values())
    sounds = {name: audio(capture/name) for name in ('Actual_Foot_DSP.wav','Actual_Chime_DSP.wav')}
    source = json.loads((ROOT/'validation/unity_source_checks.json').read_text())
    assert all(check['exit_code'] == 0 for check in source['checks'])
    restart = json.loads((ROOT/'Artifacts/UnityEditor/star-play-restart.json').read_text())['data']['result']['result']
    assert restart['ready'] and restart['collected'] == restart['chimes'] == restart['footsteps'] == 0 and restart['total'] == 3
    result = {'restart_state':restart, 'scope':__doc__, 'overlay':overlay, 'room':room, 'asset_receipts_sha256':receipts,
              'play_checks':checks, 'frames':frames, 'source_output_DSP':sounds,
              'source_checks':source, 'not_verified':['hardware listening and speaker/headphone balance','owner approval','Player build','Windows','FPS and memory pressure','keyboard/focus acceptance','collection save across app restarts','eight-star exploration layout']}
    out = ROOT/'validation/unity_star_play_v003_checks.json'
    out.write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n')
    print('PASS: protected inputs + generated overlay; real trigger gameplay, 3 Metal event frames, source DSP WAVs;',out)


if __name__ == '__main__':
    main()
