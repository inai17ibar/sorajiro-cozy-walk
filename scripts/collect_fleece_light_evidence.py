#!/usr/bin/env python3
"""Validate existing lighting evidence; no visual approval, performance or hardware-audio claim."""
import argparse
import csv
import json
import struct
from pathlib import Path
from collect_bedside_evidence import ROOT, receipt, sha, difference


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('project','capture','motion'):parser.add_argument('--'+name,type=Path,required=True)
    args=parser.parse_args();project=args.project.resolve(strict=True);capture=args.capture.resolve(strict=True);motion=args.motion.resolve(strict=True)
    assert (capture/'complete.txt').exists() and (motion/'complete.txt').exists() and (motion/'twilight-motion.mp4').exists()
    images={}
    for camera in ('S01_Portrait','S03_ThreeQuarter','S02_Gameplay','Fleece_Close'):
        base=None
        for index,label in enumerate(('Before','Side600','Side850')):
            p=capture/(camera+'_'+label+'.png');data=p.read_bytes();assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',data[16:24])==(1920,1080)
            m=json.loads(p.with_suffix('.json').read_text());assert m['api']=='Metal' and m['gpu']=='Apple M3 Max' and m['exposure']==7 and m['variant']==index
            assert m['model']=='Preferred_BearFit_v001_3' and m['overlay']=='Assets/Sorajiro/Generated/FleeceLighting_v002/L_LookDev.unity'
            if base is None:base=m
            else:assert all(m[key]==base[key] for key in ('position','rotation','fov','exposure','model','room'))
            lamps={l['name']:l for l in m['lights']}
            assert lamps['Window_Side_Key']['power']==(370 if index==0 else 70)
            assert lamps['Room_Adaptation_Neutral_Fill']['power']==(240 if index==0 else 45 if index==1 else 60)
            assert lamps['Soft_Grazing_Fleece_Key']['enabled']==(index!=0)
            if index:assert lamps['Soft_Grazing_Fleece_Key']['power']==(600 if index==1 else 850)
            images[p.stem]={'path':str(p.relative_to(ROOT)),'sha256':sha(p),'metadata':m}
    delta=difference(capture/'Fleece_Close_Before.png',capture/'Fleece_Close_Side600.png');assert delta>.01
    receipts={name:receipt(project/'Assets/Sorajiro/Generated'/name) for name in ('FleeceLighting_v002','StarPlay_v003','BedsideBear_v002','BearFit_v001','Fleece_v005','Bedside_v009')}
    rows=list(csv.DictReader((motion/'frames.csv').open()));times=[float(r['time_s']) for r in rows]
    assert times[-1]>29.5 and all(b>a for a,b in zip(times,times[1:])) and all(r['visual_model']=='Preferred_BearFit_v001_3' for r in rows)
    assert any(r['grounded']=='False' for r in rows) and any(float(r['planar_mps'])>.4 for r in rows)
    source=json.loads((ROOT/'validation/unity_source_checks.json').read_text());assert all(c['exit_code']==0 for c in source['checks'])
    result={'scope':__doc__,'chosen_variant':1,'asset_receipts_sha256':receipts,'images':images,'image_difference_8bit_luma_not_quality_score':delta,
        'motion':{'path':str(motion.relative_to(ROOT)),'frames':len(rows),'last_timestamp_s':times[-1],'sha256':sha(motion/'twilight-motion.mp4')},
        'source_checks':source,'not_verified':['owner visual approval','Player build','FPS/GPU time/memory pressure','Windows','full temporal stability acceptance','gameplay regression under new lights beyond scripted motion']}
    out=ROOT/'validation/unity_fleece_light_v002_checks.json';out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print('PASS: protected inputs, same-camera/pose/EV Metal comparisons, actual 30s motion;',out)


if __name__=='__main__':main()
