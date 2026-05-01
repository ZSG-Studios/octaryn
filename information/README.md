# Octaryn Workspace

This workspace is being ported into separate owner modules instead of a monolithic engine tree. The end goal is unchanged: native C/C++ core first, with C# ECS/gameplay and client/server networking intentionally used where they fit best.

## Active Layout

- `octaryn-client/`: presentation, windowing, input, rendering, overlays, local prediction, and the managed host export edge.
- `octaryn-server/`: authority, simulation, validation, persistence, networking host code, and server ticks.
- `octaryn-shared/`: implementation-free contracts, IDs, snapshots, commands, manifests, capability IDs, and module validation policy.
- `octaryn-basegame/`: bundled default game module with high-level gameplay, content, data, assets, and basegame-specific tools.
- `cmake/`: new-architecture CMake policy split into shared helpers, owner targets, dependency scaffolds, platform modules, and toolchains.
- `tools/`: repo-wide developer tooling for the new architecture.
- `tools/ui/workspace_control_app.py`: active Python workspace control UI for configure/build/validate/probe actions.
- `old-architecture/`: old source material used during the port. It is not the destination layout.

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

The root helper scripts wrap the same presets:

```sh
./tools/build/cmake_configure.sh debug-linux
./tools/build/cmake_build.sh debug-linux --target octaryn_all
```

Run individual owner launch probes through the public helper targets:

```sh
tools/build/cmake_build.sh debug-linux --target octaryn_run_client_launch_probe
tools/build/cmake_build.sh debug-linux --target octaryn_run_server_launch_probe
```

Managed projects can also be built directly:

```sh
dotnet restore Octaryn.DotNet.sln
dotnet build Octaryn.DotNet.sln --no-restore -maxcpucount
```

Run the managed validation probes directly when you need the same checks CMake runs:

```sh
dotnet run --project tools/validation/Octaryn.ModuleManifestProbe/Octaryn.ModuleManifestProbe.csproj --configuration Debug -- octaryn-basegame
dotnet run --project tools/validation/Octaryn.SchedulerProbe/Octaryn.SchedulerProbe.csproj --configuration Debug
dotnet run --project tools/validation/Octaryn.OwnerModuleValidationProbe/Octaryn.OwnerModuleValidationProbe.csproj --configuration Debug
```

## Outputs

- CMake driver state lives under `build/<preset>/cmake/`.
- Owner products from CMake live under `build/<preset>/<owner>/`.
- Default direct MSBuild output lives under `build/debug-linux/<owner>/managed/` and `build/debug-linux/<owner>/managed-obj/`.
- Owner logs and CMake-driven binary logs live under `logs/<owner>/`.

Old monolith outputs under `build/octaryn-engine/` are not part of the active build layout. Old target names under `old-architecture/` are source material only until intentionally ported.

## Module Policy

Game modules compile against `octaryn-shared` plus explicitly approved packages and framework API groups. Basegame is the bundled module and follows the same validation path as future modules or mods.

Host-only packages such as `LiteNetLib` and `LiteEntitySystem` belong only in client/server transport implementation. Shared contracts stay package-free, and basegame/module code must not use native bridges, filesystem/network/process APIs, reflection, runtime code generation, or direct host internals unless a future allowlist explicitly grants a narrow API.

C/C++ owners may call managed ECS/gameplay or networking through explicit owner bridges. Modules never receive bridge internals, unmanaged exports, native service handles, or direct client/server implementation access.

## Threading Model

The target host owns all threading: one main thread, one coordinator thread, and a worker pool with at least two workers that can scale to available cores. Computation systems and gameplay logic should run through coordinator-scheduled worker jobs. Modules use host scheduling APIs; they do not create raw threads, private task schedulers, timers, or worker pools.
