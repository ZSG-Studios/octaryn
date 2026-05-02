#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=tool_environment.sh
source "${script_dir}/tool_environment.sh"

command="setup"
mirror="${OCTARYN_LINUX_ARM64_MIRROR:-http://mirror.archlinuxarm.org}"
sysroot_root="${OCTARYN_LINUX_ARM64_SYSROOT_ROOT:-${octaryn_workspace_root}/build/dependencies/sysroots/linux-arm64}"
root_dir="${OCTARYN_LINUX_ARM64_SYSROOT:-${sysroot_root}/root}"
cache_dir="${sysroot_root}/packages"
db_dir="${sysroot_root}/pacman-db"
config_path="${sysroot_root}/pacman.conf"
manifest_path="${sysroot_root}/packages.txt"

base_packages=(
  glibc
  gcc
  gcc-libs
  linux-api-headers
  zlib
  xz
  zstd
  bzip2
  libffi
  libunwind
)

client_packages=(
  alsa-lib
  dbus
  expat
  flac
  fontconfig
  freetype2
  fribidi
  graphite
  harfbuzz
  jack2
  libdecor
  libdrm
  libglvnd
  libice
  libjpeg-turbo
  libogg
  libpng
  libpulse
  libsm
  libsndfile
  libthai
  libusb
  libvorbis
  libx11
  libxau
  libxcb
  libxcursor
  libxdmcp
  libxext
  libxfixes
  libxi
  libxkbcommon
  libxrandr
  libxrender
  libxss
  libxtst
  mesa
  opus
  pipewire
  sndio
  vulkan-headers
  vulkan-icd-loader
  wayland
  wayland-protocols
  xcb-proto
  xorgproto
)

usage() {
  cat <<'USAGE'
Usage: linux_arm64_sysroot.sh <command>

Commands:
  setup   Download and extract the workspace-managed Linux ARM64 sysroot.
  print   Print the sysroot path.
  clean   Remove the generated sysroot root, package cache, and pacman database.

Environment:
  OCTARYN_LINUX_ARM64_MIRROR       Arch Linux ARM mirror. Default: http://mirror.archlinuxarm.org
  OCTARYN_LINUX_ARM64_SYSROOT_ROOT Workspace sysroot root. Default: build/dependencies/sysroots/linux-arm64
  OCTARYN_LINUX_ARM64_SYSROOT      Extracted target root. Default: <sysroot-root>/root
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    setup|print|clean)
      command="$1"
      shift
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      printf '[error] unsupported linux arm64 sysroot command: %s\n' "$1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

require_tool() {
  if ! command -v "$1" >/dev/null 2>&1; then
    printf '[error] missing required tool: %s\n' "$1" >&2
    exit 2
  fi
}

write_pacman_config() {
  octaryn_ensure_dir "${sysroot_root}"
  cat >"${config_path}" <<EOF
[options]
Architecture = aarch64
RootDir = ${root_dir}
DBPath = ${db_dir}
CacheDir = ${cache_dir}
HookDir = ${sysroot_root}/hooks
GPGDir = ${sysroot_root}/gnupg
LogFile = ${sysroot_root}/pacman.log
HoldPkg = pacman glibc
SigLevel = Never
LocalFileSigLevel = Never

[core]
Server = ${mirror}/\$arch/\$repo

[extra]
Server = ${mirror}/\$arch/\$repo

[alarm]
Server = ${mirror}/\$arch/\$repo
EOF
}

extract_packages() {
  local package
  find "${cache_dir}" -maxdepth 1 -type f -name '*.pkg.tar.*' -print0 |
    sort -z |
    while IFS= read -r -d '' package; do
      bsdtar -xpf "${package}" -C "${root_dir}" \
        --exclude .BUILDINFO \
        --exclude .INSTALL \
        --exclude .MTREE \
        --exclude .PKGINFO \
        --exclude .CHANGELOG
    done
}

write_manifest() {
  {
    printf '# Octaryn Linux ARM64 sysroot\n'
    printf 'mirror=%s\n' "${mirror}"
    printf 'root=%s\n' "${root_dir}"
    printf 'created_at=%s\n' "$(date --iso-8601=seconds)"
    printf '\n[packages]\n'
    find "${cache_dir}" -maxdepth 1 -type f -name '*.pkg.tar.*' -printf '%f\n' | sort
  } >"${manifest_path}"
}

case "${command}" in
  print)
    printf '%s\n' "${root_dir}"
    ;;
  clean)
    rm -rf "${root_dir}" "${cache_dir}" "${db_dir}" "${manifest_path}" "${config_path}"
    ;;
  setup)
    require_tool pacman
    require_tool bsdtar
    require_tool zstd
    require_tool fakeroot

    octaryn_ensure_dir "${root_dir}"
    octaryn_ensure_dir "${cache_dir}"
    octaryn_ensure_dir "${db_dir}"
    octaryn_ensure_dir "${sysroot_root}/hooks"
    octaryn_ensure_dir "${sysroot_root}/gnupg"
    write_pacman_config

    fakeroot -- pacman --config "${config_path}" --noconfirm -Syw --needed "${base_packages[@]}" "${client_packages[@]}" >&2
    extract_packages
    write_manifest
    printf '%s\n' "${root_dir}"
    ;;
esac
