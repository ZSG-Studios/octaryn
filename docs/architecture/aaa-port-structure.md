# AAA Port Structure Plan

> **For agentic workers:** Use the maximum available agents/subagents where available. Inspect first, make a brief source-to-destination plan, then execute. This file defines structure and boundaries only.

**Goal:** Same end goal: port Octaryn into strict client, server, basegame, shared, and focused native support layers with a native C/C++ core first, no general `engine` folder, and no behavior rewrite. C# ECS/gameplay and client/server networking are intentional tools where they fit best. The platform should load game modules through explicit APIs and reject incompatible modules before they run.

**Architecture:** `old-architecture/` is source material only, never an active implementation target. Client owns presentation and rendering, server owns authority and persistence, basegame is the first bundled game module with high-level mechanics, content, and assets through API contracts, and shared owns contracts, value types, module manifests, compatibility rules, and validation-facing APIs. Existing focused support libraries should stay as build/internal libs under the owner that uses them instead of becoming a new generic runtime root.

**Tech Stack:** C++23/C17, CMake, SDL3 GPU/Vulkan, .NET 10, C# latest, LiteNetLib/LiteEntitySystem for transport-oriented projects only, RenderDoc, Tracy, targeted runtime/profiling validation.

---

## Current State

- The active repository root is `/home/zacharyr/octaryn-workspace`.
- The old `octaryn-engine/` tree is deleted from the working tree and preserved as tracked `old-architecture/` source material.
- `octaryn-client/`, `octaryn-server/`, `octaryn-shared/`, and `octaryn-basegame/` have real owner project files.
- `octaryn-client/` owns the managed native ABI export edge through `ClientHostExports` and a client-owned `BasegameModuleActivator`; `octaryn-server/` owns the server host export edge, server module activation, and server-side module validation.
- `octaryn-basegame/` contains the current managed game context and basegame module registration. The old gameplay migration map now lives under `docs/migration/` so active basegame source stays focused on content and gameplay implementation.
- `octaryn-shared/` now contains timing/input-only host-frame contracts, a narrow request command contract, module manifests, dependency/content/asset/compatibility declaration records, exposed host API IDs, module capability IDs, module runtime/build package allowlists, framework API group allowlists, sandbox denied-group IDs, and manifest validation for duplicate, blank, unexposed API, unapproved capability, unapproved package, unapproved framework API group, and malformed declaration requests.
- Root MSBuild policy rejects unknown project owners, package references in `octaryn-shared`, host-only packages outside client/server, unapproved direct module packages, analyzer packages with runtime assets, unapproved resolved runtime/analyzer packages for module owners, and unclassified packages in module `project.assets.json`.
- Active `cmake/` has a concrete new-architecture scaffold: root `CMakeLists.txt`, root `CMakePresets.json`, owner CMake modules, dependency policy placeholders, platform modules, toolchain files, and new `tools/build` wrappers. It builds managed owner targets, native owner aggregates, hostfxr bridge facades, launch probes, bundles, and validation targets without porting the old monolith.
- `information/` is a top-level informational/documentation-only folder. It is not a source, build, module, or runtime owner.
- `old-architecture/.octaryn-cache/` may contain ignored generated/reference cache files. Do not treat cache content as tracked source material or a migration source unless it is explicitly promoted.
- `old-architecture/tools/build/layout.sh` still points native builds at `old-architecture/`; active root `tools/build/` is reserved for intentionally ported new-architecture build helpers.
- Reference material lives under `refrances/`, including Minecraft 26.1.2, Iris, and Complementary Reimagined.

## Phase 0 Blockers

These are current transitional violations and hard blockers. Do not add or expand module-facing behavior that depends on them. Work touching these areas must remove the blocker, add real enforcement, or keep the affected code non-activated.

- Keep `octaryn-basegame` on `octaryn-shared` contracts and do not reintroduce a reference to `old-architecture/source/api/Octaryn.Engine.Api.csproj`.
- Keep unmanaged managed-host exports in host-owned code such as `octaryn-client`, not `octaryn-basegame`.
- Keep `AllowUnsafeBlocks` out of `octaryn-basegame`. Module code must not keep unsafe/native bridge access as a normal permission.
- Keep unsafe native function-pointer bridges out of `octaryn-shared`; shared exposes safe module contracts such as manifests, module frame contexts, module command request facades, declarations, and capability handles. Raw host frame/command ABI types are owner/internal only.
- Keep host-only package references out of `octaryn-basegame`; `LiteNetLib` and `LiteEntitySystem` belong only in client/server transport projects when transport is wired.
- Keep `Octaryn.Client.csproj`, `Octaryn.Server.csproj`, and `Octaryn.Shared.csproj` as real SDK project definitions with owner-routed outputs.
- Keep pre-load manifest validation file-backed: module content declarations must point at existing `Data/` records, asset declarations must point at existing `Assets/` or `Shaders/` files, and undeclared content/assets must fail validation.
- Keep resolved transitive package validation enforced for basegame and extend the same runtime/build-analyzer allowlist model to external game modules and mods when those package projects are introduced.
- Keep source-level framework API allowlist enforcement and post-build binary metadata inspection active for namespaces/types/members. External binary-only modules still need artifact identity/package/content binding before they are trusted.
- Keep the owner thread contract enforced before porting heavy compute systems: one main thread, one coordinator thread, and a scalable worker pool with at least two workers. All computation and gameplay logic must be scheduled through this pool or through host APIs backed by this pool, with runtime/profiling validation expanded before heavy systems move.
- Keep owner-project validation that rejects host-only packages outside `octaryn-client` and `octaryn-server`.
- Expand the new CMake scaffold with native owner targets and targeted platform configure checks before claiming native platform/toolchain parity with the old monolith.
- C/C++ owner code may call managed host exports only through the resolved hostfxr owner bridge. Bridge readiness requires exact managed method-name resolution, ABI size/version validation, owner bundle discovery, failure-path validation, and direct runtime launch evidence.

## Hard Boundaries

