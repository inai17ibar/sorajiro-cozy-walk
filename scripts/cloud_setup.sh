#!/usr/bin/env bash
# Lightweight preflight only. No downloads, UE installation, network or credentials.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
command -v python3 >/dev/null 2>&1 || { echo 'ERROR: Python 3 is required for handoff checks.' >&2; exit 2; }
command -v git >/dev/null 2>&1 || { echo 'ERROR: Git is required for the development workflow.' >&2; exit 2; }
TASK_GIT="$(command -v git)"
# A stale Intel-only /usr/local/bin/git may shadow Apple's working arm64 Git.
# Observe the failure and use the system executable without changing PATH/tools.
if ! "$TASK_GIT" --version >/dev/null 2>&1; then
  if [[ "$(uname -s)" == Darwin ]] && /usr/bin/git --version >/dev/null 2>&1; then
    printf '%s\n' 'PATH Git is not executable; using /usr/bin/git for this preflight only.'
    TASK_GIT=/usr/bin/git
  else
    echo 'ERROR: Git was found but cannot execute.' >&2
    exit 2
  fi
fi
python3 -c 'import sys; sys.exit(0 if sys.version_info >= (3,9) else "Python 3.9+ required")'
printf '%s\n' 'Environment preflight (no UE/GPU assumptions):'
uname -s
python3 --version
"$TASK_GIT" --version
python3 "$ROOT/scripts/validate_handoff.py"
printf '%s\n' 'UE BUILD / ASSET IMPORT / RENDER / PERFORMANCE: NOT RUN. This is documentation validation only.'
