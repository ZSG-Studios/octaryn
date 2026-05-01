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
  build             Build Tracy profiler, capture, and CSV export tools.
  launch-profiler  Build and launch the Tracy profiler UI.
  capture          Capture a Tracy session from the running client.
  print-profiler   Build if needed and print the profiler executable path.
  print-capture    Build if needed and print the capture executable path.
  print-csvexport  Build if needed and print the CSV export executable path.

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
source_cache_root="$(octaryn_dependency_source_root)/cpm"
log_root="$(octaryn_tool_log_root)"
capture_root="${log_root}/tracy"
log_path="${log_root}/tracy_tool.log"
lock_path="${log_root}/tracy_tool.lock"
profiler_build_dir="${tool_root}/profiler"
capture_build_dir="${tool_root}/capture"
csvexport_build_dir="${tool_root}/csvexport"

octaryn_ensure_dir "${tool_root}"
octaryn_ensure_dir "${capture_root}"

log() {
  printf '[%s] %s\n' "$(date --iso-8601=seconds)" "$*" | tee -a "${log_path}"
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
  env GIT_TERMINAL_PROMPT=0 CPM_SOURCE_CACHE="${source_cache_root}" \
    cmake -S "${source_dir}" -B "${build_dir}" -DCMAKE_BUILD_TYPE=Release >>"${log_path}" 2>&1
  env GIT_TERMINAL_PROMPT=0 CPM_SOURCE_CACHE="${source_cache_root}" \
    cmake --build "${build_dir}" --target "${target}" --parallel "$(octaryn_host_core_count)" >>"${log_path}" 2>&1
}

build_tracy_tools() {
  exec 9>"${lock_path}"
  log "waiting for Tracy tool build lock"
  flock 9
  log "acquired Tracy tool build lock"
  local source_root
  source_root="$(tracy_source)"
  build_cmake_tool "${source_root}/profiler" "${profiler_build_dir}" tracy-profiler tracy-profiler
  build_cmake_tool "${source_root}/capture" "${capture_build_dir}" tracy-capture tracy-capture
  build_cmake_tool "${source_root}/csvexport" "${csvexport_build_dir}" tracy-csvexport tracy-csvexport
}

tracy_ui_connected() {
  ss -tnp 2>/dev/null | grep -E "(:${port}[[:space:]]|:${port}$)" | grep -q 'tracy-profiler'
}

tracy_client_listening() {
  ss -ltn 2>/dev/null | awk '{print $4}' | grep -Eq "(^|:)${port}$"
}

case "${command}" in
  build)
    build_tracy_tools
    log "Tracy tools ready under ${tool_root}"
    ;;
  launch-profiler)
    build_tracy_tools
    if pgrep -x tracy-profiler >/dev/null 2>&1; then
      log "Tracy profiler is already running"
      exit 0
    fi
    log "launching Tracy profiler"
    setsid "${profiler_build_dir}/tracy-profiler" >>"${log_path}" 2>&1 < /dev/null &
    log "Tracy profiler pid=$!"
    ;;
  capture)
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
    capture_path="${capture_root}/${stamp}.tracy"
    log "capturing Tracy session to ${capture_path}"
    timeout_seconds=$((seconds + 15))
    timeout "${timeout_seconds}s" "${capture_build_dir}/tracy-capture" \
      -o "${capture_path}" -a "${address}" -p "${port}" -f -s "${seconds}" | tee -a "${log_path}"
    if [[ "${export_capture}" == "1" ]]; then
      "${csvexport_build_dir}/tracy-csvexport" -u "${capture_path}" >"${capture_path%.tracy}.zones.csv" || true
      "${csvexport_build_dir}/tracy-csvexport" -m "${capture_path}" >"${capture_path%.tracy}.messages.csv" || true
    fi
    printf 'tracy_capture=%s\n' "${capture_path}"
    ;;
  print-profiler)
    build_tracy_tools
    printf '%s\n' "${profiler_build_dir}/tracy-profiler"
    ;;
  print-capture)
    build_tracy_tools
    printf '%s\n' "${capture_build_dir}/tracy-capture"
    ;;
  print-csvexport)
    build_tracy_tools
    printf '%s\n' "${csvexport_build_dir}/tracy-csvexport"
    ;;
  *)
    usage >&2
    exit 2
    ;;
esac
