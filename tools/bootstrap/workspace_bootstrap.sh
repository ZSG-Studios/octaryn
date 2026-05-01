#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../tooling/tool_environment.sh
source "${script_dir}/../tooling/tool_environment.sh"

command="detect"
repo_url="${OCTARYN_REPOSITORY_URL:-}"
destination="${OCTARYN_WORKSPACE_DESTINATION:-}"
image="${OCTARYN_ARCH_IMAGE:-docker.io/archlinux:latest}"
container_name="${OCTARYN_BUILDER_NAME:-octaryn-arch-builder}"

usage() {
  cat <<'USAGE'
Usage: workspace_bootstrap.sh [options] <command>

Commands:
  detect        Print detected host platform and required external tools.
  clone         Clone the Octaryn workspace when --repo and --destination are set.
  podman-arch   Start an Arch Linux Podman builder mounted at the workspace.

Options:
  --repo <url>          Repository URL to clone.
  --destination <path>  Clone destination or mounted workspace path.
  --image <image>       Arch builder image. Default: docker.io/archlinux:latest.
  --name <name>         Podman container name. Default: octaryn-arch-builder.
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo)
      repo_url="$2"
      shift 2
      ;;
    --destination)
      destination="$2"
      shift 2
      ;;
    --image)
      image="$2"
      shift 2
      ;;
    --name)
      container_name="$2"
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

detect_host() {
  case "$(uname -s)" in
    Linux) printf 'linux\n' ;;
    Darwin) printf 'macos\n' ;;
    MINGW*|MSYS*|CYGWIN*) printf 'windows\n' ;;
    *) printf 'unknown\n' ;;
  esac
}

require_tool() {
  if ! command -v "$1" >/dev/null 2>&1; then
    printf '[error] missing required tool: %s\n' "$1" >&2
    return 1
  fi
}

case "${command}" in
  detect)
    host="$(detect_host)"
    printf 'host=%s\n' "${host}"
    printf 'workspace=%s\n' "${octaryn_workspace_root}"
    printf 'cores=%s\n' "$(octaryn_host_core_count)"
    if command -v podman >/dev/null 2>&1; then
      printf 'podman=%s\n' "$(command -v podman)"
    else
      printf 'podman=missing\n'
      case "${host}" in
        linux) printf 'install_hint=install podman with the host package manager\n' ;;
        macos|windows) printf 'install_hint=install Podman Desktop or podman machine, then rerun podman-arch\n' ;;
      esac
    fi
    ;;
  clone)
    require_tool git
    if [[ -z "${repo_url}" || -z "${destination}" ]]; then
      printf '[error] clone requires --repo and --destination.\n' >&2
      exit 2
    fi
    git clone "${repo_url}" "${destination}"
    ;;
  podman-arch)
    require_tool podman
    workspace_path="${destination:-${octaryn_workspace_root}}"
    if [[ ! -d "${workspace_path}" ]]; then
      printf '[error] workspace path does not exist: %s\n' "${workspace_path}" >&2
      exit 2
    fi
    podman pull "${image}"
    podman run --replace --name "${container_name}" -it \
      --volume "${workspace_path}:/workspace:Z" \
      --workdir /workspace \
      "${image}" \
      bash -lc 'pacman -Syu --noconfirm && pacman -S --noconfirm base-devel clang cmake ninja git python python-pyside6 dotnet-sdk podman && bash'
    ;;
  *)
    usage >&2
    exit 2
    ;;
esac
