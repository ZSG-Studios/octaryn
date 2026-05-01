#!/usr/bin/env sh
set -eu

preset="${1:-debug-linux}"
shift || true
exec cmake --build --preset "$preset" --parallel "$(nproc)" "$@"
