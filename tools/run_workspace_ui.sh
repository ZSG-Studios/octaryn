#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

find_repo_root() {
  local current="${OCTARYN_WORKSPACE_ROOT:-${script_dir}}"
  while [[ "${current}" != "/" ]]; do
    if [[ -f "${current}/CMakePresets.json" && -f "${current}/tools/ui/workspace_control_app.py" ]]; then
      printf '%s\n' "${current}"
      return 0
    fi
    current="$(dirname "${current}")"
  done
  printf '[error] could not find Octaryn workspace root from %s\n' "${script_dir}" >&2
  return 1
}

log() {
  printf '[workspace-ui] %s\n' "$*"
}

repo_root="$(find_repo_root)"
setup_script="${repo_root}/tools/build/linux_build_environment.sh"
podman_dir="${repo_root}/tools/build"
containerfile="${podman_dir}/Containerfile.arch-build"
image_name="${OCTARYN_ARCH_BUILDER_IMAGE:-localhost/octaryn-arch-builder:latest}"
builder_version="20260421-2"

run_setup() {
  if [[ ! -f "${setup_script}" ]]; then
    printf '[error] missing Linux setup helper: %s\n' "${setup_script}" >&2
    exit 1
  fi
  log "installing missing launcher dependencies"
  bash "${setup_script}" --yes --launcher-only
}

find_python() {
  if command -v python3 >/dev/null 2>&1; then
    command -v python3
    return 0
  fi
  if command -v python >/dev/null 2>&1; then
    command -v python
    return 0
  fi
  return 1
}

python_imports_pyside6() {
  "$1" -c 'from PySide6 import QtCore, QtGui, QtWidgets' >/dev/null 2>&1
}

python_cmd=""

ensure_python() {
  local resolved_python
  if ! resolved_python="$(find_python)"; then
    run_setup
    resolved_python="$(find_python)" || {
      printf '[error] Python is still missing after setup.\n' >&2
      exit 1
    }
  fi

  if ! "${resolved_python}" -c 'import sys; raise SystemExit(0 if sys.version_info >= (3, 10) else 1)' >/dev/null 2>&1; then
    printf '[error] Python 3.10 or newer is required: %s\n' "${resolved_python}" >&2
    exit 1
  fi

  if ! python_imports_pyside6 "${resolved_python}"; then
    run_setup
    if ! python_imports_pyside6 "${resolved_python}"; then
      printf '[error] PySide6 is still not importable by %s after setup.\n' "${resolved_python}" >&2
      exit 1
    fi
  fi

  python_cmd="${resolved_python}"
}

ensure_podman() {
  if ! command -v podman >/dev/null 2>&1; then
    run_setup
  fi
  if ! command -v podman >/dev/null 2>&1; then
    printf '[error] Podman is still missing after setup.\n' >&2
    exit 1
  fi
  podman info >/dev/null
}

ensure_arch_image() {
  if [[ ! -f "${containerfile}" || ! -f "${podman_dir}/arch_packages.txt" ]]; then
    printf '[error] missing Podman builder files under %s\n' "${podman_dir}" >&2
    exit 1
  fi
  local current_version=""
  if podman image exists "${image_name}"; then
    current_version="$(podman image inspect "${image_name}" --format '{{ index .Config.Labels "org.octaryn.arch-builder.version" }}' 2>/dev/null || true)"
  fi
  if [[ "${current_version}" == "${builder_version}" ]]; then
    log "Arch builder image ready: ${image_name}"
    return 0
  fi
  log "building Arch builder image ${image_name}"
  podman build \
    --build-arg "OCTARYN_ARCH_BUILDER_VERSION=${builder_version}" \
    -t "${image_name}" \
    -f "${containerfile}" \
    "${podman_dir}"
}

validate_workspace_mount() {
  mkdir -p "${repo_root}/logs/tools"
  log "validating workspace mount in ${image_name}"
  podman run --rm \
    --volume "${repo_root}:/workspace:Z" \
    --workdir /workspace \
    "${image_name}" \
    bash -lc 'test -f CMakePresets.json && test -f tools/ui/workspace_control_app.py && mkdir -p logs/tools && test -w logs/tools'
}

ensure_python
ensure_podman
ensure_arch_image
validate_workspace_mount

log "launching workspace control UI"
exec "${python_cmd}" "${repo_root}/tools/ui/workspace_control_app.py"