- No new top-level `engine/` or `octaryn-engine/` folder.
- No new `Octaryn.Engine.*` namespaces.
- Do not port by copying the monolith shape into a new name.
- Do not rewrite behavior first; preserve behavior by moving it into the right owner.
- Do not make root `cmake/` a dumping ground. Shared build policy, owner targets, dependencies, platform detection, and toolchains must stay in separate named folders.
- Do not mix host platform logic with owner target definitions. Windows/MinGW, Linux distro families, BSD, and macOS platform logic must be isolated behind platform/toolchain modules.
- Do not put networking packages in `octaryn-basegame`.
- Do not put GPU upload, mesh upload, render descriptors, windowing, audio, or UI in server.
- Do not put authoritative world edits, save ownership, or server simulation in client.
- Do not put product-specific game rules in shared or native support libraries.
- Do not put voxel host internals in `octaryn-basegame`: chunks, mesh data, lighting propagation, persistence, replication, transport, storage formats, or low-level world mutation belong to shared/client/server APIs and implementations.
- Do not load a game module by reaching into its internals. Game modules must declare their API version, required capabilities, content registrations, assets, dependencies, and compatibility constraints through shared contracts.
- Do not let client, server, or tools silently accept incompatible game modules. The host must validate manifests, API versions, dependency ranges, required capabilities, content IDs, asset declarations, and multiplayer compatibility before activation.
- Do not expose broad implementation assemblies to game modules. Module code may use only explicitly approved shared APIs, approved host interfaces, and approved .NET packages.
- Do not allow module-owned NuGet dependency drift. Game modules must compile against a deny-by-default package allowlist; unapproved .NET packages, framework namespaces, reflection, native interop, filesystem, process, threading, dynamic loading, and networking access are rejected unless the allowlist says otherwise.
- Do not let gameplay, module, client, server, or tool code create arbitrary computation threads. The host owns threading; systems submit work to the coordinator and worker pool through approved scheduling APIs.

## Destination Roots

```text
octaryn-client/
octaryn-server/
octaryn-basegame/
octaryn-shared/
tools/
cmake/
  Shared/
  Owners/
  Dependencies/
  Platforms/
    Windows/
    Linux/
    BSD/
    MacOS/
  Toolchains/
    Windows/MinGW/
    Linux/
    BSD/
    MacOS/
docs/
refrances/
old-architecture/
build/<owner>/
build/dependencies/
logs/<owner>/
```

## Threading And Work Scheduling

The active architecture must use one explicit host-owned scheduling model. All systems that do meaningful computation or gameplay logic must be written so they can run safely through that model.

Thread roles:

- Main thread: owns process startup/shutdown, platform event pumping, presentation handoff, final frame submission, and the narrow places where a platform or graphics API requires main-thread access. It must not become the place where gameplay, chunk generation, simulation, asset processing, or other bulk computation runs.
- Coordinator thread: owns frame/tick scheduling, dependency graph assembly, work submission, synchronization fences, cancellation, and deterministic handoff between client, server, basegame/module logic, and tools. It schedules work; it does not run bulk work itself except for tiny coordination tasks.
- Worker pool: owns computation. It starts with a minimum of two worker threads and scales up to the available system cores according to host policy. The pool is the execution path for simulation systems, gameplay systems, world generation, mesh/data preparation, asset processing, validation jobs, async save/load preparation, replication preparation, and other CPU-heavy logic.

Scheduling rules:

- All computation systems and gameplay logic must run as jobs in the worker pool or through approved host APIs that schedule onto the pool.
- New code must be thread-safe by default: no hidden global mutable state, no unsynchronized shared containers, no lifetime borrowing across jobs without an explicit owner, and no blocking waits on the main thread.
- Systems must declare their read/write access, ordering dependencies, cancellation behavior, and frame/tick ownership before they enter the scheduler.
- Client presentation work may prepare data on workers, but graphics API calls, window events, and final presentation stay on client-owned main-thread/platform paths.
- Server authority work runs through coordinator-scheduled jobs and commits through deterministic server tick barriers. Save, validation, replication, and world-edit commits must have explicit synchronization points.
- Basegame and external modules do not own threads, tasks, or timers. They receive scheduled system/update entry points and capability handles from the host.
- Tools may use the same scheduler model for offline work, but tool-specific worker use must still be isolated under `tools/` or the owning module tool folder.

API and sandbox impact:

- `octaryn-shared` should expose scheduling contracts only as stable capability-shaped APIs: job scopes, tick phases, read/write declarations, cancellation tokens, and result handles. It must not expose native worker internals or third-party scheduler types.
- Client/server own scheduler implementations and thread creation. Shared/basegame/modules own only contracts and scheduled logic.
- Approved packages such as `Arch.System` may define gameplay systems, but those systems must be driven by host scheduling rather than direct task/thread creation from module code.
- Native C/C++ owner code may drive managed ECS/gameplay or networking through explicit owner bridges. Those bridges are host implementation details, not module APIs.
- Raw `System.Threading`, `Task.Run`, custom timers, unmanaged threads, and ad hoc thread pools remain denied for module code. Any exception requires a documented host API and an allowlist update.
- Native job support should use the approved `octaryn::deps::taskflow` wrapper through focused owner targets such as `octaryn_native_jobs`; do not hand-roll a parallel scheduler in client, server, or basegame.

Validation requirements:

- Source/API validation must reject raw threading and task scheduling from module code.
- Module manifests must declare scheduled systems with phase, owner, resource reads/writes, ordering, flags, and commit barrier before a host can activate scheduled work.
- Scheduler-facing systems must have targeted runtime/profiling validation through direct runs, Tracy captures, focused logs, or benchmarks. Do not use smoke tests or `ctest` as a substitute unless explicitly requested.
- CMake and MSBuild owner targets must keep scheduler support owner-partitioned under `build/<owner>/` and `logs/<owner>/`.

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
    Overlay/
    Player/
    ClientHost/
    FrameLoop/
    Window/
    Rendering/
      Atlas/
      Buffers/
      Pipelines/
      Postprocess/
      Resources/
      Scene/
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
    Libraries/
    Managed/
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
  Shaders/  # stays empty unless a real server-owned compute/offline shader need appears.
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

Basegame is the default bundled game module. It owns high-level game features, game mechanics, content, assets, and default-game behavior. It must interact with the host through shared API contracts only. It should define what the game is, not how the voxel host stores, lights, meshes, saves, streams, replicates, or transports it.

