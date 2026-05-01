#!/usr/bin/env sh
set -eu

preset="${1:-debug}"
exec cmake --preset "$preset"
