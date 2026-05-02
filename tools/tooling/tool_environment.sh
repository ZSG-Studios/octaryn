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

octaryn_validate_preset_name() {
  local preset="${1:-debug-linux}"
  case "${preset}" in
    debug-linux|release-linux|debug-windows|release-windows) ;;
    *)
      printf '[error] unsupported preset: %s\n' "${preset}" >&2
      printf '[error] expected one of: debug-linux release-linux debug-windows release-windows\n' >&2
      exit 2
      ;;
  esac
}

octaryn_target_arch() {
  local arch="${OCTARYN_TARGET_ARCH:-x64}"
  case "${arch}" in
    x64|arm64) printf '%s\n' "${arch}" ;;
    *)
      printf '[error] unsupported OCTARYN_TARGET_ARCH: %s\n' "${arch}" >&2
      printf '[error] expected one of: x64 arm64\n' >&2
      exit 2
      ;;
  esac
}

octaryn_build_root_name() {
  local preset="${1:-debug-linux}"
  local arch="${2:-$(octaryn_target_arch)}"
  octaryn_validate_preset_name "${preset}"
  if [[ "${arch}" == "x64" ]]; then
    printf '%s\n' "${preset}"
  else
    printf '%s-%s\n' "${preset}" "${arch}"
  fi
}

octaryn_tool_build_root() {
  local preset="${1:-debug-linux}"
  octaryn_validate_preset_name "${preset}"
  printf '%s/build/%s/tools\n' "${octaryn_workspace_root}" "$(octaryn_build_root_name "${preset}")"
}

octaryn_cmake_build_dir() {
  local preset="${1:-debug-linux}"
  octaryn_validate_preset_name "${preset}"
  printf '%s/build/%s/cmake\n' "${octaryn_workspace_root}" "$(octaryn_build_root_name "${preset}")"
}

octaryn_dependency_build_root() {
  local preset="${1:-debug-linux}"
  octaryn_validate_preset_name "${preset}"
  printf '%s/build/%s/deps/tool-build\n' "${octaryn_workspace_root}" "$(octaryn_build_root_name "${preset}")"
}

octaryn_preset_target_platform() {
  local preset="${1:-debug-linux}"
  octaryn_validate_preset_name "${preset}"
  case "${preset}" in
    *-linux) printf 'linux\n' ;;
    *-windows) printf 'windows\n' ;;
  esac
}

octaryn_preset_cmake_system_name() {
  case "$(octaryn_preset_target_platform "$1")" in
    linux) printf 'Linux\n' ;;
    windows) printf 'Windows\n' ;;
  esac
}

octaryn_preset_toolchain_file() {
  case "$(octaryn_preset_target_platform "$1")" in
    linux) printf '%s\n' "${octaryn_workspace_root}/cmake/Toolchains/Linux/clang.cmake" ;;
    windows) printf '%s\n' "${octaryn_workspace_root}/cmake/Toolchains/Windows/clang.cmake" ;;
  esac
}

octaryn_preset_build_type() {
  local preset="${1:-debug-linux}"
  octaryn_validate_preset_name "${preset}"
  case "${preset}" in
    debug-*) printf 'Debug\n' ;;
    release-*) printf 'Release\n' ;;
  esac
}

octaryn_target_executable_name() {
  local preset="$1"
  local name="$2"
  case "$(octaryn_preset_target_platform "${preset}")" in
    windows) printf '%s.exe\n' "${name}" ;;
    *) printf '%s\n' "${name}" ;;
  esac
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
