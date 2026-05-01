#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../tooling/tool_environment.sh
source "${script_dir}/../tooling/tool_environment.sh"

preset="debug-linux"
tag="${OCTARYN_RENDERDOC_TAG:-v1.43}"
command="launch"
program=""

usage() {
  cat <<'USAGE'
Usage: renderdoc_tool.sh [options] <command>

Commands:
  build       Fetch and build project-local RenderDoc.
  launch      Build and launch qrenderdoc.
  capture     Launch a program under renderdoccmd capture.
  print-ui    Build if needed and print qrenderdoc path.
  print-cmd   Build if needed and print renderdoccmd path.

Options:
  --preset <name>   Active build preset. Default: debug-linux.
  --tag <tag>       RenderDoc git tag. Default: v1.43.
  --program <path>  Program to launch for capture.
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
    --program)
      program="$2"
      shift 2
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

tool_root="$(octaryn_tool_build_root "${preset}")/renderdoc-${tag}"
source_cache_root="$(octaryn_dependency_source_root)/cpm"
log_root="$(octaryn_tool_log_root)"
capture_root="${log_root}/renderdoc"
log_path="${log_root}/renderdoc_tool.log"
lock_path="${log_root}/renderdoc_tool.lock"

octaryn_ensure_dir "${tool_root}"
octaryn_ensure_dir "${capture_root}"

log() {
  printf '[%s] %s\n' "$(date --iso-8601=seconds)" "$*" | tee -a "${log_path}"
}

renderdoc_source() {
  octaryn_fetch_git_tag renderdoc "https://github.com/baldurk/renderdoc.git" "${tag}"
}

find_renderdoc_binary() {
  local name="$1"
  find "${tool_root}" -type f -perm -111 -name "${name}" | head -n 1
}

build_renderdoc() {
  exec 9>"${lock_path}"
  log "waiting for RenderDoc build lock"
  flock 9
  log "acquired RenderDoc build lock"
  if [[ "$(uname -m)" != "x86_64" ]]; then
    printf '[error] project-local RenderDoc Linux builds are only supported on x86_64 hosts.\n' >&2
    exit 1
  fi

  local source_root
  source_root="$(renderdoc_source)"
  if [[ -x "$(find_renderdoc_binary qrenderdoc)" && -x "$(find_renderdoc_binary renderdoccmd)" ]]; then
    return
  fi

  log "building RenderDoc ${tag} from ${source_root}"
  octaryn_ensure_dir "${source_cache_root}"
  env GIT_TERMINAL_PROMPT=0 CPM_SOURCE_CACHE="${source_cache_root}" \
    cmake -S "${source_root}" -B "${tool_root}" -DCMAKE_BUILD_TYPE=Release -DENABLE_QRENDERDOC=ON >>"${log_path}" 2>&1
  env GIT_TERMINAL_PROMPT=0 CPM_SOURCE_CACHE="${source_cache_root}" \
    cmake --build "${tool_root}" --parallel "$(octaryn_host_core_count)" >>"${log_path}" 2>&1
}

register_vulkan_layer() {
  local renderdoccmd_path
  renderdoccmd_path="$(find_renderdoc_binary renderdoccmd)"
  if [[ -z "${renderdoccmd_path}" || ! -x "${renderdoccmd_path}" ]]; then
    printf '[warning] renderdoccmd is missing; skipping Vulkan layer registration.\n' >&2
    return
  fi
  if "${renderdoccmd_path}" vulkanlayer --explain 2>&1 | grep -q 'not registered'; then
    log "registering project RenderDoc Vulkan layer for this user"
    "${renderdoccmd_path}" vulkanlayer --register --user
  fi
}

renderdoc_ui_path() {
  local path
  path="$(find_renderdoc_binary qrenderdoc)"
  if [[ -z "${path}" || ! -x "${path}" ]]; then
    printf '[error] qrenderdoc was not produced by the project RenderDoc build.\n' >&2
    exit 1
  fi
  printf '%s\n' "${path}"
}

renderdoc_cmd_path() {
  local path
  path="$(find_renderdoc_binary renderdoccmd)"
  if [[ -z "${path}" || ! -x "${path}" ]]; then
    printf '[error] renderdoccmd was not produced by the project RenderDoc build.\n' >&2
    exit 1
  fi
  printf '%s\n' "${path}"
}

case "${command}" in
  build)
    build_renderdoc
    register_vulkan_layer
    log "RenderDoc ready under ${tool_root}"
    ;;
  launch)
    build_renderdoc
    register_vulkan_layer
    if pgrep -x qrenderdoc >/dev/null 2>&1; then
      log "RenderDoc UI is already running"
      exit 0
    fi
    qrenderdoc_path="$(renderdoc_ui_path)"
    log "launching RenderDoc UI"
    setsid env QT_QPA_PLATFORM=xcb "${qrenderdoc_path}" >>"${log_path}" 2>&1 < /dev/null &
    log "RenderDoc pid=$!"
    ;;
  capture)
    build_renderdoc
    register_vulkan_layer
    if [[ -z "${program}" || ! -x "${program}" ]]; then
      printf '[error] --program must point to an executable client artifact.\n' >&2
      exit 2
    fi
    renderdoccmd_path="$(renderdoc_cmd_path)"
    stamp="$(date +%Y%m%d-%H%M%S)"
    capture_template="${capture_root}/${stamp}"
    log "capturing ${program} to ${capture_template}.rdc"
    env SDL_VIDEODRIVER=x11 QT_QPA_PLATFORM=xcb \
      OCTARYN_PRESENT_MODE="${OCTARYN_PRESENT_MODE:-immediate}" \
      OCTARYN_ACQUIRE_MODE="${OCTARYN_ACQUIRE_MODE:-late}" \
      OCTARYN_FRAMES_IN_FLIGHT="${OCTARYN_FRAMES_IN_FLIGHT:-2}" \
      OCTARYN_FPS_CAP="${OCTARYN_FPS_CAP:-0}" \
      OCTARYN_LOG_RENDER_PROFILE="${OCTARYN_LOG_RENDER_PROFILE:-1}" \
      "${renderdoccmd_path}" capture \
      -d "${octaryn_workspace_root}" \
      -c "${capture_template}" \
      --opt-disallow-vsync \
      --opt-api-validation \
      "${program}" | tee -a "${log_path}"
    ;;
  print-ui)
    build_renderdoc
    renderdoc_ui_path
    ;;
  print-cmd)
    build_renderdoc
    renderdoc_cmd_path
    ;;
  *)
    usage >&2
    exit 2
    ;;
esac
