#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../tooling/tool_environment.sh
source "${script_dir}/../tooling/tool_environment.sh"

preset="debug-linux"
tag="${OCTARYN_TRACY_TAG:-v0.13.1}"
command="launch-profiler"
address="127.0.0.1"
port="8086"
seconds="10"
export_capture="1"

usage() {
  cat <<'USAGE'
Usage: tracy_tool.sh [options] <command>

Commands:
  build             Stage target-native Tracy tools for the selected preset.
  launch-profiler  Build and launch the Linux-host Tracy profiler UI.
  capture          Capture a Tracy session from the running Linux client.
  print-profiler   Build if needed and print the profiler executable path.
  print-capture    Build if needed and print the target capture executable path.
  print-csvexport  Build if needed and print the target CSV export executable path.

Options:
  --preset <name>   Active build preset. Default: debug-linux.
  --tag <tag>       Tracy git tag. Default: v0.13.1.
  --address <addr>  Tracy client address for capture. Default: 127.0.0.1.
  --port <port>     Tracy client port for capture. Default: 8086.
  --seconds <n>     Capture duration. Default: 10.
  --no-export       Keep only the .tracy capture.
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --preset)
      preset="$2"
      shift 2
      ;;
    --tag)
      tag="$2"
      shift 2
      ;;
    --address)
      address="$2"
      shift 2
      ;;
    --port)
      port="$2"
      shift 2
      ;;
    --seconds)
      seconds="$2"
      shift 2
      ;;
    --no-export)
      export_capture="0"
      shift
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      command="$1"
      shift
      ;;
  esac
done

tool_root="$(octaryn_tool_build_root "${preset}")/tracy"
dependency_build_root="$(octaryn_dependency_build_root "${preset}")/tracy"
source_cache_root="$(octaryn_dependency_source_root)/cpm"
release_cache_root="$(octaryn_dependency_source_root)/tracy-releases"
target_platform="$(octaryn_preset_target_platform "${preset}")"
toolchain_file="$(octaryn_preset_toolchain_file "${preset}")"
log_root="$(octaryn_tool_log_root)"
log_path="${log_root}/tracy_tool.log"
lock_path="${log_root}/tracy_tool.lock"
profiler_build_dir="${tool_root}/profiler"
capture_build_dir="${tool_root}/capture"
csvexport_build_dir="${tool_root}/csvexport"
profiler_binary="$(octaryn_target_executable_name "${preset}" tracy-profiler)"
capture_binary="$(octaryn_target_executable_name "${preset}" tracy-capture)"
csvexport_binary="$(octaryn_target_executable_name "${preset}" tracy-csvexport)"
tracy_version="${tag#v}"

octaryn_ensure_dir "${tool_root}"
octaryn_ensure_dir "${dependency_build_root}"
octaryn_ensure_dir "${log_root}"

log() {
  printf '[%s] %s\n' "$(date --iso-8601=seconds)" "$*" | tee -a "${log_path}" >&2
}

tracy_source() {
  octaryn_fetch_git_tag tracy "https://github.com/wolfpld/tracy.git" "${tag}"
}