Basegame may define content-facing concepts such as block definitions, item definitions, recipes, tags, loot, features, biome rules, player/game rules, interaction rules, and content data. It must not contain hard-coded core voxel host concepts such as chunk storage, mesh generation, light propagation, persistence implementation, replication internals, transport code, or direct client/server internals.

Basegame must expose a module manifest and registration entry point. The host validates that manifest before activating basegame content, the same way it will validate future game modules or mod-like packages.

```text
octaryn-basegame/
  Octaryn.Basegame.csproj
  Source/
    Native/
    Libraries/
    Managed/
      GameContext.cs
      ManagedGameTag.cs
    Module/
      BasegameModuleRegistration.cs
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
      Actions/
      Interaction/
      Inventory/
      MovementRules/
      Player/
      Time/
      Rules/
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
    Features/
  Shaders/
  Tools/
```

Port source candidates:

- `docs/migration/gameplay-migration-map.md`
- content definitions from `old-architecture/source/world/block/`, after stripping storage, lighting, mesh, and engine-state details.
- high-level game-rule portions of `old-architecture/source/app/player/`.
- high-level interaction-rule portions of `old-architecture/source/world/edit/`; authoritative edit execution stays server-owned.
- biome, feature, and terrain rule data from `old-architecture/source/world/generation/`; generation execution stays server-owned and core noise/chunk internals do not move into basegame.
- unmanaged bridge entry points must stay in host-owned code such as `octaryn-client`; basegame exposes a module registration and manifest only.
- `Octaryn.Basegame.csproj` is the active basegame project name. Do not reintroduce `Octaryn.Game` for the bundled module.
- `Gameplay/Actions/` is for game action declarations and bindings after client input is translated through approved APIs; raw device input remains client-owned.
- `Gameplay/MovementRules/` is for high-level game movement/collision rules; physics execution and authoritative simulation remain server-owned, with client prediction copies owned by client.

## Shared Ownership

Shared owns small contracts and value types used across client, server, basegame, and future game modules. It must not own runtime policy, rendering, persistence implementation, or gameplay behavior. It does own the API shapes that allow the host to load and validate game modules without knowing their internals.

```text
octaryn-shared/
  Octaryn.Shared.csproj
  Source/
    Native/
    Managed/
    Libraries/
    ApiExposure/
    FrameworkAllowlist/
    Time/
    World/
    Networking/
    GameModules/
    ModuleSandbox/
    Compatibility/
    Math/
    Diagnostics/
  Assets/
  Data/
  Shaders/
  Tools/
```

Shared `Assets/`, `Data/`, `Shaders/`, `Tools/`, and `Source/Libraries/` are placeholders only until a pure contract/value need is approved. `Source/Native/HostAbi/` may contain pure ABI layout/version contracts for owner bridges; it must not accumulate runtime implementation, scanners, asset processors, shaders, native support libraries, or gameplay policy.

Port source candidates:

- `old-architecture/source/world/direction.h`
- value-type pieces of `old-architecture/source/world/block/block.h`
- snapshot/command shapes from `old-architecture/source/api/`
- time value types from `old-architecture/source/core/world_time/`

## Game Module Loading And Compatibility

Octaryn should behave like a host for game modules. `octaryn-basegame` is just the bundled default module, not a privileged place for core host logic. Future games, mods, or content modules should follow the same contract shape.

Shared contracts define:

- Module identity: stable module ID, display name, version, API version, and dependency ranges.
- Required capabilities: client presentation features, server authority features, content systems, asset kinds, networking needs, and optional tool requirements.
- API exposure: a manifest-declared list of host API IDs the module requests, with each API granted or rejected by compatibility validation.
- Package exposure: manifest-declared lists of runtime packages, build/analyzer packages, and framework API groups the module requests, checked against allowlists before load.
- Registrations: blocks, items, materials, recipes, tags, loot, features, biomes, interactions, commands, snapshots, and game rules through API-owned registries.
- Compatibility declarations: supported host/API versions, required modules, incompatible modules, multiplayer compatibility, save compatibility, and asset compatibility.
- Validation reports: structured errors and warnings that tools, client, and server can show without loading unsafe gameplay code.

Sandbox and API exposure policy:

- Module APIs are least-privilege and capability-scoped. A module only receives APIs accepted from its manifest.
- In-process modules are an API isolation boundary, not a security boundary for hostile code. Truly untrusted code must run out-of-process under a separate module host or be rejected.
- Modules must not access raw client/server internals, native pointers, persistence backends, transport sockets, GPU resources, process state, threads, filesystem paths, reflection, dynamic assembly loading, unmanaged interop, or network endpoints except through explicit host APIs.
- C# ECS and host networking implementations may cross into C/C++ only through explicit owner host bridges. Modules still see capability-scoped shared APIs, not bridge internals, native pointers, transport packages, or interop surfaces.
- Host APIs expose stable IDs, immutable value types, validated commands, queries, snapshots, registries, events, and bounded resource handles instead of mutable implementation objects.
- Host scheduling APIs expose bounded scheduled work scopes instead of direct threads. Modules request work through the host, and the coordinator places eligible logic on the worker pool.
- Server APIs own authoritative simulation, saves, validation, world edits, and multiplayer state. Client APIs own presentation, input, audio, UI, prediction views, and assets.
- Tools may validate and package module assets offline, but tool access does not imply runtime access.
- Any new host capability required by a module must be added as a small shared contract and implemented by the correct owner before modules can request it.

Enforcement points:

- Shared contracts currently define `GameModuleManifest`, host API ID constants, module capability ID constants, runtime/build package allowlists, framework API group allowlists, denied sandbox group IDs, `ModuleValidationReport`, timing/input-only host frame contracts, and narrow host request command contracts. Typed request records can replace raw manifest string lists once external module packaging needs version ranges and richer diagnostics.
- `octaryn-shared/Source/ApiExposure/` owns API exposure contract shapes and capability names. It does not grant permissions by itself.
- `octaryn-shared/Source/FrameworkAllowlist/` owns allowlist contract shapes for package IDs and framework API groups. It does not scan assemblies by itself.
- `octaryn-shared/Source/ModuleSandbox/` owns sandbox policy contracts and validation result types. It does not contain client/server enforcement logic.
- Client/server/tool hosts currently enforce the contracts with MSBuild/package checks, resolved asset graph checks, source scans, post-build binary metadata checks, manifest validation, and pre-load activation checks. Richer artifact identity binding remains Phase 0 enforcement work before external binary-only modules are trusted.
- Runtime access must go through capability handles supplied by the activating host. Modules must not discover services by scanning assemblies, globals, or implementation namespaces.

