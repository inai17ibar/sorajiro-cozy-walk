#!/usr/bin/env python3
"""Compile against installed Unity references; execute shared pure logic. No Editor/Player/render claim."""
import argparse
import json
import subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / 'Unity/Packages/com.sorajiro.cozywalk'

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--editor', type=Path, required=True, help='Unity.app')
    parser.add_argument('--project', type=Path, required=True)
    args = parser.parse_args()
    scripting = args.editor / 'Contents/Resources/Scripting'
    sdk = scripting / 'DotNetSdk'
    dotnet = sdk / 'dotnet'
    csc = next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
    standard = list(sdk.glob('packs/NETStandard.Library.Ref/*/ref/netstandard2.1/*.dll'))
    engine = list((scripting / 'Managed/UnityEngine').glob('*.dll'))
    assemblies = args.project / 'Library/ScriptAssemblies'
    output = ROOT / 'Artifacts/UnitySourceChecks'
    output.mkdir(parents=True, exist_ok=True)
    checks = []

    def compile(name, sources, references, executable=False):
        target = output / (name + '.dll')
        response = output / (name + '.rsp')
        lines = ['-nologo', '-nostdlib+', '-langversion:9.0',
                 '-target:' + ('exe' if executable else 'library'), '-out:"' + str(target) + '"']
        lines += ['-r:"' + str(p) + '"' for p in references]
        lines += ['"' + str(p) + '"' for p in sources]
        response.write_text('\n'.join(lines) + '\n')
        return run(name, [str(dotnet), str(csc), '@' + str(response)])

    def run(name, argv):
        result = subprocess.run(argv, cwd=ROOT, capture_output=True, text=True)
        log = output / (name + '.log')
        log.write_text(result.stdout + result.stderr)
        checks.append({'name': name, 'exit_code': result.returncode, 'log': str(log.relative_to(ROOT))})
        print(name, 'PASS' if result.returncode == 0 else 'FAIL', result.stdout + result.stderr)
        return result.returncode == 0

    base = standard + engine
    if compile('Sorajiro.Runtime', sorted((PACKAGE / 'Runtime').glob('*.cs')),
               base + [assemblies / 'Unity.InputSystem.dll']):
        compile('Sorajiro.Editor', sorted((PACKAGE / 'Editor').glob('*.cs')),
                base + [output / 'Sorajiro.Runtime.dll',
                        assemblies / 'Unity.RenderPipelines.Core.Runtime.dll',
                        assemblies / 'Unity.RenderPipelines.HighDefinition.Runtime.dll'])
    net8 = list(sdk.glob('packs/Microsoft.NETCore.App.Ref/8.*/ref/net8.0/*.dll'))
    if compile('MotionPolicyTest', [PACKAGE / 'Runtime/MotionPolicy.cs', ROOT / 'Tests/Unity/MotionPolicyTest.cs'], net8, True):
        (output / 'MotionPolicyTest.runtimeconfig.json').write_text(json.dumps({
            'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '8.0.0'}}}))
        run('MotionPolicy_execution', [str(dotnet), str(output / 'MotionPolicyTest.dll')])
    if compile('PlushGaitTest', [PACKAGE / 'Runtime/PlushGait.cs', ROOT / 'Tests/Unity/PlushGaitTest.cs'], net8, True):
        (output / 'PlushGaitTest.runtimeconfig.json').write_text(json.dumps({
            'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '8.0.0'}}}))
        run('PlushGait_execution', [str(dotnet), str(output / 'PlushGaitTest.dll')])
    for name, source in (('FootstepBeatTest','Runtime/FootstepBeat.cs'),('PlushSfxTest','Editor/PlushSfx.cs'),('CameraComfortTest','Runtime/CameraComfortPolicy.cs'),('CutePlushSfxTest','Editor/CutePlushSfx.cs')):
        if compile(name, [PACKAGE / source, ROOT / ('Tests/Unity/'+name+'.cs')], net8, True):
            (output / (name+'.runtimeconfig.json')).write_text(json.dumps({
                'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '8.0.0'}}}))
            run(name+'_execution', [str(dotnet), str(output / (name+'.dll'))])
    report = {'observed_at_utc': datetime.now(timezone.utc).isoformat(), 'checks': checks,
              'scope': 'Roslyn assemblies against installed Unity references + shared pure logic execution',
              'unity_import': 'not_run', 'player_build': 'not_run', 'render': 'not_run', 'performance': 'not_run'}
    (ROOT / 'validation/unity_source_checks.json').write_text(json.dumps(report, indent=2) + '\n')
    return int(any(c['exit_code'] != 0 for c in checks))

if __name__ == '__main__':
    raise SystemExit(main())
