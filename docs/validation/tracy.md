# Tracy Validation

Tracy validation applies to performance-sensitive runtime work.

## Target Use

- Server simulation ticks, job scheduling, chunk generation, persistence, and replication.
- Client frame pacing, rendering, GPU upload staging, streaming, and asset generation.
- Scheduler lanes for main thread, coordinator thread, and worker pool threads. Captures should show computation/gameplay work on workers, coordination on the coordinator thread, and presentation/platform handoff on the main thread.

## Port Status

Old Tracy/profiling wrappers remain under `old-architecture/` until they are ported into root `tools/` with logs routed away from old monolith paths.

Scheduler profiling is not accepted until a capture verifies at least two worker threads and scaling behavior on the current host.
