#!/usr/bin/env sh
set -eu

preset="${1:-debug-linux}"
exec cmake --preset "$preset"
