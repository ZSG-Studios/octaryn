#!/usr/bin/env bash
set -euo pipefail

octaryn_tool_script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

octaryn_find_workspace_root() {
  local current="${OCTARYN_WORKSPACE_ROOT:-${octaryn_tool_script_dir}}"
  while [[ "${current}" != "/" ]]; do
    if [[ -f "${current}/CMakePresets.json" && -d "${current}/tools" ]]; then
      printf '%s\n' "${current}"
      return
    fi
    current="$(dirname "${current}")"
  done
  printf '[error] could not find Octaryn workspace root from %s\n' "${octaryn_tool_script_dir}" >&2
  exit 1
}

octaryn_workspace_root="$(octaryn_find_workspace_root)"

octaryn_host_core_count() {
  if command -v nproc >/dev/null 2>&1; then
    nproc
    return
  fi
  if command -v sysctl >/dev/null 2>&1; then
    sysctl -n hw.ncpu
    return
  fi
  printf '2\n'
}

octaryn_tool_build_root() {
  local preset="${1:-debug-linux}"
  printf '%s/build/%s/tools\n' "${octaryn_workspace_root}" "${preset}"
}

octaryn_dependency_source_root() {
  printf '%s/build/dependencies/src\n' "${octaryn_workspace_root}"
}

octaryn_tool_log_root() {
  printf '%s/logs/tools\n' "${octaryn_workspace_root}"
}

octaryn_ensure_dir() {
  mkdir -p "$1"
}

octaryn_fetch_git_tag() {
  local name="$1"
  local repository="$2"
  local tag="$3"
  local destination
  destination="$(octaryn_dependency_source_root)/${name}"
  octaryn_ensure_dir "$(dirname "${destination}")"

  if [[ -d "${destination}/.git" ]]; then
    git -C "${destination}" fetch --tags --force --prune origin >&2
  else
    rm -rf "${destination}"
    git clone --filter=blob:none "${repository}" "${destination}" >&2
  fi

  git -C "${destination}" checkout --force "${tag}" >&2
  printf '%s\n' "${destination}"
}
