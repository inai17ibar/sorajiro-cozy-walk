#!/usr/bin/env python3
"""Verify existing local baked-lighting evidence, not execute/approve rendering."""
import argparse
import csv
import hashlib
import json
import re
import struct
import subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def receipt(folder):
    p=folder/'generation-receipt.json'
    data=json.loads(p.read_text())
    expected=dict(zip(data['files'],data['hashes'],strict=True))
    actual={f.relative_to(folder).as_posix():sha(f) for f in folder.rglob('*') if f.is_file() and f.name not in (p.name,p.name+'.meta')}
    assert actual==expected, 'Generated assets changed: '+str(folder)
    return sha(p)


def baseline_integrity(folder):
    """Allow only the exact installed HDRP OnValidate migration, proven against old bytes."""
    data=json.loads((folder/'generation-receipt.json').read_text())
    expected=dict(zip(data['files'],data['hashes'],strict=True))
    actual={f.relative_to(folder).as_posix():sha(f) for f in folder.rglob('*') if f.is_file() and f.name not in ('generation-receipt.json','generation-receipt.json.meta')}
    assert actual.keys()==expected.keys(), 'Baseline inventory changed'
    changed=[k for k in actual if actual[k]!=expected[k]]
    if not changed:return {'state':'byte_identical','receipt_sha256':sha(folder/'generation-receipt.json')}
    assert changed==['APV_BakingSet.asset'], 'Unreviewed baseline edit preserved: '+str(changed)
    migrated=(folder/'APV_BakingSet.asset').read_text()
    inverse=[('m_Version: 1','m_Version: 0'),('rayOriginBias: -0.001','rayOriginBias: 0'),
             ('m_Bits: 4294967291','m_Bits: 0'),('  m_LightingScenarios:\n  - Default','  m_LightingScenarios: []')]
    original=migrated
    for current,previous in inverse:
        assert original.count(current)==1
        original=original.replace(current,previous)
    assert hashlib.sha256(original.encode()).hexdigest()==expected['APV_BakingSet.asset'], 'Migration does not fully explain baseline change'
    return {'state':'engine_OnValidate_migration_only_preserved_not_byte_identical',
        'changed_file':'APV_BakingSet.asset','original_sha256':expected['APV_BakingSet.asset'],
        'current_sha256':actual['APV_BakingSet.asset'],'inverse_migration_exactly_matches_original_hash':True,
        'migration_fields':['settings version 0 -> 1','virtual offset ray origin 0 -> -0.001','collision mask 0 -> -5','lighting scenario [] -> Default'],
        'scope':'All other baseline files remain byte-identical. Old receipt intentionally not rewritten; old generator strict reuse can reject this migrated asset.'}


