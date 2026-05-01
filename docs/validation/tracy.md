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
tools/profiling/tracy_tool.sh --preset debug-linux build
tools/profiling/tracy_tool.sh --preset debug-linux launch-profiler
tools/profiling/tracy_tool.sh --preset debug-linux --seconds 10 capture
```

The wrapper fetches/builds project-local Tracy from the workspace-managed dependency cache, uses all available cores, writes tool logs under `logs/tools/tracy_tool.log`, and writes capture artifacts under `logs/tools/tracy/`.

Scheduler profiling is not accepted until a capture verifies at least two worker threads and scaling behavior on the current host.
