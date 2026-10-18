#!/usr/bin/env python3
"""Source-only checks. Does NOT compile UCLASS/UHT, create assets or render."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import py_compile
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--report', type=Path, default=ROOT / 'validation/source_checks.json')
    args = parser.parse_args()
    output = ROOT / 'Artifacts/SourceChecks'
    output.mkdir(parents=True, exist_ok=True)
    checks = []

    def run(name, argv):
        result = subprocess.run(argv, cwd=ROOT, text=True, capture_output=True)
        log = output / (name + '.log')
        log.write_text(result.stdout + result.stderr)
        print(name + ': ' + ('PASS' if result.returncode == 0 else 'FAIL'))
        checks.append({'name': name, 'argv': argv, 'status': 'PASS' if result.returncode == 0 else 'FAIL',
                       'exit_code': result.returncode, 'log': str(log.relative_to(ROOT)),
                       'output': result.stdout + result.stderr})
        return result.returncode == 0

    run('handoff', [sys.executable, 'scripts/validate_handoff.py'])
    run('cloud_setup', ['bash', 'scripts/cloud_setup.sh'])
    syntax_errors = []
    for path in [*ROOT.glob('scripts/*.py'), *ROOT.glob('Tools/**/*.py'), *ROOT.glob('Tests/*.py')]:
        try:
            # py_compile only; never imports/executes unreal or bpy.
            relative = path.relative_to(ROOT)
            compiled = output / 'pycache' / relative.with_suffix('.pyc')
            compiled.parent.mkdir(parents=True, exist_ok=True)
            py_compile.compile(str(path), cfile=str(compiled), doraise=True)
        except py_compile.PyCompileError as exc:
            syntax_errors.append(str(exc))
    checks.append({'name': 'python_syntax_only', 'status': 'FAIL' if syntax_errors else 'PASS',
                   'errors': syntax_errors})
    try:
        project = json.loads((ROOT / 'Unreal/Sorajiro.uproject').read_text())
        assert project['Modules'][0]['Name'] == 'Sorajiro'
        assert project['Modules'][0]['Type'] == 'Runtime'
        checks.append({'name': 'uproject_json_only', 'status': 'PASS'})
    except (ValueError, KeyError, AssertionError) as exc:
        checks.append({'name': 'uproject_json_only', 'status': 'FAIL', 'reason': str(exc)})
    run('recipe_contract', [sys.executable, '-m', 'unittest', 'discover', '-s', 'Tests', '-p', 'test_*.py', '-v'])
    compiler = os.environ.get('CXX') or shutil.which('clang++') or shutil.which('g++')
    if compiler:
        binary = output / 'camera_policy_test'
        binary_relative = str(binary.relative_to(ROOT))
        if run('pure_cpp_compile', [compiler, '-std=c++17', '-Wall', '-Wextra', '-Werror', '-pedantic',
               '-IUnreal/Source/Sorajiro/Public', 'Tests/Pure/camera_policy_test.cpp', '-o', binary_relative]):
            run('pure_cpp_runtime', ['./' + binary_relative])
    else:
        checks.append({'name': 'pure_cpp_compile', 'status': 'NOT_RUN', 'reason': 'No native C++ compiler'})
    passed = all(c['status'] == 'PASS' for c in checks)
    source_files = sorted({*ROOT.glob('scripts/*.py'), *ROOT.glob('Tools/**/*.py'),
                           *ROOT.glob('Tools/**/*.json'), *ROOT.glob('Tests/**/*.py'),
                           *ROOT.glob('Tests/**/*.cpp'), *ROOT.glob('Unreal/Source/**/*.*'),
                           *ROOT.glob('Unreal/Config/*.ini'), ROOT / 'Unreal/Sorajiro.uproject'})
    fingerprints = {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
                    for p in source_files if p.is_file()}
    report = {'scope': 'source_only_not_UE_build_assets_runtime_render_or_performance',
              'checked_at_utc': datetime.now(timezone.utc).isoformat(),
              'source_sha256': fingerprints,
              'status': 'PASS' if passed else 'INCOMPLETE_OR_FAIL', 'checks': checks,
              'not_run': ['UE UBT/UHT build', 'Editor Python execution/asset generation',
                          'Blender asset generation', 'Metal/shaders', 'gameplay',
                          'visual/motion verification', 'performance', 'Mac package', 'Windows']}
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    print('UE BUILD / ASSETS / RUNTIME / VISUAL / PERFORMANCE: NOT_RUN')
    return 0 if passed else 1


if __name__ == '__main__':
    raise SystemExit(main())