def difference(first, second):
    # Subtract an identical-image baseline to account for ffmpeg's YUV black-range offset.
    def measure(a,b):
        result=subprocess.run(['ffmpeg','-hide_banner','-i',str(a),'-i',str(b),
            '-filter_complex','[0:v][1:v]blend=all_mode=difference,signalstats,metadata=mode=print',
            '-frames:v','1','-f','null','-'],capture_output=True,text=True,check=True)
        match=re.search(r'lavfi.signalstats.YAVG=([0-9.]+)',result.stderr)
        assert match, 'No measured image difference'
        return float(match[1])
    return measure(first,second)-measure(first,first)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project',type=Path,required=True)
    parser.add_argument('--capture',type=Path,required=True)
    parser.add_argument('--bake-job',type=Path,required=True)
    parser.add_argument('--motion',type=Path,required=True)
    parser.add_argument('--revision',default='v003')
    args=parser.parse_args()
    for field in ('project','capture','bake_job','motion'):
        setattr(args,field,getattr(args,field).resolve(strict=True))
    assert re.fullmatch(r'v\d{3}',args.revision)
    folder=args.project/f'Assets/Sorajiro/Generated/Bedside_{args.revision}'
    protected=receipt(folder)
    source=receipt(args.project/'Assets/Sorajiro/Generated/Twilight_v010')
    bake=json.loads(args.bake_job.read_text())
    scene=f'Assets/Sorajiro/Generated/Bedside_{args.revision}/L_LookDev.unity'
    assert bake['state']=='completed' and bake['apvBaked'] and bake['lightmaps']>0 and bake['scene']==scene
    assert (args.capture/'complete.txt').exists()
    images=[]
    for p in sorted(args.capture.glob('*.png')):
        content=p.read_bytes()
        assert content.startswith(b'\x89PNG\r\n\x1a\n') and struct.unpack('>II',content[16:24])==(1920,1080)
        metadata=json.loads(p.with_suffix('.json').read_text())
        assert metadata['scene']==scene and metadata['api']=='Metal' and metadata['apvBaked'] and metadata['lightmaps']>0
        assert not metadata['dynamicResolution']
        images.append({'path':p.relative_to(ROOT).as_posix(),'sha256':sha(p),'metadata':metadata})
    assert len(images)==7
    comparisons={}
    for name in ('S05_Bedding','S01_Portrait'):
        delta=difference(args.capture/f'{name}_Cotton_GI_ON.png',args.capture/f'{name}_Cotton_GI_OFF.png')
        assert delta>.05, 'Indirect-light switch has no visible workload effect'
        comparisons[name+'_GI_switch_YAVG_8bit']=delta
    delta=difference(args.capture/'S05_Bedding_Cotton_GI_ON.png',args.capture/'S05_Bedding_Lit_GI_ON.png')
    assert delta>.01, 'Fabric switch has no visible effect'
    comparisons['S05_Fabric_switch_YAVG_8bit']=delta
    assert (args.motion/'complete.txt').exists() and (args.motion/'twilight-motion.mp4').exists()
    assert 'Scene='+scene in (args.motion/'complete.txt').read_text()
    rows=list(csv.DictReader((args.motion/'frames.csv').open()))
    times=[float(r['time_s']) for r in rows]
    assert len(rows)>20 and times[-1]>29.5 and all(b>a for a,b in zip(times,times[1:]))
    assert any(r['grounded']=='False' for r in rows) and any(float(r['planar_mps'])>.4 for r in rows)
    assert len({r['animator_state_hash'] for r in rows})>=4
    editor=ROOT/'Artifacts/UnityEditor'
    prefix='bedding' if args.revision in ('v004','v005','v006','v007','v008','v009') else 'bedside'
    extra={}
    if args.revision in ('v004','v005','v006','v007','v008','v009'):
        extra['baseline_v003_integrity']=baseline_integrity(args.project/'Assets/Sorajiro/Generated/Bedside_v003')
        art=ROOT/('Artifacts/SourceArt/Room/twilight_v012' if args.revision in ('v008','v009') else 'Artifacts/SourceArt/Room/twilight_v011' if args.revision=='v007' else 'Artifacts/SourceArt/Room/twilight_v010' if args.revision=='v006' else 'Artifacts/SourceArt/Room/twilight_v009' if args.revision=='v005' else 'Artifacts/SourceArt/Room/twilight_v008')
        source_art=json.loads((art/'receipt.json').read_text())
        assert all(sha(art/name)==digest for name,digest in source_art['files'].items())
        assert sha(folder/'Room.fbx')==sha(art/'TwilightRoom.fbx')
        assert source_art['observations']['pillow_closed_panels']
        if args.revision in ('v006','v007','v008','v009'):assert source_art['observations']['fold_join_max_gap_m']<.001
        extra['bedding_source_art']=source_art
        baseline=ROOT/'Artifacts/Visual/bedside/20261018T123249573/S05_Bedding_Cotton_GI_ON.png'
        extra['v003_baseline_bedding_luma_difference']=difference(args.capture/'S05_Bedding_Cotton_GI_ON.png',baseline)
        extra['baseline_difference_scope']='Same fixed camera/exposure/material recipe; geometry, occluders, APV validity and fresh bake differ. Not an isolated geometry or quality score.'
    tests={}
    for name in ('bedside-checks.txt','bedside-animation-runtime.txt','bedside-motor-smoke.txt','bedside-scripted-focus.txt'):
        path=editor/name.replace('bedside-',prefix+'-')
        assert path.stat().st_mtime>=(folder/'generation-receipt.json').stat().st_mtime
        tests[name]=path.read_text()
        assert 'PASS:' in tests[name] if name!='bedside-scripted-focus.txt' else 'focused=true' in tests[name]
    result={'scope':__doc__,'collected_at_utc':datetime.now(timezone.utc).isoformat(),'scene':scene,
        'revision_details':extra,'generated_receipt_sha256':protected,'unchanged_v010_receipt_sha256':source,
        'bake':bake,'images':images,'image_difference_analysis':comparisons,
        'difference_scope':'Measured 8-bit luma signal difference from actual captures; not an artistic quality score or FPS metric.',
        'tests':tests,'motion':{'folder':args.motion.relative_to(ROOT).as_posix(),'frames':len(rows),'last_timestamp_s':times[-1],
                               'sha256':sha(args.motion/'twilight-motion.mp4')},
        'source_checks':json.loads((ROOT/'validation/unity_source_checks.json').read_text()),
        'not_verified':['owner visual approval','Player build','Windows','FPS/memory pressure','keyboard and real focus acceptance',
                        'runtime cloth simulation','LOD and workload quality controls']}
    destination=ROOT/f'validation/unity_bedside_{args.revision}_checks.json'
    destination.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    print('PASS: protected source/current assets, GPU bake/APV, 7 actual captures, visible GI/fabric switches, scripted motion;',destination)


if __name__=='__main__':
    main()
