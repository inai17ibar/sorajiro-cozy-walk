#!/usr/bin/env python3
"""Check existing local bear shape/light evidence; not an artistic or performance approval."""
import argparse
import csv
import json
import struct
from datetime import datetime, timezone
from pathlib import Path

from collect_bedside_evidence import ROOT, difference, receipt, sha


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project',type=Path,required=True)
    parser.add_argument('--capture',type=Path,required=True)
    parser.add_argument('--motion',type=Path,required=True)
    args=parser.parse_args()
    project,capture,motion=(getattr(args,n).resolve(strict=True) for n in ('project','capture','motion'))
    scene='Assets/Sorajiro/Generated/BearFit_v002/L_LookDev.unity'
    generated=project/'Assets/Sorajiro/Generated/BearFit_v002'
    receipts={n:receipt(project/'Assets/Sorajiro/Generated'/n) for n in ('Twilight_v010','Fleece_v005','Bedside_v009','BearFit_v002')}
    art=ROOT/'Artifacts/SourceArt/Bear/fit_v001'
    source=json.loads((art/'receipt.json').read_text())
    assert all(sha(art/n)==digest for n,digest in source['files'].items())
    assert sha(generated/'Bear.fbx')==sha(art/'Sorajiro_Bear_Fit.fbx')
    observation=source['observations']
    before=observation['foot_centers_before_m'];after=observation['foot_centers_after_m']
    assert abs((after['foot_R'][0]-after['foot_L'][0])-.1)<1e-6
    assert abs((before['foot_R'][0]-before['foot_L'][0])-.084)<1e-6
    assert observation['face_max_change_m']<1e-7
    assert observation['belly_depth_after_m']>1.15*observation['belly_depth_before_m']
    assert (capture/'complete.txt').exists()
    cameras=('F05_FrontFullBody','F01_Portrait','F03_Surface','F02_FullBody')
    variants=('Baseline_Fleece2','Relit_Fleece2','Fuller_Relit_Fleece2')
    expected={f'{c}_{v}' for c in cameras for v in variants}
    images={}
    for p in capture.glob('*.png'):
        data=p.read_bytes();assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',data[16:24])==(1920,1080)
        m=json.loads(p.with_suffix('.json').read_text())
        assert m['scene']==scene and m['api']=='Metal' and m['gpu']=='Apple M3 Max'
        assert m['retainedFleeceVariant']==2 and 'Cotton' in m['bodyShader'] and not m['dynamicResolution']
        assert abs(m['fixedExposure']-(7 if m['mode']==0 else 6.3))<1e-6
        images[p.stem]={'path':str(p.relative_to(ROOT)),'sha256':sha(p),'metadata':m}
    assert set(images)==expected
    deltas={}
    for c in cameras:
        a,b,d=[images[f'{c}_{v}']['metadata'] for v in variants]
        assert all(a[k]==b[k]==d[k] for k in ('cameraPosition','cameraRotation','fov','width','height','bodyShader'))
        assert a['modelAsset']==b['modelAsset'] and b['modelAsset']!=d['modelAsset']
        for v1,v2 in zip(variants,variants[1:]):
            delta=difference(capture/f'{c}_{v1}.png',capture/f'{c}_{v2}.png')
            # Face geometry is preserved; its close-up can change slightly from belly occlusion.
            if c!='F03_Surface' or v1=='Baseline_Fleece2':assert delta>.001
            deltas[f'{c}:{v1}->{v2}']=delta
    assert 'Scene='+scene in (motion/'complete.txt').read_text()
    assert (motion/'twilight-motion.mp4').exists()
    rows=list(csv.DictReader((motion/'frames.csv').open()));times=[float(r['time_s']) for r in rows]
    assert len(rows)>20 and times[-1]>29.5 and all(b>a for a,b in zip(times,times[1:]))
    assert all(r['fit_variant']=='2' and r['fit_active_model']=='Current_Bear_FullerBelly_WiderStance' for r in rows)
    assert any(r['grounded']=='False' for r in rows) and any(float(r['planar_mps'])>.4 for r in rows)
    assert len({r['animator_state_hash'] for r in rows})>=4
    tests={}
    for name in ('bear-fit-checks.txt','bear-fit-animation-clips.txt','bear-fit-sole-test.txt','bear-fit-motor-smoke.txt','bear-fit-animation-runtime.txt','bear-fit-variant-runtime.txt','bear-fit-rest-contact.txt'):
        p=ROOT/'Artifacts/UnityEditor'/name
        assert 'PASS:' in p.read_text() and p.stat().st_mtime>=(generated/'generation-receipt.json').stat().st_mtime
        tests[name]=p.read_text()
    result={'scope':__doc__,'collected_at_utc':datetime.now(timezone.utc).isoformat(),'scene':scene,
        'receipts_sha256':receipts,'source_art':source,'images':images,'image_differences_8bit_luma':deltas,
        'comparison_scope':'All modes retain prior material variant 2. Baseline->relit changes light geometry, powers, ambient and exposure. Relit->fuller changes sculpt, rig rest pose and regenerated clips. Image difference is not a quality score.',
        'tests':tests,'motion':{'path':str(motion.relative_to(ROOT)),'frames':len(rows),'last_timestamp_s':times[-1],'sha256':sha(motion/'twilight-motion.mp4')},
        'source_checks':json.loads((ROOT/'validation/unity_source_checks.json').read_text()),
        'not_verified':['owner visual approval','Player build','performance/GPU time/memory','Windows','full temporal stability acceptance','real keyboard/focus acceptance','room integration','LOD/quality workloads']}
    out=ROOT/'validation/unity_bear_fit_v002_checks.json';out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    print('PASS: source/current receipts, imported stance, 12 Metal frames, sampled skin/sole clips, live Animator/motor, 30s Play;',out)


if __name__=='__main__':main()
