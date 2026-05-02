# Tracy Validation

Tracy validation applies to performance-sensitive runtime work.

## Target Use

- Server simulation ticks, job scheduling, chunk generation, persistence, and replication.
- Client frame pacing, rendering, GPU upload staging, streaming, and asset generation.
- Scheduler lanes for main thread, coordinator thread, and worker pool threads. Captures should show computation/gameplay work on workers, coordination on the coordinator thread, and presentation/platform handoff on the main thread.

## Active Tooling

Tracy is a first-class debug tool. Debug presets stage the wrapper through `octaryn_debug_tools` into `build/<preset>/tools/profiling/tracy_tool.sh`, with the source wrapper kept at `tools/profiling/tracy_tool.sh`.

```sh
tools/build/cmake_build.sh debug-linux --target octaryn_debug_tools
tools/build/cmake_build.sh debug-windows --target octaryn_debug_tools
tools/profiling/tracy_tool.sh --preset debug-linux build
tools/profiling/tracy_tool.sh --preset debug-linux launch-profiler
tools/profiling/tracy_tool.sh --preset debug-linux --seconds 10 capture
tools/profiling/tracy_tool.sh --preset debug-windows print-profiler
```

The wrapper stages target-native Tracy tools from the workspace-managed dependency cache. Windows uses the official Tracy release binary package when one exists for the selected tag; Linux builds from source with workspace-managed dependencies and all available cores. Tool logs stay under `logs/tools/tracy_tool.log`, and capture artifacts stay directly under `logs/tools/`.

Linux-host execution is only valid for the Linux preset. Windows presets still stage native profiler/capture/export binaries under `build/<preset>/tools/tracy/`, but those binaries must be launched on Windows.

Scheduler profiling is not accepted until a capture verifies at least two worker threads and scaling behavior on the current host.