build_cmake_tool() {
  local source_dir="$1"
  local build_dir="$2"
  local target="$3"
  local binary_name="$4"
  local binary_path="${build_dir}/${binary_name}"

  if [[ -x "${binary_path}" ]]; then
    return
  fi

  log "building ${target} from ${source_dir}"
  octaryn_ensure_dir "${source_cache_root}"
  configure_args=(
    -DCMAKE_BUILD_TYPE=Release
    -DCMAKE_TOOLCHAIN_FILE="${toolchain_file}"
    -DFETCHCONTENT_BASE_DIR="${dependency_build_root}/${target}"
    -DCPM_MODULE_PATH="${dependency_build_root}/${target}/cpm-modules"
    -DCPM_DONT_CREATE_PACKAGE_LOCK=ON
  )
  if [[ "${target_platform}" != "linux" ]]; then
    configure_args+=(-DNO_ISA_EXTENSIONS=ON)
    configure_args+=(
      -DDOWNLOAD_GLFW=ON
      -DDOWNLOAD_FREETYPE=ON
      -DDOWNLOAD_LIBCURL=ON
      -DDOWNLOAD_PUGIXML=ON
      -DCURL_ENABLE_SSL=OFF
      -DCURL_DISABLE_LDAP=ON
      -DCURL_DISABLE_LDAPS=ON
    )
  fi
  if [[ "${target_platform}" == "windows" ]]; then
    configure_args+=(-DCMAKE_EXE_LINKER_FLAGS=-lws2_32)
  fi
  octaryn_ensure_dir "${dependency_build_root}/empty-pkgconfig"
  environment_args=(GIT_TERMINAL_PROMPT=0 CPM_SOURCE_CACHE="${source_cache_root}")
  if [[ "${target_platform}" != "linux" ]]; then
    environment_args+=(PKG_CONFIG_LIBDIR="${dependency_build_root}/empty-pkgconfig")
  fi
  env "${environment_args[@]}" \
    cmake -S "${source_dir}" -B "${build_dir}" "${configure_args[@]}" >>"${log_path}" 2>&1
  env "${environment_args[@]}" \
    cmake --build "${build_dir}" --target "${target}" --parallel "$(octaryn_host_core_count)" >>"${log_path}" 2>&1
}

download_release_asset() {
  local asset_name="$1"
  local expected_sha256="$2"
  local output_path="${release_cache_root}/${tag}/${asset_name}"
  octaryn_ensure_dir "$(dirname "${output_path}")"

  if [[ ! -f "${output_path}" ]]; then
    local url="https://github.com/wolfpld/tracy/releases/download/${tag}/${asset_name}"
    log "downloading Tracy release asset ${url}"
    curl -fL --retry 3 --retry-delay 2 -o "${output_path}.tmp" "${url}" >>"${log_path}" 2>&1
    mv "${output_path}.tmp" "${output_path}"
  fi

  if [[ -n "${expected_sha256}" ]]; then
    printf '%s  %s\n' "${expected_sha256}" "${output_path}" | sha256sum --check --status
  fi

  printf '%s\n' "${output_path}"
}

extract_windows_release() {
  local archive_path="$1"
  local extract_dir="${tool_root}/prebuilt/windows-${tracy_version}"
  local stamp_path="${extract_dir}/.extracted"
  if [[ -f "${stamp_path}" ]]; then
    printf '%s\n' "${extract_dir}"
    return
  fi

  rm -rf "${extract_dir}"
  octaryn_ensure_dir "${extract_dir}"
  python3 - "${archive_path}" "${extract_dir}" <<'PY'
from pathlib import Path
from zipfile import ZipFile
import sys

archive = Path(sys.argv[1])
destination = Path(sys.argv[2])
with ZipFile(archive) as zip_file:
    for member in zip_file.infolist():
        if member.is_dir():
            continue
        name = Path(member.filename).name
        if name not in {
            "tracy-profiler.exe",
            "tracy-capture.exe",
            "tracy-csvexport.exe",
            "tracy-import-chrome.exe",
            "tracy-import-fuchsia.exe",
            "tracy-update.exe",
        }:
            continue
        target = destination / name
        target.write_bytes(zip_file.read(member))
        target.chmod(0o755)
PY
  touch "${stamp_path}"
  printf '%s\n' "${extract_dir}"
}

stage_windows_prebuilt_tools() {
  local expected_sha256=""
  if [[ "${tracy_version}" == "0.13.1" ]]; then
    expected_sha256="ee6db1a7e71a12deb5973a8dbfdf9f36d3635bec0e0b31b1cc74f28de7dac4c9"
  fi

  local archive_path
  archive_path="$(download_release_asset "windows-${tracy_version}.zip" "${expected_sha256}")"
  local extract_dir
  extract_dir="$(extract_windows_release "${archive_path}")"

  octaryn_ensure_dir "${profiler_build_dir}"
  octaryn_ensure_dir "${capture_build_dir}"
  octaryn_ensure_dir "${csvexport_build_dir}"
  cp -f "${extract_dir}/tracy-profiler.exe" "${profiler_build_dir}/${profiler_binary}"
  cp -f "${extract_dir}/tracy-capture.exe" "${capture_build_dir}/${capture_binary}"
  cp -f "${extract_dir}/tracy-csvexport.exe" "${csvexport_build_dir}/${csvexport_binary}"
  chmod 755 "${profiler_build_dir}/${profiler_binary}" "${capture_build_dir}/${capture_binary}" "${csvexport_build_dir}/${csvexport_binary}"
}

