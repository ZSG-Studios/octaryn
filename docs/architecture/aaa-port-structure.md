# AAA Port Structure Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans before implementing port tasks. This file defines structure and boundaries only.

**Goal:** Port the existing Octaryn runtime into strict client, server, basegame, shared, and native-runtime layers without keeping a general `engine` folder or doing a behavior rewrite.

**Architecture:** `old-architecture/` is source material for the port, not the destination. Client owns presentation and rendering, server owns authority and persistence, basegame owns game rules and content, and shared owns contracts and value types. Existing focused support libraries should stay as build/internal libs under the owner that uses them instead of becoming a new generic runtime root.

**Tech Stack:** C++23/C17, CMake, SDL3 GPU/Vulkan, .NET 10, C# latest, LiteNetLib/LiteEntitySystem for transport-oriented projects only, RenderDoc, Tracy, targeted runtime/profiling validation.

---

## Current State

- The active repository root is `/home/zacharyr/octaryn-workspace`.
- The old `octaryn-engine/` tree is deleted from the working tree and preserved as untracked `old-architecture/`.
- `octaryn-client/` and `octaryn-server/` currently exist as blank roots.
- `octaryn-basegame/` contains the current managed game bootstrap and migration map.
- `tools/build/layout.sh` still points native builds at `old-architecture/`.
- Reference material lives under `refrances/`, including Minecraft 26.1.2, Iris, and Complementary Reimagined.

## Hard Boundaries

- No new top-level `engine/` or `octaryn-engine/` folder.
- No new `Octaryn.Engine.*` namespaces.
- Do not port by copying the monolith shape into a new name.
- Do not rewrite behavior first; preserve behavior by moving it into the right owner.
- Do not put networking packages in `octaryn-basegame`.
- Do not put GPU upload, mesh upload, render descriptors, windowing, audio, or UI in server.
- Do not put authoritative world edits, save ownership, or server simulation in client.
- Do not put product-specific game rules in shared or runtime.

## Destination Roots

```text
octaryn-client/
octaryn-server/
octaryn-basegame/
octaryn-shared/
tools/
cmake/
docs/
refrances/
old-architecture/
build/<owner>/
logs/<owner>/
```

## Client Ownership

Client owns the playable local application and every presentation concern.

```text
octaryn-client/
  CMakeLists.txt
  Octaryn.Client.csproj
  Source/
    Native/
    Managed/
    Libraries/
    App/
    Audio/
    Display/
    Input/
    Managed/
    Overlay/
    Player/
    Runtime/
      Render/
      Startup/
    Window/
    Rendering/
      Atlas/
      Buffers/
      Pipelines/
      Postprocess/
      Resources/
      Scene/
      Shaders/
      Ui/
      Visibility/
      World/
    WorldPresentation/
  Assets/
    Icons/
    Textures/
    Ui/
  Shaders/
  Tools/
  Data/
```

Port source candidates:

- `old-architecture/source/app/`
- `old-architecture/source/render/`
- `old-architecture/source/shaders/`
- client-side pieces of `old-architecture/source/world/chunks/build_mesh.*`
- client-side pieces of `old-architecture/source/world/chunks/upload*.cpp`
- client-side pieces of `old-architecture/source/world/runtime/render_descriptors.cpp`

## Server Ownership

Server owns authority, persistence, validation, simulation, and transport hosting.

```text
octaryn-server/
  Octaryn.Server.csproj
  Source/
    Native/
    Managed/
    Libraries/
    Program.cs
    ServerHost.cs
    Tick/
    Simulation/
    World/
      Blocks/
      Chunks/
      Generation/
      Lighting/
      Queries/
    Persistence/
    Networking/
    Physics/
    Validation/
  Assets/
  Data/
  Shaders/
  Tools/
```

Port source candidates:

- `old-architecture/source/world/edit/`
- server-side pieces of `old-architecture/source/world/runtime/`
- server-side pieces of `old-architecture/source/world/chunks/`
- `old-architecture/source/world/jobs/`
- `old-architecture/source/world/generation/`
- `old-architecture/source/physics/`
- server-owned pieces of `old-architecture/source/core/persistence/`

## Basegame Ownership

Basegame owns content, gameplay rules, and default game behavior. It should be usable by both client and server through explicit APIs.

