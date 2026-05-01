# Old Architecture Map

`old-architecture/` is source material only. New work lands in owner roots.

## Owner Destinations

| Old source | Destination owner | Notes |
| --- | --- | --- |
| `old-architecture/source/app/` | `octaryn-client/Source/` | Window, input, local frame loop, overlays, and managed host edge. |
| `old-architecture/source/render/` | `octaryn-client/Source/Rendering/` | GPU upload, render resources, pipelines, scene presentation, UI rendering. |
| `old-architecture/source/shaders/` | `octaryn-client/Shaders/` plus shader tooling | Runtime shader sources are client-owned assets. |
| `old-architecture/source/world/edit/` | `octaryn-server/Source/World/` and shared command contracts | Server owns authoritative edit validation and execution. |
| `old-architecture/source/world/runtime/` | split between client presentation and server world authority | Query/render descriptor pieces are not shared implementation. |
| `old-architecture/source/world/chunks/` | split between server chunk authority and client mesh presentation | Storage, streaming, meshing, and upload must be separated. |
| `old-architecture/source/world/generation/` | `octaryn-server/Source/World/Generation/` plus basegame content rules | Execution is server-owned; high-level rules/content are basegame. |
| `old-architecture/source/runtime/jobs/` and `old-architecture/source/world/jobs/` | focused scheduler/job support under client/server owners plus `octaryn_native_jobs` when intentionally ported | New target is one main thread, one coordinator thread, and a scalable worker pool with at least two workers. Do not port old job code as a generic runtime root. |
| `old-architecture/source/core/persistence/` | `octaryn-server/Source/Persistence/` | Save ownership stays server-side. |
| `old-architecture/source/physics/` | `octaryn-server/Source/Physics/` plus client prediction copies | Server authority first. |
| `old-architecture/source/api/` | replaced by `octaryn-shared/` contracts and host-specific adapters | Do not recreate `Octaryn.Engine.Api`. |

## Removal Rules

- Do not move files into generic `engine/`, `runtime/`, `common`, or catch-all support folders.
- Do not port old job/threading helpers into module code. Thread creation and worker-pool ownership belong to host scheduler support only.
- If a file mixes client/server/basegame responsibilities, split it before porting behavior.
- Old target names such as `octaryn_engine_runtime` remain only under `old-architecture/` until that build is retired.
