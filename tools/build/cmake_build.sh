#!/usr/bin/env sh
set -eu

preset="${1:-debug}"
shift || true
exec cmake --build --preset "$preset" "$@"
