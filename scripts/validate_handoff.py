#!/usr/bin/env python3
"""Validate this handoff pack only. Does NOT build, run or render Unreal Engine."""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import unquote, urlparse

REQUIRED = (
    'README.md', 'AGENTS.md', 'REQUIREMENTS_JA.md', 'project.environment.json',
    'docs/CLOUD_AND_LOCAL.md', 'docs/RENDERING_PLAN.md', 'docs/ARCHITECTURE.md',
    'docs/MILESTONES.md', 'docs/VERIFICATION.md', 'docs/SOURCES.md',
    'prompts/01_CLOUD_FIRST_TASK.md', 'prompts/02_MAC_FIRST_RUN.md',
    'prompts/03_RENDERING_ITERATION.md', 'reference/manifest.json',
    'templates/LOCAL_REPORT.md', 'templates/feature_matrix.json',
    'templates/benchmark_run.json', 'templates/asset_manifest.example.json',
    'templates/gitignore.example', 'templates/gitattributes.example',
)


def validate(root: Path) -> list[dict[str, object]]:
    checks: list[dict[str, object]] = []
    def add(name: str, ok: bool, details: str) -> None:
        checks.append({'name': name, 'status': 'PASS' if ok else 'FAIL', 'details': details})

    missing = [p for p in REQUIRED if not (root / p).is_file()]
    add('required_document_files', not missing, ', '.join(missing) or f'{len(REQUIRED)} required files exist')

    errors: list[str] = []
    for path in root.rglob('*.json'):
        if 'validation' in path.relative_to(root).parts:
            continue
        try:
            json.loads(path.read_text(encoding='utf-8'))
        except (OSError, ValueError, UnicodeError) as exc:
            errors.append(f'{path.relative_to(root)}: {exc}')
    add('json_syntax', not errors, '; '.join(errors) or 'All configuration/example JSON files parsed')

    bad_links: list[str] = []
    for path in root.rglob('*.md'):
        text = path.read_text(encoding='utf-8')
        if text.count('```') % 2:
            bad_links.append(f'{path.relative_to(root)}: unbalanced fenced code blocks')
        for dest in re.findall(r'!?\[[^\]]*\]\(([^)]+)\)', text):
            target = dest.strip().split(' "', 1)[0]
            parsed = urlparse(target)
            if parsed.scheme or target.startswith('#'):
                continue
            local = path.parent / unquote(target.split('#', 1)[0])
            if not local.exists():
                bad_links.append(f'{path.relative_to(root)}: missing link {target}')
    add('markdown_local_links_and_fences', not bad_links, '; '.join(bad_links) or 'Relative Markdown targets exist; code fences balanced')

    agents = root / 'AGENTS.md'
    size = agents.stat().st_size if agents.is_file() else 0
    add('root_agents_size', 0 < size < 32768, f'{size} bytes; global or nested instructions may add to Codex limits')

    manifest_path = root / 'reference/manifest.json'
    try:
        manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
        for item in manifest['references']:
            path = (root / item['path']).resolve()
            if not path.is_relative_to(root.resolve()):
                raise ValueError('Reference path escapes handoff root')
            data = path.read_bytes()
            actual = hashlib.sha256(data).hexdigest()
            ok = actual == item['sha256'] and len(data) == item['bytes']
            add('reference_integrity:' + item['path'], ok, f'{len(data)} bytes; SHA-256 {actual}')
            magic = data.startswith(b'\x89PNG\r\n\x1a\n') or data.startswith(b'\xff\xd8\xff')
            add('reference_image_header:' + item['path'], magic, 'PNG/JPEG header check only; not a rendering evaluation')
    except (OSError, KeyError, TypeError, ValueError) as exc:
        add('reference_manifest', False, str(exc))

    try:
        env = json.loads((root / 'project.environment.json').read_text(encoding='utf-8'))
        engine = env['engine']
        ok = not engine['version_locked'] or bool(engine['verified_version'])
        add('engine_lock_consistency', ok, 'A locked engine must have an observed version; pending is allowed')
    except (OSError, KeyError, TypeError, ValueError) as exc:
        add('engine_lock_consistency', False, str(exc))
    return checks


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--report', type=Path, help='Optional JSON report path; does not launch UE')
    args = parser.parse_args()
    checks = validate(args.root.resolve())
    passed = all(c['status'] == 'PASS' for c in checks)
    result = {
        'scope': 'handoff_documentation_and_reference_integrity_only',
        'checked_at_utc': datetime.now(timezone.utc).isoformat(),
        'python_version': sys.version.split()[0],
        'status': 'PASS' if passed else 'FAIL',
        'checks': checks,
        'not_run': ['Codex Cloud task', 'Unreal C++ build', 'UE asset generation/import',
                    'Metal or other UE shader/render test', 'gameplay', 'Mac FPS/memory benchmark',
                    'Windows compatibility', 'packaging'],
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    return 0 if passed else 1


if __name__ == '__main__':
    raise SystemExit(main())