.NET package allowlist policy:

`octaryn-shared` must stay package-free or BCL-only unless a contract-only dependency is deliberately approved in this plan. Module-facing contracts must use Octaryn-owned value types and interfaces. Do not expose third-party package types in shared public APIs.

| Package or API group | Allowed owners | Purpose | Version policy | Runtime scope | Validation rule | Enforcement location |
| --- | --- | --- | --- | --- | --- | --- |
| `Arch` | `octaryn-basegame`, approved full games/game modules/mods, `octaryn-client`, `octaryn-server` | Gameplay ECS implementation and host-side ECS integration. | Exact central pin in `Directory.Packages.props`. | Module implementation and client/server host integration only. | Allowed package ID and version must match the pin; no public shared API types. | Module/host package validation and project restore checks. |
| `Arch.LowLevel` | Transitive package for approved Arch runtime packages. | Low-level Arch runtime support. | Exact central pin. | Transitive module runtime only. | May not be referenced directly by module code unless promoted to an explicit approved direct package. | Resolved package validation. |
| `Arch.System` | `octaryn-basegame`, approved full games/game modules/mods, `octaryn-client`, `octaryn-server` | ECS system update framework. | Exact central pin. | Module implementation and client/server host integration only. | Allowed package ID and version must match the pin; no public shared API types. | Module/host package validation and project restore checks. |
| `Arch.System.SourceGenerator` | `octaryn-basegame`, approved full games/game modules/mods, `octaryn-client`, `octaryn-server` | Compile-time ECS system generation. | Exact central pin. | Build/analyzer only. | Must be listed as a requested build package and use `PrivateAssets=\"all\"`, `OutputItemType=\"Analyzer\"`, and `IncludeAssets=\"analyzers;build;buildTransitive\"`. | MSBuild/package validation and resolved asset validation. |
| `Arch.EventBus` | `octaryn-basegame`, approved full games/game modules/mods, `octaryn-client`, `octaryn-server` | Gameplay and host integration events. | Exact central pin. | Module implementation and client/server host integration only. | Allowed package ID and version must match the pin; no public shared API types. | Module/host package validation and project restore checks. |
| `Arch.Relationships` | `octaryn-basegame`, approved full games/game modules/mods, `octaryn-client`, `octaryn-server` | Gameplay and host entity relationships. | Exact central pin. | Module implementation and client/server host integration only. | Allowed package ID and version must match the pin; no public shared API types. | Module/host package validation and project restore checks. |
| `Collections.Pooled` | Transitive package for approved Arch runtime packages. | Pooled collection implementation used by Arch. | Exact central pin. | Transitive module runtime only. | May not be referenced directly by module code unless promoted to an explicit approved direct package. | Resolved package validation. |
| `CommunityToolkit.HighPerformance` | Transitive package for approved Arch runtime packages. | High-performance memory/collection primitives used by Arch. | Exact central pin. | Transitive module runtime only. | May not be referenced directly by module code unless promoted to an explicit approved direct package. | Resolved package validation. |
| `Microsoft.Extensions.ObjectPool` | Transitive package for approved Arch runtime packages. | Object pooling used by Arch package graph. | Exact central pin. | Transitive module runtime only. | May not be referenced directly by module code unless promoted to an explicit approved direct package. | Resolved package validation. |
| `ZeroAllocJobScheduler` | Transitive package for approved Arch runtime packages. | Scheduler support used by Arch.System. | Exact central pin. | Transitive module runtime only. | May not be referenced directly by module code unless promoted to an explicit approved direct package. | Resolved package validation plus source and binary API denial for `Schedulers.*`. |
| `Humanizer.Core` | Transitive build package for approved source generators. | Source-generator support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `Microsoft.Bcl.AsyncInterfaces` | Transitive build package for approved source generators. | Source-generator support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `Microsoft.CodeAnalysis.Analyzers` | Transitive build package for approved source generators. | Analyzer support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `Microsoft.CodeAnalysis.Common` | Transitive build package for approved source generators. | Compiler API support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `Microsoft.CodeAnalysis.CSharp` | Transitive build package for approved source generators. | C# compiler API support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `Microsoft.CodeAnalysis.CSharp.Workspaces` | Transitive build package for approved source generators. | Workspace support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `Microsoft.CodeAnalysis.Workspaces.Common` | Transitive build package for approved source generators. | Workspace support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `Microsoft.NETCore.Platforms` | Transitive build package for approved source generators. | Platform metadata support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `NETStandard.Library` | Transitive build package for approved source generators. | Reference assembly support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `System.Composition` | Transitive build package for approved source generators. | Composition support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `System.Composition.AttributedModel` | Transitive build package for approved source generators. | Composition support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `System.Composition.Convention` | Transitive build package for approved source generators. | Composition support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `System.Composition.Hosting` | Transitive build package for approved source generators. | Composition support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `System.Composition.Runtime` | Transitive build package for approved source generators. | Composition support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| `System.Composition.TypedParts` | Transitive build package for approved source generators. | Composition support. | Exact central pin. | Build graph only. | Must be reachable from approved build packages. | Resolved package validation. |
| Safe BCL value APIs | `octaryn-shared`, `octaryn-basegame`, approved full games/game modules/mods | Primitives, collections, spans/memory, math/numerics, dates/times, text, and diagnostics abstractions. | Runtime-provided .NET 10 APIs only. | Shared contracts and module implementation. | Allowed namespace/API group only; no filesystem, network, reflection, process, environment, or threading APIs. | Analyzer/source scan and pre-load validation. |
| Host-routed JSON/data parsing | `octaryn-basegame`, approved full games/game modules/mods, tools | Content data parsing through approved host APIs. | Runtime-provided .NET 10 APIs unless a package is explicitly approved. | Offline tools or bounded host API runtime path. | Module cannot open arbitrary paths; host supplies bounded data streams/handles. | Tool validation and host capability checks. |
| `LiteNetLib` | `octaryn-client`, `octaryn-server` | Reliable UDP transport. | Exact central pin. | Host transport only. | Rejected in `octaryn-shared`, `octaryn-basegame`, game modules, and mods. | Package validation and owner project checks. |
| `LiteEntitySystem` | `octaryn-client`, `octaryn-server` | Network entity replication/synchronization. | Exact central pin. | Host replication implementation only. | Rejected in `octaryn-shared`, `octaryn-basegame`, game modules, and mods. | Package validation and owner project checks. |