build_tracy_tools() {
  exec 9>"${lock_path}"
  log "waiting for Tracy tool build lock"
  flock 9
  log "acquired Tracy tool build lock"

  if [[ "${target_platform}" == "windows" ]]; then
    stage_windows_prebuilt_tools
    return
  fi

  local source_root
  source_root="$(tracy_source)"
  build_cmake_tool "${source_root}/profiler" "${profiler_build_dir}" tracy-profiler "${profiler_binary}"
  build_cmake_tool "${source_root}/capture" "${capture_build_dir}" tracy-capture "${capture_binary}"
  build_cmake_tool "${source_root}/csvexport" "${csvexport_build_dir}" tracy-csvexport "${csvexport_binary}"
}

tracy_ui_connected() {
  ss -tnp 2>/dev/null | grep -E "(:${port}[[:space:]]|:${port}$)" | grep -q 'tracy-profiler'
}

tracy_client_listening() {
  ss -ltn 2>/dev/null | awk '{print $4}' | grep -Eq "(^|:)${port}$"
}

require_linux_host_tool_run() {
  local action="$1"
  if [[ "${target_platform}" == "linux" ]]; then
    return
  fi

  printf '[error] %s is only supported for the Linux host preset. Preset %s builds %s target Tracy tools; run those tools on the target platform.\n' \
    "${action}" "${preset}" "${target_platform}" >&2
  exit 2
}

case "${command}" in
  build)
    build_tracy_tools
    log "Tracy tools ready under ${tool_root}"
    ;;
  launch-profiler)
    require_linux_host_tool_run "Tracy profiler launch"
    build_tracy_tools
    if pgrep -x tracy-profiler >/dev/null 2>&1; then
      log "Tracy profiler is already running"
      exit 0
    fi
    log "launching Tracy profiler"
    setsid "${profiler_build_dir}/${profiler_binary}" >>"${log_path}" 2>&1 < /dev/null &
    log "Tracy profiler pid=$!"
    ;;
  capture)
    require_linux_host_tool_run "Tracy capture"
    build_tracy_tools
    if tracy_ui_connected; then
      printf '[error] Tracy UI is already connected to port %s; close it before capture.\n' "${port}" >&2
      exit 1
    fi
    if ! tracy_client_listening; then
      printf '[error] no Tracy client is listening on %s:%s. Start the client first.\n' "${address}" "${port}" >&2
      exit 1
    fi
    stamp="$(date +%Y%m%d-%H%M%S)"
    capture_path="${log_root}/tracy-${stamp}.tracy"
    log "capturing Tracy session to ${capture_path}"
    timeout_seconds=$((seconds + 15))
    timeout "${timeout_seconds}s" "${capture_build_dir}/${capture_binary}" \
      -o "${capture_path}" -a "${address}" -p "${port}" -f -s "${seconds}" | tee -a "${log_path}"
    if [[ "${export_capture}" == "1" ]]; then
      "${csvexport_build_dir}/${csvexport_binary}" -u "${capture_path}" >"${capture_path%.tracy}.zones.csv" || true
      "${csvexport_build_dir}/${csvexport_binary}" -m "${capture_path}" >"${capture_path%.tracy}.messages.csv" || true
    fi
    printf 'tracy_capture=%s\n' "${capture_path}"
    ;;
  print-profiler)
    build_tracy_tools
    printf '%s\n' "${profiler_build_dir}/${profiler_binary}"
    ;;
  print-capture)
    build_tracy_tools
    printf '%s\n' "${capture_build_dir}/${capture_binary}"
    ;;
  print-csvexport)
    build_tracy_tools
    printf '%s\n' "${csvexport_build_dir}/${csvexport_binary}"
    ;;
  *)
    usage >&2
    exit 2
    ;;
esac