```text
octaryn-basegame/
  Octaryn.Game.csproj
  Source/
    Native/
    Managed/
    Libraries/
    GameContext.cs
    GameExports.cs
    ManagedGameTag.cs
    Content/
      Biomes/
      Blocks/
      Features/
      Fluids/
      Items/
      LootTables/
      Materials/
      Recipes/
      Tags/
    Gameplay/
      Entities/
      Input/
      Interaction/
      Inventory/
      Physics/
      Player/
      Time/
      World/
    World/
      Blocks/
      Chunks/
      Generation/
      Lighting/
      Persistence/
    Networking/
      Commands/
      Snapshots/
  Assets/
    Atlases/
    Blockstates/
    Models/
    Shaders/
    Textures/
  Data/
    Blocks/
    Items/
    Materials/
    Recipes/
    Tags/
    Worldgen/
  Shaders/
  Tools/
```

Port source candidates:

- `octaryn-basegame/Source/Gameplay/GameplayMigrationMap.cs`
- `old-architecture/source/world/block/`
- game-rule portions of `old-architecture/source/app/player/`
- game-rule portions of `old-architecture/source/world/edit/`
- world/content definitions from `old-architecture/source/world/generation/`

## Shared Ownership

Shared owns small contracts and value types used across client, server, and basegame. It must not own runtime policy, rendering, persistence implementation, or gameplay behavior.

```text
octaryn-shared/
  Octaryn.Shared.csproj
  Source/
    Native/
    Managed/
    Libraries/
    Time/
    World/
    Networking/
    Math/
    Diagnostics/
  Assets/
  Data/
  Shaders/
  Tools/
```

Port source candidates:

- `old-architecture/source/world/direction.h`
- value-type pieces of `old-architecture/source/world/block/block.h`
- snapshot/command shapes from `old-architecture/source/api/`
- time value types from `old-architecture/source/core/world_time/`

## Support Libraries

Do not create a generic runtime root. The old native tree already has focused support libs for logging, diagnostics, memory, jobs, dependency wrappers, profiling, and shader tooling. During the port, keep those as small build targets owned by the layer that needs them.

```text
cmake/
tools/
old-architecture/cmake/
old-architecture/tools/
old-architecture/tools/build/
build/client/
build/server/
build/basegame/
build/shared/
build/tools/
build/dependencies/
build/old-architecture/
logs/client/
logs/server/
logs/basegame/
logs/shared/
logs/build/
logs/tools/
logs/old-architecture/
octaryn-client/Source/Runtime/
octaryn-client/Source/Managed/
octaryn-server/Source/Tick/
octaryn-server/Source/Simulation/
octaryn-shared/Source/Diagnostics/
```

Port source candidates:

- `old-architecture/source/core/log.*` -> focused diagnostics/logging lib used by client/server.
- `old-architecture/source/core/memory_mimalloc.*` -> native build support linked where needed.
- `old-architecture/source/core/crash_diagnostics.*` -> diagnostics support, not a product root.
- `old-architecture/source/runtime/jobs/` -> client/server job support or server simulation scheduler depending usage.
- native managed-host bridge pieces -> client host or shared contracts after renaming away from engine API names.

## Old Architecture Tooling

Old build helpers, old CMake modules, old desktop helper tools, and old profiling wrappers stay under `old-architecture/` until they are intentionally ported.

The old atlas builder is basegame-specific content tooling and belongs at `octaryn-basegame/Tools/build_atlas_from_pack.py`.

Active root `cmake/` and `tools/` are reserved for new architecture support only. Generated outputs should be owner-partitioned under `build/<owner>/` and `logs/<owner>/`.

## Build Target Names

```text
octaryn_client
octaryn_client_assets
octaryn_client_shaders
octaryn_client_bundle
octaryn_server
octaryn_server_bundle
octaryn_basegame
octaryn_basegame_assets
octaryn_shared
octaryn_native_logging
octaryn_native_diagnostics
octaryn_native_memory
octaryn_native_jobs
```

## Validation

- Do not use smoke tests unless explicitly requested.
- Do not run `ctest` unless explicitly requested.
- Use direct runtime launches, targeted benchmarks, RenderDoc captures, Tracy captures, and focused logs.
- For reference-parity work, inspect Minecraft, Iris, and Complementary Reimagined before implementing behavior.

## Phase Order

1. Create blank structure and plan files.
2. Rename managed API away from `Octaryn.Engine.Api` into shared contracts and client/server host contracts.
3. Split build target names away from `octaryn_engine_*`.
4. Port shared value contracts.
5. Port server-authoritative world and persistence.
6. Port client presentation, windowing, rendering, shaders, and uploads.
7. Port basegame content and gameplay rules.
8. Wire client-server transport through shared message contracts.
9. Validate with runtime/profiling paths.