Denied to modules by default: `System.IO`, raw filesystem paths, `System.Net`, sockets, HTTP clients, `System.Diagnostics.Process`, unmanaged interop, unsafe native bridges, reflection/dynamic loading, runtime code generation, arbitrary threading/task scheduling, timers, custom worker pools, environment variables, direct host service discovery, direct console/stdout/stderr writes, and unlisted NuGet packages. If a game or mod needs a new package or framework API group, add it here with owner, purpose, version policy, allowed runtime scope, validation rule, and enforcement location before using it.

Package validation currently has two layers:

- MSBuild item validation catches direct package misuse, host-only package misuse, and analyzer metadata mistakes.
- `tools/validation/validate_module_manifest_packages.py` checks that module manifest requested package lists match the module project’s direct runtime/build `PackageReference`s.
- `tools/validation/validate_module_manifest_files.py` checks that module manifest content/assets point at real module files and that non-placeholder `Data/`, `Assets/`, and `Shaders/` files are declared.
- `octaryn-basegame/Data/Module/octaryn.basegame.module.json` is the checked-in bundled module package descriptor. It mirrors `BasegameModuleRegistration.Manifest`, is copied to client/server bundles, and is compared by `tools/validation/Octaryn.ModuleManifestProbe/` so package metadata can become the future discovery source without replacing the current in-process registration path yet.
- `tools/validation/Octaryn.ModuleManifestProbe/` writes the generated validation manifest under `build/basegame/<preset>/generated/octaryn.basegame.manifest.json` for CMake/manual validation and compares the checked-in package descriptor with the code manifest.
- `tools/validation/validate_all_project_reference_boundaries.py` discovers every active `.csproj` under shared, client, server, basegame, tools, games, modules, and mods, then applies `validate_project_reference_boundaries.py` so new projects cannot bypass owner-reference rules. Modules may reference shared contracts only.
- `tools/validation/validate_dotnet_package_assets.py` is owner-aware. It parses module and host `project.assets.json` target graphs, rejects unclassified direct, runtime-transitive, build-direct, and build-transitive packages for modules, and rejects unclassified or unapproved direct packages in client/server host graphs. Shared and old-architecture projects stay outside resolved-package validation unless a future policy explicitly opts them in.
- `tools/validation/Octaryn.ModuleApiProbe/` scans module C# source with Roslyn before compile and rejects denied filesystem, networking, process, reflection, native interop, console, environment, dynamic-loading, raw-threading APIs, transitive scheduler APIs, and unapproved shared networking contracts. Its denied groups must stay aligned with `DeniedFrameworkApiGroups`.
- `tools/validation/validate_module_layout.py` checks module `Data/`, `Assets/`, and `Shaders/` layout, rejects unsupported file suffixes, duplicate normalized file IDs, and empty content/asset/shader files.
- `tools/validation/validate_package_policy_sync.py` keeps `tools/package-policy/module-packages.json`, `ModulePackagePolicy.props`, `Directory.Packages.props`, and shared package allowlist constants synchronized.
- `tools/package-policy/module-packages.json` is the machine-readable package policy source used by the asset graph validator. MSBuild and shared constants must stay aligned with it until those surfaces are generated from the policy file.

External full game projects should live under `octaryn-games/`, external module projects under `octaryn-modules/`, and external mod projects under `octaryn-mods/`, or set an owner through an early-imported props file before the root `Directory.Build.props` is evaluated. Setting `OctarynModuleOwner` only in the `.csproj` body is too late for output routing.

Client/server hosts own activation:

- The server validates authoritative content, gameplay rules, save compatibility, command shapes, snapshot shapes, and multiplayer compatibility before simulation starts.
- The current server project validates the bundled basegame manifest and server compatibility before returning from `ServerHost.Run`; deeper authoritative content/save/multiplayer validation is still a porting task under `octaryn-server/Source/Validation/`.
- The client validates assets, presentation capabilities, UI/overlay declarations, local prediction hooks, and client-compatible snapshot views before presenting a module.
- Tools validate content and assets offline before packaging a game module.
- Basegame and future modules register high-level content and mechanics only after the host accepts their manifest.

No module may bypass shared registries or write directly into client/server/core voxel internals. If a gameplay feature needs a new host capability, add a small shared API contract and implement it in the correct client or server owner.

## Support Libraries

Do not create a generic runtime root. The old native tree already has focused support libs for logging, diagnostics, memory, jobs, dependency wrappers, profiling, and shader tooling. During the port, keep those as small build targets owned by the layer that needs them.

Agent workers must check this library catalog before implementing any feature. If a listed library matches the job, use it through the owner that is allowed to depend on it. Do not hand-roll replacement systems, add parallel dependencies, or move a library across ownership boundaries without updating this plan and the migration map.

Native support landing zones:

- `octaryn-client/Source/ClientHost/`
- `octaryn-client/Source/FrameLoop/`
- `octaryn-client/Source/Managed/`
- `octaryn-client/Source/Libraries/<ExactLibraryName>/` for client-local native support.
- `octaryn-server/Source/Tick/`
- `octaryn-server/Source/Simulation/`
- `octaryn-server/Source/Libraries/<ExactLibraryName>/` for server-local native support.
- `octaryn-shared/Source/Diagnostics/`
- `octaryn-basegame/Source/Libraries/<ExactLibraryName>/` only for basegame-local native support that does not expose host internals.
- `tools/<ExactToolName>/` for repo-wide developer tools that are not owned by a game/content package.
- focused native support targets such as `octaryn_native_logging`, `octaryn_native_diagnostics`, `octaryn_native_memory`, and `octaryn_native_jobs`.

Port source candidates:

- `old-architecture/source/core/log.*` -> focused diagnostics/logging lib used by client/server.
- `old-architecture/source/core/memory_mimalloc.*` -> native build support linked where needed.
- `old-architecture/source/core/crash_diagnostics.*` -> diagnostics support, not a product root.
- `old-architecture/source/runtime/jobs/` -> client/server job support or server simulation scheduler depending usage.
- native managed-host bridge pieces -> client host or shared contracts after renaming away from engine API names.

## CMake And Platform Build Architecture

Root `cmake/` owns only new-architecture build policy. Old CMake files stay under `old-architecture/cmake/` until intentionally ported into the structure below. Do not move old CMake modules wholesale; split them by responsibility first.

The tree below is the required target structure. In the current workspace these paths are placeholders unless the named `.cmake` file exists. Do not describe Windows, Linux, BSD, macOS, owner target, dependency, or root preset support as implemented until the concrete module and a targeted configure check exist.

```text
cmake/
  Shared/
    ProjectDefaults.cmake
    CompilerWarnings.cmake
    BuildOutputs.cmake
    OwnerBuildLayout.cmake
  Owners/
    DotNetOwner.cmake
    NativeOwner.cmake
    ClientTargets.cmake
    ServerTargets.cmake
    SharedTargets.cmake
    BasegameTargets.cmake
    ToolTargets.cmake
  Dependencies/
    BasegameDependencies.cmake
    DependencyPolicy.cmake
    DotNetHosting.cmake
    NativeDependencyAliases.cmake
    ClientDependencies.cmake
    ServerDependencies.cmake
    ToolDependencies.cmake
  Platforms/
    PlatformDispatch.cmake
    Windows/
      WindowsPlatform.cmake
      MinGWPlatform.cmake
    Linux/
      LinuxPlatform.cmake
      ArchFamily.cmake
      DebianFamily.cmake
      FedoraFamily.cmake
      SuseFamily.cmake
    BSD/
      BSDPlatform.cmake
      FreeBSDPlatform.cmake
    MacOS/
      MacOSPlatform.cmake
  Toolchains/
    Windows/
      MinGW/
        x86_64-w64-mingw32.cmake
    Linux/
      gcc.cmake
      clang.cmake
    BSD/
      clang.cmake
    MacOS/
      apple-clang.cmake
```

Layer responsibilities:

- `cmake/Shared/` owns repo-wide CMake defaults: C/C++ standards, warning policy, output layout, build/log owner paths, shared helper functions, and naming rules.
- `cmake/Owners/` owns target construction for client, server, shared contracts, basegame assets/modules, and tools. Owner modules may call shared helpers and dependency aliases, but must not contain platform detection.
- `cmake/Dependencies/` owns dependency wrappers and allowed dependency aliases. Dependencies must be grouped by real owner need; do not recreate one old global dependency bag.
- `cmake/Platforms/` owns host platform facts and distro-family policy: Windows, MinGW specifics, Linux family package hints, BSD differences, macOS SDK/signing/framework details, and platform capability checks.
- `cmake/Toolchains/` owns cross/native compiler toolchain files only. Toolchain files set compilers, sysroots, target triples, find-root behavior, and platform knobs; they must not create Octaryn targets or fetch dependencies.
- `tools/build/` owns new developer-facing build commands that select presets/toolchains and write to `build/<owner>/` and `logs/<owner>/`.

Platform rules:

- Windows cross-builds from Linux use explicit MinGW toolchain files under `cmake/Toolchains/Windows/MinGW/`; do not hide MinGW behavior inside generic Windows logic.
- Native Windows policy belongs under `cmake/Platforms/Windows/`, but native MSVC Windows support is unimplemented until a native Windows preset/toolchain path exists. The old public Windows presets are MinGW cross-builds from Linux, not native Windows coverage.
- Linux policy is split by distro family only when real package/tool behavior differs. Start with Arch, Debian, Fedora, and Suse/openSUSE because the old dependency installer already has distinct package-manager logic for those families.
- BSD policy belongs under `cmake/Platforms/BSD/`, with FreeBSD-specific logic isolated from generic BSD checks. BSD is target coverage, not currently implemented old-CMake behavior.
- macOS policy belongs under `cmake/Platforms/MacOS/`, including SDK, deployment target, frameworks, signing/notarization hooks, and AppleClang behavior. macOS is target coverage, not currently implemented old-CMake behavior.
- Platform modules report capabilities; owner targets decide whether to use those capabilities. Platform modules must not own gameplay, rendering, server, basegame, or module-sandbox behavior.

Port map for old CMake:

- `old-architecture/cmake/BuildLayout.cmake` -> `cmake/Shared/OwnerBuildLayout.cmake`, after renaming away from engine product names and enforcing `build/<owner>/` and `logs/<owner>/`.
- Old dependency cache paths such as `build/shared/deps/<bucket>` and `logs/deps/<bucket>` -> `build/dependencies/<bucket>`, with dependency logs kept under the same dependency build bucket unless a dependency is truly owner-local.
- `old-architecture/cmake/ProjectOptions.cmake` -> `cmake/Shared/ProjectDefaults.cmake` plus owner-specific options in `cmake/Owners/`.
- `old-architecture/cmake/Dependencies.cmake` -> `cmake/Dependencies/`, split by dependency policy, alias creation, and owner-specific dependency groups.
- `old-architecture/cmake/CPM.cmake` -> `cmake/Dependencies/` only if CPM remains the selected dependency mechanism.
- `old-architecture/cmake/toolchains/windows-x64.cmake` -> `cmake/Toolchains/Windows/MinGW/x86_64-w64-mingw32.cmake` if it is actually a MinGW cross toolchain; otherwise create a correctly named native Windows toolchain.
- `old-architecture/CMakePresets.json` -> new root presets only after the owner/platform/toolchain split exists.
- `old-architecture/tools/build/configure.sh`, `cmake_build.sh`, `build_all.sh`, and repair/install helpers -> `tools/build/` only after they select new owner presets, use the new platform/toolchain modules, and write outputs to `build/<owner>/`, `build/dependencies/`, `logs/<owner>/`, or `logs/build/`.
- `old-architecture/tools/build/renderdoc.sh` and `tracy_capture.sh` -> focused profiling wrappers under root `tools/` only after logs are moved away from old `logs/engine_control`, `logs/tracy`, and `logs/octaryn-engine` paths.
- Old architecture scripts that remain under `old-architecture/` must be corrected to write under `build/old-architecture/` and `logs/old-architecture/` before they are used as accepted validation paths.

