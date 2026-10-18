#!/usr/bin/env python3
"""Explicit local UE commands with exact argv/exit logs. No installs or version changes."""
from __future__ import annotations

import argparse
import json
import os
import platform
import subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / 'Unreal/Sorajiro.uproject'


def paths(ue_root):
    base = Path(ue_root).expanduser().resolve()
    return {
        'build': base / 'Engine/Build/BatchFiles/Mac/Build.sh',
        'editor': base / 'Engine/Binaries/Mac/UnrealEditor.app/Contents/MacOS/UnrealEditor',
        'uat': base / 'Engine/Build/BatchFiles/RunUAT.sh',
        'version': base / 'Engine/Build/Build.version',
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=['preflight', 'build-editor', 'build-game', 'editor', 'play', 'package'])
    parser.add_argument('--ue-root', default=os.environ.get('UE_ROOT'))
    parser.add_argument('--print-only', action='store_true', help='No engine execution; only print exact argv')
    args = parser.parse_args()
    if not args.ue_root:
        parser.error('Supply --ue-root or UE_ROOT (installation root containing Engine/)')
    p = paths(args.ue_root)
    if not p['version'].is_file():
        parser.error('Engine/Build/Build.version not found; no valid engine root')
    build_version = json.loads(p['version'].read_text())
    version = '.'.join(str(build_version[k]) for k in ('MajorVersion', 'MinorVersion', 'PatchVersion'))
    lock = json.loads((ROOT / 'project.environment.json').read_text())['engine']
    if lock['version_locked'] and lock['verified_version'] != version:
        parser.error('Selected engine differs from the locked version; preserve the lock/project')
    if platform.system() != 'Darwin' or platform.machine() != 'arm64':
        parser.error('This helper targets native Apple Silicon macOS; run on the target Mac')
    if not all(p[k].is_file() for k in ('build', 'editor', 'uat')):
        parser.error('Required build/editor/UAT files missing')
    print(json.dumps({'engine_observed': version, 'version_locked': lock['version_locked'],
                      'toolchain_compatibility': 'NOT_RUN; check Epic requirements for this patch',
                      'action': args.action}, ensure_ascii=False))
    if args.action == 'preflight':
        return 0  # File/path check only, not a UBT/UHT, shader, asset or render pass.
    recipe = json.loads((ROOT / 'Tools/Unreal/recipes/bootstrap_v001.json').read_text())
    map_path = recipe['output_root'] + '/' + recipe['map_name']
    if args.action.startswith('build-'):
        target = 'SorajiroEditor' if args.action == 'build-editor' else 'Sorajiro'
        argv = ['bash', str(p['build']), target, 'Mac', 'Development',
                '-Project=' + str(PROJECT), '-architecture=arm64', '-WaitMutex']
    elif args.action in ('editor', 'play'):
        argv = [str(p['editor']), str(PROJECT)]
        if args.action == 'editor':
            # The generated startup map does not exist before the first generator run.
            argv.append('/Engine/Maps/Entry')
        else:
            if not (ROOT / 'Unreal/Content' / map_path.removeprefix('/Game/')).with_suffix('.umap').is_file():
                parser.error('Generate real editor assets before play')
            argv.extend([map_path, '-game', '-windowed', '-ResX=1920', '-ResY=1080'])
        argv.extend(['-NoSplash', '-log'])
    else:
        if not (ROOT / 'Unreal/Content' / map_path.removeprefix('/Game/')).with_suffix('.umap').is_file():
            parser.error('Generate real editor assets before cook/package')
        archive = ROOT / 'Artifacts/Package'
        argv = ['bash', str(p['uat']), 'BuildCookRun', '-project=' + str(PROJECT),
                '-noP4', '-platform=Mac', '-clientconfig=Development', '-architecture=arm64',
                '-build', '-cook', '-stage', '-pak', '-archive', '-archivedirectory=' + str(archive),
                '-utf8output', '-unattended', '-nocompileeditor']
    print(json.dumps({'argv': argv}, ensure_ascii=False))
    if args.print_only:
        return 0
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    output = ROOT / 'Artifacts/Logs'
    output.mkdir(parents=True, exist_ok=True)
    logfile = output / (stamp + '-' + args.action + '.log')
    # Build tools retain their own stdout; tee without shell interpolation.
    with logfile.open('w') as log:
        process = subprocess.Popen(argv, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                   text=True, bufsize=1)
        for line in process.stdout:
            print(line, end='', flush=True)
            log.write(line)
        code = process.wait()
    (output / (stamp + '-' + args.action + '.json')).write_text(json.dumps({
        'argv': argv, 'engine_version': version, 'exit_code': code,
        'log': str(logfile.relative_to(ROOT)), 'action': args.action,
        'visual_verified': False, 'performance_verified': False}, indent=2) + '\n')
    print(f'Exit {code}; log: {logfile}')
    return code


if __name__ == '__main__':
    raise SystemExit(main())
