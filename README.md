# Octaryn

Octaryn is being ported from a monolithic engine tree into a clean owner-split platform. The target is a native C/C++ core first, with managed C# gameplay and networking used only where the boundary is explicit and validated.

The active work is the AAA modular port: client presentation, server authority, shared contracts, and the bundled basegame are separate owners. `old-architecture/` remains source material only.

## Repository Layout

- `octaryn-client/`: windowing, input, rendering, shaders, overlays, client prediction, and client host integration.
- `octaryn-server/`: authoritative simulation, validation, persistence, server ticks, replication, and transport hosting.
- `octaryn-shared/`: implementation-free contracts, IDs, commands, snapshots, module manifests, capability IDs, host API IDs, scheduling contracts, and validation policy.
- `octaryn-basegame/`: bundled default game module with gameplay rules, content declarations, assets, data, and basegame-specific tools.
- `tools/`: repo-wide build, validation, profiling, launch, and developer operations.
- `cmake/`: build policy, owner target construction, dependency wrappers, platform facts, and toolchains.
- `docs/`: architecture, migration, validation, and GitHub Pages documentation.
- `old-architecture/`: old implementation source material used for the port. It is not the destination architecture.

No new top-level `engine/`, `octaryn-engine/`, generic `runtime/`, `common`, `helpers`, or catch-all owner is part of the target layout.

## Documentation

- [GitHub Pages entry](docs/index.md)
- [API guide](docs/api/index.md)
- [API examples](docs/api/examples/index.md)
- [AAA port structure](docs/architecture/aaa-port-structure.md)
- [Old architecture map](docs/migration/old-architecture-map.md)
- [Build matrix](docs/validation/build-matrix.md)

When GitHub Pages is enabled, the public documentation site is served from `docs/` on `main`.

## Build

Configure and build the new owner-target scaffold:

```sh
cmake --preset debug-linux
tools/build/cmake_build.sh debug-linux
```

Build a specific owner target:

```sh
tools/build/cmake_build.sh debug-linux --target octaryn_client_bundle
tools/build/cmake_build.sh debug-linux --target octaryn_server
tools/build/cmake_build.sh debug-linux --target octaryn_basegame
tools/build/cmake_build.sh debug-linux --target octaryn_shared
```

Run owner launch probes through the public helper targets:

```sh
tools/build/cmake_build.sh debug-linux --target octaryn_run_client_launch_probe
tools/build/cmake_build.sh debug-linux --target octaryn_run_server_launch_probe
```

Run managed validation probes directly when you need the same checks CMake uses:

```sh
dotnet run --project tools/validation/Octaryn.ModuleManifestProbe/Octaryn.ModuleManifestProbe.csproj --configuration Debug -- octaryn-basegame
dotnet run --project tools/validation/Octaryn.SchedulerProbe/Octaryn.SchedulerProbe.csproj --configuration Debug
dotnet run --project tools/validation/Octaryn.OwnerModuleValidationProbe/Octaryn.OwnerModuleValidationProbe.csproj --configuration Debug
```

Windows is a Linux-hosted cross-build path. Use the Linux launcher and `tools/build/podman_build.sh` for `debug-windows` or `release-windows`.

## Output Layout

- CMake driver state: `build/<preset>/cmake/`
- Owner products: `build/<preset>/<owner>/`
- Managed outputs: `build/<preset>/<owner>/managed/`
- Managed intermediates: `build/<preset>/<owner>/managed-obj/`
- Native binaries: `build/<preset>/<owner>/native/bin/`
- Native libraries: `build/<preset>/<owner>/native/lib/`
- Logs: `logs/<owner>/`, `logs/build/`, and `logs/tools/`

Old monolith outputs under `build/octaryn-engine/` are not part of the active build layout.

## API Direction

`octaryn-shared/` is the API boundary. It exposes contracts that client, server, and game modules can share without leaking implementation:

- module manifests and validation reports
- capability and host API IDs
- host scheduling declarations
- frame, tick, and time contracts
- command and snapshot message shapes
- world IDs, positions, block edits, and chunk snapshots

Game modules compile against `octaryn-shared` plus explicitly approved package and framework API groups. They do not receive direct filesystem, networking, process, reflection, native interop, raw threading, or client/server implementation access unless a future allowlist grants a narrow contract.

Basegame is the first bundled game module, not a privileged internal bucket. Future games and mods should use the same manifest, capability, scheduling, and validation path.

## Threading Model

The host owns all threading:

- one main thread
- one coordinator thread
- a worker pool with at least two workers that can scale to available cores

Gameplay systems run through coordinator-scheduled work and declared read/write access. Modules must not create raw threads, private schedulers, timers, or worker pools.

## Porting Rules

Each port pass starts by mapping old files to explicit destination owners:

- client presentation/rendering stays in `octaryn-client/`
- server authority, simulation, persistence, and validation stay in `octaryn-server/`
- product gameplay and content stay in `octaryn-basegame/`
- contracts stay implementation-free in `octaryn-shared/`
- repo-wide developer operations stay in `tools/`
- build policy and toolchains stay in `cmake/`

Behavior should be preserved unless a boundary or API change is required. Temporary compatibility paths, duplicate old wrappers, and dead code should be removed as each slice is ported.
