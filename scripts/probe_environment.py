#!/usr/bin/env python3
"""Read-only, sanitized observations; never installs tools or locks an engine."""
from __future__ import annotations

import argparse
import importlib.util
import json
import os
import platform
import shutil
import subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def command(argv):
    try:
        result = subprocess.run(argv, text=True, capture_output=True, timeout=30)
        return {'exit_code': result.returncode,
                'stdout': result.stdout.strip(), 'stderr': result.stderr.strip()}
    except (OSError, subprocess.TimeoutExpired) as exc:
        return {'exit_code': None, 'stdout': '', 'stderr': type(exc).__name__}


def observe(ue_root=None):
    mac = platform.system() == 'Darwin'
    git_candidate = shutil.which('git')
    git_result = command([git_candidate, '--version']) if git_candidate else command(['git', '--version'])
    git_program = git_candidate or 'git'
    if git_result['exit_code'] != 0 and mac:
        original_failure = git_result
        git_program = '/usr/bin/git'
        git_result = command([git_program, '--version'])
        git_result['path_candidate_failure'] = original_failure
        git_result['using_system_git_fallback'] = True
    observed = {
        'observed_at_utc': datetime.now(timezone.utc).isoformat(),
        'session_type': 'local_mac' if mac else 'non_mac_source_session',
        'os': platform.system(), 'architecture': platform.machine(),
        'python': platform.python_version(), 'git': git_result,
        'compiler': command(['clang++', '--version']) if shutil.which('clang++') else None,
        'git_lfs': command([git_program, 'lfs', 'version']),
        'python_modules': {m: importlib.util.find_spec(m) is not None for m in ('unreal', 'bpy')},
        'disk_free_bytes': shutil.disk_usage(ROOT).free,
        'ue': {'available': False, 'version': None, 'build_id': None,
               'search_scope': 'UE_ROOT, PATH, /Users/Shared/Epic Games/UE_*, /Applications/UE_*',
               'compatibility_verified': False},
        'gpu': {'available_reported_by_os': None, 'render_test_run': False},
    }
    if mac:
        observed['macos'] = command(['sw_vers', '-productVersion'])['stdout'] or None
        observed['macos_build'] = command(['sw_vers', '-buildVersion'])['stdout'] or None
        observed['xcode'] = command(['xcodebuild', '-version'])
        # Do not serialize system_profiler's full output: it contains serial/UUID/UDID.
        result = command(['system_profiler', 'SPHardwareDataType', 'SPDisplaysDataType', '-json'])
        if result['exit_code'] == 0:
            data = json.loads(result['stdout'])
            hardware = data.get('SPHardwareDataType', [{}])[0]
            displays = data.get('SPDisplaysDataType', [{}])[0]
            observed['machine'] = {key: hardware.get(key) for key in
                                   ('machine_name', 'machine_model', 'chip_type', 'physical_memory')}
            observed['gpu'] = {
                'available_reported_by_os': bool(displays.get('sppci_model')),
                'model': displays.get('sppci_model'), 'cores': displays.get('sppci_cores'),
                'metal_os_report': displays.get('spdisplays_metal'), 'render_test_run': False}
    blender = shutil.which('blender')
    if not blender and mac:
        candidates = sorted(Path('/Applications').glob('Blender*.app/Contents/MacOS/Blender'))
        blender = str(candidates[-1]) if candidates else None
    if blender:
        result = command([blender, '--version'])
        observed['blender'] = {'exit_code': result['exit_code'],
                               'version_line': next((line for line in result['stdout'].splitlines()
                                                     if line.startswith('Blender ')), None),
                               'warning_present': bool(result['stderr'])}
    else:
        observed['blender'] = None
    candidates = []
    explicit = ue_root or os.environ.get('UE_ROOT')
    if explicit:
        candidates.append(Path(explicit))
    executable = shutil.which('UnrealEditor')
    if executable:
        candidates.extend(Path(executable).resolve().parents)
    if mac:
        candidates.extend(Path('/Users/Shared/Epic Games').glob('UE_*'))
        candidates.extend(Path('/Applications').glob('UE_*'))
    for candidate in candidates:
        version = candidate / 'Engine/Build/Build.version'
        editor = candidate / 'Engine/Binaries/Mac/UnrealEditor.app/Contents/MacOS/UnrealEditor'
        if version.is_file():
            data = json.loads(version.read_text())
            observed['ue'].update({
                'available': editor.is_file() if mac else bool(executable),
                'version': '.'.join(str(data[k]) for k in ('MajorVersion', 'MinorVersion', 'PatchVersion')),
                'build_id': data.get('BuildId', data.get('Changelist')),
                'distribution': 'local_install_observed_not_license_verified'})
            break
    return observed


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ue-root', type=Path)
    parser.add_argument('--report', type=Path)
    parser.add_argument('--write-environment', action='store_true')
    args = parser.parse_args()
    observed = observe(args.ue_root)
    encoded = json.dumps(observed, ensure_ascii=False, indent=2) + '\n'
    print(encoded)
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(encoded)
    if args.write_environment:
        path = ROOT / 'project.environment.json'
        env = json.loads(path.read_text())
        env['execution_environment'] = observed
        env['status'] = 'M0_M1_source_scaffold_not_UE_built_or_rendered'
        env['cloud'].update({'os_observed': observed['os'],
                             'ue_available_verified': observed['ue']['available'],
                             'gpu_available_verified': observed['gpu']['available_reported_by_os'],
                             'session_type_observed': observed['session_type']})
        if observed.get('machine', {}).get('chip_type') == 'Apple M3 Max':
            env['primary_target']['gpu_cores_verified'] = int(observed['gpu']['cores'])
            env['primary_target']['os_version_verified'] = observed['macos']
            env['primary_target']['memory_gb_verified'] = 36 if observed['machine']['physical_memory'] == '36 GB' else None
        env['toolchain']['xcode_verified'] = observed.get('xcode', {}).get('stdout') or None
        env['toolchain']['blender_verified'] = (observed.get('blender') or {}).get('version_line')
        if observed['ue']['version']:
            if env['engine']['version_locked'] and env['engine']['verified_version'] != observed['ue']['version']:
                raise SystemExit('Observed engine differs from lock; environment file not changed')
            env['engine']['verified_version'] = observed['ue']['version']
            env['engine']['build_id'] = observed['ue']['build_id']
            env['engine']['distribution'] = observed['ue']['distribution']
        # Observation never sets version_locked or asserts toolchain compatibility.
        path.write_text(json.dumps(env, ensure_ascii=False, indent=2) + '\n')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