Validation for CMake changes:

- For structure-only CMake work, run `validate_cmake_target_inventory.py`; it verifies active target names, required owner/platform/dependency files, and absence of old generic product CMake paths.
- For build policy changes, configure the smallest owner target that uses the changed policy.
- For platform/toolchain changes, run targeted configure checks for the affected platform or toolchain when the host has that compiler/sysroot installed.
- Active configure presets cover default debug/release, Linux GCC, Linux Clang, and Windows MinGW. MinGW configure may disable hostfxr bridge/probe targets when target-compatible .NET native hosting assets are unavailable, but Linux host validation must still build and run those bridge/probe targets.
- Do not validate CMake work with smoke tests or `ctest` unless explicitly requested.

## Library Catalog

Use these libraries for the cases they are intended for. Keep wrappers focused and owner-scoped; the old `octaryn_engine_runtime` target is the monolith to split, not a library to preserve.

### Native Support Targets

| Old target | Purpose | Destination |
| --- | --- | --- |
| `octaryn_engine_log` | Native logging through `spdlog`. | `octaryn_native_logging`, used by client, server, tools, and support libraries that need logs. |
| `octaryn_engine_diagnostics` | Crash diagnostics and stack traces. | `octaryn_native_diagnostics`, used by executables and tools that need crash reports. |
| `octaryn_engine_memory` | Process allocator setup through `mimalloc`. | `octaryn_native_memory`; remove or isolate its current SDL coupling while porting. |
| `octaryn_engine_imgui_backend` | Dear ImGui backend glue for SDL3 and SDL GPU. | Client UI/debug UI only. |
| `octaryn_engine_shader_tool` | GLSL compilation, reflection, validation, and SPIR-V/MSL asset generation. | Root `tools/` shader compiler; generated shaders are client-owned assets. |
| `octaryn_engine_texture_atlas` | Builds block/material texture atlases from basegame content and packs. | `octaryn-basegame/Tools/`; generated atlas assets are consumed by the client. |
| `octaryn_managed_game` | Publishes the C# basegame assembly. | `octaryn-basegame`. |
| `octaryn_engine_shader_assets` | Generated shader asset target. | `octaryn-client` asset build. |
| `octaryn_engine_runtime_assets` | Copies generated assets into a runnable client bundle. | `octaryn-client` packaging. |
| `octaryn_engine_runtime_bundle` | Bundles current monolithic runtime and generated assets. | Replace with `octaryn_client_bundle` during the port. |
| `octaryn_engine_runtime` | Current monolithic native executable. | Split across client, server, shared, high-level basegame mechanics/content, tools, and support targets. Do not recreate it under a new name. |

### Native Dependency Wrappers

| Wrapper | Purpose | Allowed owners |
| --- | --- | --- |
| `octaryn::deps::spdlog` | Fast structured/native logging. | Native logging support, client, server, tools. |
| `octaryn::deps::cpptrace` | Stack traces for crash diagnostics. | Native diagnostics support and executable crash paths. |
| `octaryn::deps::tracy` | Profiling instrumentation. | Client, server, tools, and native support when profiling is enabled. |
| `octaryn::deps::mimalloc` | Allocator backend. | Native memory support only; consumers use the support target. |
| `octaryn::deps::taskflow` | Job scheduling graph and worker execution. | Native jobs support, coordinator-thread scheduling, scalable worker pool execution, client presentation work queues, server simulation jobs, and tool jobs. |
| `octaryn::deps::unordered_dense` | High-performance hash maps and sets. | Owner-local implementation code that needs dense hash containers. |
| `octaryn::deps::eigen` | Math and linear algebra. | Shared pure math/value code or owner-local math; rendering-only math stays client-owned. |
| `octaryn::deps::glaze` | JSON and metadata serialization. | Shared contracts when pure, server persistence, basegame content tools, root tools. |
| `octaryn::deps::sdl3` | Windowing, input, SDL GPU, platform services, timers. | Client only, except isolated tool use when a tool truly needs SDL. |
| `octaryn::deps::sdl3_image` | Image loading. | Client UI/assets and asset import tools. |
| `octaryn::deps::sdl3_ttf` | Text rendering. | Client UI and overlays. |
| `octaryn::deps::imgui` | Immediate-mode runtime/debug UI. | Client UI/debug UI and tools. |
| `octaryn::deps::implot` | 2D plots and telemetry panels. | Client debug UI and tools. |
| `octaryn::deps::implot3d` | 3D plots and debug visualization. | Client debug UI and tools. |
| `octaryn::deps::imgui_node_editor` | Node graph editor UI. | Tools first; client only for explicit debug/editor UI. |
| `octaryn::deps::imguizmo` | Transform gizmos. | Tools first; client only for explicit debug/editor UI. |
| `octaryn::deps::imanim` | ImGui animation/editor widgets. | Tools first; do not ship in core client unless a real client feature uses it. |
| `octaryn::deps::imfiledialog` | ImGui file dialogs. | Tools or explicit client debug/editor UI. |
| `octaryn::deps::shaderc` | GLSL to SPIR-V compilation. | Shader tooling only. |
| `octaryn::deps::shadercross` | SDL shader cross-compilation. | Shader tooling only. |
| `octaryn::deps::spirv_tools` | SPIR-V validation and optimization. | Shader tooling only. |
| `octaryn::deps::spirv_cross` | Shader reflection and cross-compilation. | Shader tooling only. |
| `octaryn::deps::fastgltf` | glTF import/loading. | Asset import tools; client only if runtime glTF loading is intentionally added. |
| `octaryn::deps::ktx` | KTX texture containers and GPU texture pipeline. | Asset tools and intentional client texture loading. |
| `octaryn::deps::meshoptimizer` | Mesh optimization and import processing. | Asset tools; client only for intentional runtime optimization. |
| `octaryn::deps::ozz_animation` | Skeletal animation runtime. | Client animation runtime; basegame may own animation content data. |
| `octaryn::deps::fastnoise2` | Fast procedural noise. | Server-side authoritative generation execution and tools; basegame may expose high-level generation parameters through API-owned data only. |
| `octaryn::deps::jolt` | Physics simulation and collision. | Server authority first; client only for prediction/presentation copies. |
| `octaryn::deps::openal` | Audio playback backend. | Client audio. |
| `octaryn::deps::miniaudio` | Audio decode/playback helpers. | Client audio. |
| `octaryn::deps::zlib` | Compression. | Server persistence, asset tools, and support wrappers. |
| `octaryn::deps::lz4` | Fast save/cache compression. | Server persistence and tooling caches. |
| `octaryn::deps::zstd` | Strong save/cache compression. | Server persistence and tooling caches. |

