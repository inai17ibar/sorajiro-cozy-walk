#!/usr/bin/env python3
"""Validate existing preferred-bear room evidence; not a visual or performance approval."""
import argparse
import csv
import json
import struct
from datetime import datetime, timezone
from pathlib import Path

from collect_bedside_evidence import ROOT, difference, receipt, sha


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('project','capture','motion','bake-job'):parser.add_argument('--'+name,type=Path,required=True)
    args=parser.parse_args()
    project,capture,motion,bake_job=(getattr(args,n).resolve(strict=True) for n in ('project','capture','motion','bake_job'))
    scene='Assets/Sorajiro/Generated/BedsideBear_v002/L_LookDev.unity'
    chosen='Assets/Sorajiro/Generated/BearFit_v001'
    generated=project/'Assets/Sorajiro/Generated/BedsideBear_v002'
    receipts={n:receipt(project/'Assets/Sorajiro/Generated'/n) for n in ('BearFit_v001','Fleece_v005','Bedside_v009','BedsideBear_v002')}
    bake=json.loads(bake_job.read_text())
    assert bake['scene']==scene and bake['state']=='completed' and bake['lightmaps']>0 and bake['apvBaked']
    assert (capture/'complete.txt').exists()
    expected={c+'_Preferred' for c in ('S01_Portrait','S02_Gameplay','S03_ThreeQuarter','S05_Bedding')}
    expected.update(('S01_Portrait_Hero_OFF','S01_Portrait_Room_Direct'))
    images={}
    for p in capture.glob('*.png'):
        data=p.read_bytes();assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',data[16:24])==(1920,1080)
        m=json.loads(p.with_suffix('.json').read_text())
        assert m['scene']==scene and m['api']=='Metal' and m['gpu']=='Apple M3 Max'
        assert m['controller']==chosen+'/BearMotion.controller' and m['mesh']==chosen+'/Bear.fbx'
        assert m['model']=='Preferred_BearFit_v001_3' and m['sourceComparison']==3
        assert m['fixedEV']==7 and not m['dynamicResolution'] and m['lightmaps']>0 and m['apvBaked']
        assert m['renderingLayer']==(1 if m['variant']=='Room_Direct' else 2)
        images[p.stem]={'path':str(p.relative_to(ROOT)),'sha256':sha(p),'metadata':m}
    assert set(images)==expected
    deltas={}
    base=images['S01_Portrait_Preferred']['metadata']
    for variant in ('Hero_OFF','Room_Direct'):
        m=images['S01_Portrait_'+variant]['metadata']
        assert all(base[k]==m[k] for k in ('position','rotation','fov','fixedEV','controller','mesh'))
        delta=difference(capture/'S01_Portrait_Preferred.png',capture/f'S01_Portrait_{variant}.png')
        assert delta>.01
        deltas[variant]=delta
    assert 'Scene='+scene in (motion/'complete.txt').read_text() and (motion/'twilight-motion.mp4').exists()
    rows=list(csv.DictReader((motion/'frames.csv').open()));times=[float(r['time_s']) for r in rows]
    assert len(rows)>20 and times[-1]>29.5 and all(b>a for a,b in zip(times,times[1:]))
    assert all(r['visual_model']=='Preferred_BearFit_v001_3' for r in rows)
    assert any(r['grounded']=='False' for r in rows) and any(float(r['planar_mps'])>.4 for r in rows)
    assert len({r['animator_state_hash'] for r in rows})>=4
    tests={}
    for name in ('bedside-bear-checks.txt','bedside-bear-motor-smoke.txt','bedside-bear-animation-runtime.txt'):
        p=ROOT/'Artifacts/UnityEditor'/name;assert 'PASS:' in p.read_text()
        assert p.stat().st_mtime>=(generated/'generation-receipt.json').stat().st_mtime
        tests[name]=p.read_text()
    result={'scope':__doc__,'collected_at_utc':datetime.now(timezone.utc).isoformat(),'scene':scene,
        'chosen_source':chosen+'/L_LookDev.unity','chosen_comparison':3,'receipts_sha256':receipts,'bake':bake,
        'images':images,'portrait_lighting_difference_8bit_luma':deltas,
        'comparison_scope':'Same selected mesh/materials/controller, pose, camera and EV7. Direct hero lights or receiving layer changes. Room ambient and post processing differ from the source studio. Differences are not a quality score.',
        'tests':tests,'motion':{'path':str(motion.relative_to(ROOT)),'frames':len(rows),'last_timestamp_s':times[-1],'sha256':sha(motion/'twilight-motion.mp4')},
        'source_checks':json.loads((ROOT/'validation/unity_source_checks.json').read_text()),
        'not_verified':['owner approval of room appearance','Player build','FPS/GPU time/memory pressure','Windows','full temporal stability acceptance','real keyboard/focus acceptance','LOD/quality workload controls']}
    out=ROOT/'validation/unity_bedside_bear_v002_checks.json';out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    print('PASS: exact chosen v001 mesh/controller, protected source receipts, private GPU bake/APV, 6 Metal frames, lighting switches, motor/Animator and 30s Play;',out)


if __name__=='__main__':main()