### Managed Packages

| Package | Purpose | Allowed owners |
| --- | --- | --- |
| `Arch` | C# ECS core for gameplay entities and systems. | `octaryn-basegame`; client/server only for private owner-local state, never `octaryn-shared`. |
| `Arch.System` | ECS system update framework. | Same as `Arch`; use for real system code instead of custom ECS loops. |
| `Arch.System.SourceGenerator` | Compile-time support for `Arch.System`. | Only projects that directly define Arch systems; keep analyzer/private. |
| `Arch.EventBus` | Gameplay event bus for Arch-style systems. | `octaryn-basegame` or private owner-local systems, never shared contracts. |
| `Arch.Relationships` | ECS entity relationship helpers. | `octaryn-basegame` or private owner-local systems, never shared contracts. |
| `LiteNetLib` | Reliable UDP transport. | `octaryn-client` and `octaryn-server` transport implementation only. |
| `LiteEntitySystem` | Network entity replication/synchronization. | `octaryn-client` and `octaryn-server` replication implementation only. |

`LiteNetLib` and `LiteEntitySystem` stay centrally pinned for future host transport work, but they are not module permissions. Basegame must talk through shared command, snapshot, registry, interaction, feature, query, and scheduling contracts rather than transport packages, raw threads, or client/server internals.

Host-owned C# ECS or networking packages may be driven from C/C++ through narrow client/server bridge contracts when that is the best owner route. That does not make those packages module-facing or weaken the module sandbox.

Sandboxed game modules and mods must not reference host-only packages or unlisted NuGet packages. `Directory.Packages.props` is only the central version pin file; it is not permission to use a package from module code.

## Old Architecture Tooling

Old build helpers, old CMake modules, old desktop helper tools, and old profiling wrappers stay under `old-architecture/` until they are intentionally ported.

The old atlas builder is basegame-specific content tooling and belongs under `octaryn-basegame/Tools/AtlasBuilder/` or another focused basegame tool folder. Keep a one-file script only if it stays small and deliberately scoped.

Active root `cmake/` and `tools/` are reserved for new architecture support only. Generated outputs should be owner-partitioned under `build/<owner>/` and `logs/<owner>/`.

## Build Target Names

Active root targets:

```text
octaryn_shared
octaryn_shared_native
octaryn_shared_host_abi
octaryn_basegame
octaryn_basegame_native
octaryn_server
octaryn_server_bundle
octaryn_server_native
octaryn_server_managed_bridge
octaryn_server_launch_probe
octaryn_client_managed
octaryn_client_native
octaryn_client_managed_bridge
octaryn_client_launch_probe
octaryn_client_bundle
octaryn_tools
octaryn_validate_all
octaryn_validate_cmake_targets
octaryn_validate_cmake_policy_separation
octaryn_validate_package_policy_sync
octaryn_validate_project_references
octaryn_validate_module_manifest_packages
octaryn_validate_module_manifest_files
octaryn_validate_module_manifest_probe
octaryn_validate_module_source_api
octaryn_validate_module_binary_sandbox
octaryn_validate_module_layout
octaryn_validate_dotnet_package_assets
octaryn_validate_native_abi_contracts
octaryn_validate_dotnet_owners
octaryn_validate_scheduler_contract
octaryn_validate_scheduler_probe
octaryn_validate_owner_module_validation_probe
octaryn_validate_hostfxr_bridge_exports
octaryn_validate_owner_launch_probes
octaryn_run_client_launch_probe
octaryn_run_server_launch_probe
octaryn_all
```

Internal dependency targets such as `octaryn_dotnet_hosting` and `octaryn_native_threads` are implementation details for owner CMake modules. They are not public build targets, but they must stay under `cmake/Dependencies/` and remain out of old-architecture target wiring.

Planned focused targets, added only when their implementation exists:

```text
octaryn_client_assets
octaryn_client_shaders
octaryn_basegame_assets
octaryn_module_validator
octaryn_module_sandbox_contracts
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
- For architecture-only structure work, verify file tree shape, empty-file/placeholders, owner partitioning, and absence of stale old product names in active roots.
- For every touched old-architecture file, verify the source-to-destination map or a removal reason is recorded.
- For module/API/package work, verify no unapproved package, transitive package, framework API group, unsafe/native bridge, direct console write, reflection, filesystem, network, process, threading, or dynamic loading access was introduced.
- For CMake work, verify old dependency/log paths are not recreated in active roots and targeted configure checks cover the changed owner/platform/toolchain when practical.

## Phase Order

1. Create blank owner structure and migration maps.
2. Rename managed API away from `Octaryn.Engine.Api` into shared contracts and client/server host contracts.
3. Define shared game-module manifest, API exposure, package allowlist, compatibility, validation, registry, command, snapshot, and query contracts.
4. Define the host scheduling contract: main thread, coordinator thread, scalable worker pool with at least two workers, scheduled system declarations, and thread-safety rules for all computation and gameplay logic.
5. Create the new CMake split: shared build policy, owner targets, dependency aliases, platform modules, and toolchains.
6. Split build target names away from `octaryn_engine_*`.
7. Port shared value contracts.
8. Port server-authoritative world and persistence behind shared APIs and coordinator-scheduled worker jobs.
9. Port client presentation, windowing, rendering, shaders, and uploads behind shared APIs, keeping computation on worker jobs and presentation handoff on client-owned main-thread paths.
10. Port basegame as the first validated game module with high-level content and gameplay rules only, scheduled through host APIs.
11. Wire client-server transport through shared message contracts.
12. Validate module loading, package allowlist checks, API exposure checks, scheduler/thread-safety checks, CMake platform/toolchain checks, compatibility checks, and runtime/profiling paths.
