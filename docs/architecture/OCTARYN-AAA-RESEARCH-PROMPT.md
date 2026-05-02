# Octaryn AAA Research Prompt

Use this document as the prompt for a deep research and architecture-planning pass. The goal is not to implement code. The goal is to catch every major AAA engine, game-module, modding, ECS, networking, physics, UI, world, tooling, validation, and dependency decision that Octaryn must plan before the modular port goes too far.

## Prompt To Paste Into ChatGPT

You are researching and planning the Octaryn AAA modular engine/API architecture. Treat the facts in this prompt as current project constraints. Produce a complete architecture research report and gap list. Do not invent a monolithic engine target, generic runtime bucket, or implementation shortcut that violates the owner split.

Your output must be practical for a C/C++ first voxel game platform with a managed C# gameplay API, basegame module, future games, and mods. Research the right patterns, libraries, APIs, system boundaries, data models, validation strategies, and milestones. When recommending a library, explain why it fits, where it belongs, what alternatives exist, what risks it introduces, and whether it should be exposed to modules or hidden behind an Octaryn API.

The most important outcome is a full list of systems and decisions that must be planned so nothing important is missed.

## Current Project Base

Repository root:

```text
/home/zacharyr/octaryn-workspace
```

Active owner roots:

```text
octaryn-client/
octaryn-server/
octaryn-shared/
octaryn-basegame/
tools/
cmake/
docs/
refrances/
old-architecture/
```

Generated output roots:

```text
build/<preset>/<owner>/
build/<preset>/deps/
build/dependencies/
logs/<owner>/
```

Primary language and build base:

- C++23 and C17 for native owner code.
- CMake 3.28+ for native/build orchestration.
- .NET 10 and latest C# for managed shared/basegame/client/server code.
- Linux/Arch first with Linux-hosted Windows cross builds.
- Public presets are expected to be `debug-linux`, `release-linux`, `debug-windows`, and `release-windows`.
- Validation should use targeted builds, direct runtime probes, owner launch probes, structure validators, profiling logs, or focused benchmarks.
- Do not use smoke tests or `ctest` as validation unless explicitly requested.

## Hard Architecture Rules

- Keep strict separation between `octaryn-client`, `octaryn-server`, `octaryn-shared`, `octaryn-basegame`, `tools`, and `cmake`.
- Do not create new top-level `engine/`, `octaryn-engine/`, generic `runtime/`, `common`, `helpers`, `misc`, or catch-all buckets.
- Treat `old-architecture/` as source material only.
- Port behavior into the correct owner with the smallest practical changes.
- Preserve behavior unless a boundary/API change is required.
- Keep client presentation/rendering out of server.
- Keep server authority/persistence/simulation out of client.
- Keep gameplay/content in basegame or another game module.
- Keep shared implementation-free and contract/API focused.
- Keep module/game/mod APIs explicit, capability-scoped, and deny-by-default.
- Keep build outputs under `build/<preset>/<owner>/`.
- Keep logs under `logs/<owner>/`.
- Do not expose raw backend internals to basegame, games, or mods.
- Do not expose broad service locators, native pointers, renderer handles, raw physics worlds, raw ECS storage, raw sockets, transport sessions, filesystem access, arbitrary threading, process control, or scheduler internals to modules.

## Owner Responsibilities

### `octaryn-client`

Owns presentation:

- windowing
- input collection and mapping
- rendering
- shaders
- GPU upload
- audio
- UI rendering
- overlays
- local prediction
- interpolation
- client host code
- screen-space UI execution
- world-space UI execution
- render-to-texture
- textured 3D UI quads/panels
- focus handling
- raycast input routing for world-space UI

Client must not own authoritative world edits, persistence authority, server simulation, or product game rules.

### `octaryn-server`

Owns authority:

- simulation
- validation
- persistence
- world saves
- server ticks
- replication
- transport hosting
- authoritative physics
- authoritative world edits
- server-side fluid/gas/block/entity simulation
- server-side module activation and validation

Server must not own GPU upload, render descriptors, shaders for presentation, product UI, audio, or client-only rendering.

### `octaryn-shared`

Owns clean API contracts and implementation-free value types:

- host interfaces
- tick contracts
- commands
- snapshots
- registries
- queries
- IDs
- positions
- world bounds contracts
- replication contracts
- component declarations
- system declarations
- module manifests
- dependency declarations
- content declarations
- asset declarations
- capability IDs
- API allowlists
- framework/package allowlists
- validation-facing shapes

Shared must not contain product gameplay policy, rendering implementation, persistence implementation, networking transport, physics backend implementation, or third-party backend types in public APIs.

### `octaryn-basegame`

Owns the default bundled game module:

- blocks
- items
- materials
- recipes
- tags
- loot
- feature and biome rules
- player rules
- interactions
- base content data
- basegame main menu
- pause menu
- inventory UI
- HUD
- world-space panels
- block/entity panels
- product-specific options
- high-level gameplay systems

Basegame is not privileged engine internals. It must use the same public API model as future games and mods.

### `tools`

Owns repo-wide developer operations:

- build orchestration
- profiling capture wrappers
- validation tools
- module inspection
- package policy validators
- shader compiler tools
- workspace UI/dev tools
- ABI checks
- packaging checks

Basegame-specific content tools belong under `octaryn-basegame/Tools/`, not root `tools/`.

### `cmake`

Owns build/dependency/toolchain policy:

- `cmake/Shared/`
- `cmake/Owners/`
- `cmake/Dependencies/`
- `cmake/Platforms/`
- `cmake/Toolchains/`

Do not mix platform detection into owner target definitions. Do not turn root `cmake/` into a dump for old monolithic modules.

## Current Core Host Baseline

The core host should boot into a minimal inspectable world:

- flying camera only
- no default player physics
- no default collision controller
- no default survival/avatar rules
- no product main menu or product UI
- flat blank terrain
- target world height is 512 blocks
- vertical world span should be centered around origin
- deterministic owner-routed build/log output

Basegame, game modules, or mods add:

- player movement rules
- physics bodies
- entity controllers
- world generation
- interactions
- inventory/items
- UI overlays
- main menu/front-end flow
- progression/game state

Existing 256-height constants or chunk-edge-derived height constants are migration debt. Research should plan a clean world constants model where chunk width/depth and world height are separate concepts.

## Current ECS And API Direction

Octaryn should be ECS-based for:

- entities
- blocks
- items
- UI state
- global game state
- input actions
- world interactions
- fluids
- gases
- machines
- projectiles
- abilities
- rules
- content systems

The target author experience:

- Define the thing.
- Attach components.
- Write allowed logic through explicit APIs.
- Declare replication and persistence intent.
- Let the host-owned backend ECS handle storage, scheduling, networking, persistence, validation, and presentation handoff.

Native C++ should own:

- fast ECS storage and query execution
- scheduling
- worker execution
- native simulation kernels
- networking packers
- persistence packers
- world interaction pipelines
- physics backend integration
- high-throughput validation and packaging paths

C# should own:

- ergonomic public API declarations
- basegame gameplay logic where appropriate
- game module and mod authoring
- managed system definitions
- content registration
- component declarations
- module manifests
- capability declarations

The shared API should expose contracts, not storage. Modules should never access raw ECS storage or backend implementation types.

## UI Direction

Octaryn needs first-class UI API support for:

- screen-space UI
- world-space UI
- render-to-texture UI
- textured 3D quads and panels
- HUD
- menus
- inventory
- block/entity panels
- nameplates
- diegetic controls
- tool/editor/debug UI where allowed
- pointer, keyboard, controller, and gamepad routing
- focus and capture
- raycast-to-UI mapping
- layout, styling, fonts, text shaping, localization, accessibility, animation, transitions, and input actions

Client renderer owns execution and presentation. Basegame/modules declare UI models, surfaces, anchors, bindings, actions, and styling through approved APIs.

Core/client-owned UI is limited to debug, diagnostics, profiler, validation, editor/developer, and emergency host surfaces. Product UI belongs in basegame or another game module.

## DDGI And Lighting Hold

DDGI, skylight propagation, lighting architecture, and old CPU skylight behavior are on hold until a dedicated user-approved lighting plan exists.

Research may include a future placeholder for DDGI/lighting planning, but do not treat lighting as an active implementation slice. Do not recommend immediate DDGI/skylight implementation work in this research output.

## Active Project Files And Structure To Consider

Managed project files:

```text
octaryn-shared/Octaryn.Shared.csproj
octaryn-basegame/Octaryn.Basegame.csproj
octaryn-client/Octaryn.Client.csproj
octaryn-server/Octaryn.Server.csproj
```

Native/build project files:

```text
CMakeLists.txt
octaryn-client/CMakeLists.txt
cmake/Shared/*.cmake
cmake/Owners/*.cmake
cmake/Dependencies/*.cmake
cmake/Platforms/Linux/*.cmake
cmake/Platforms/Windows/*.cmake
cmake/Toolchains/Linux/clang.cmake
cmake/Toolchains/Windows/clang.cmake
```

Current source roots include:

```text
octaryn-shared/Source/ApiExposure/
octaryn-shared/Source/FrameworkAllowlist/
octaryn-shared/Source/GameModules/
octaryn-shared/Source/Host/
octaryn-shared/Source/ModuleSandbox/
octaryn-shared/Source/Networking/
octaryn-shared/Source/Time/
octaryn-shared/Source/World/

octaryn-basegame/Source/Managed/
octaryn-basegame/Source/Module/
octaryn-basegame/Data/
octaryn-basegame/Assets/
octaryn-basegame/Tools/

octaryn-client/Source/ClientHost/
octaryn-client/Source/Managed/
octaryn-client/Source/WorldPresentation/
octaryn-client/Shaders/

octaryn-server/Source/Managed/
octaryn-server/Source/Tick/
octaryn-server/Source/Validation/
octaryn-server/Source/Networking/
octaryn-server/Source/Persistence/
octaryn-server/Source/Physics/
octaryn-server/Source/Simulation/

tools/validation/
tools/package-policy/
tools/build/
tools/profiling/
tools/ui/
tools/Source/ShaderCompiler/
```

Current docs to align with:

```text
AGENTS.md
docs/architecture/octaryn-appendix.md
docs/architecture/octaryn-master-plan.md
```

## Current Build And Validation Targets

Root aggregate target:

```text
octaryn_all
```

Current owner targets include:

```text
octaryn_shared
octaryn_basegame
octaryn_server
octaryn_server_native
octaryn_server_bundle
octaryn_client_native
octaryn_client_bundle
octaryn_tools
```

Current validation aggregate:

```text
octaryn_validate_all
```

Known validation targets include:

```text
octaryn_validate_cmake_targets
octaryn_validate_cmake_policy_separation
octaryn_validate_cmake_dependency_aliases
octaryn_validate_package_policy_sync
octaryn_validate_project_references
octaryn_validate_module_manifest_packages
octaryn_validate_module_manifest_files
octaryn_validate_module_manifest_probe
octaryn_validate_bundle_module_payload
octaryn_validate_module_source_api
octaryn_validate_module_binary_sandbox
octaryn_validate_module_layout
octaryn_validate_basegame_block_catalog
octaryn_validate_dotnet_package_assets
octaryn_validate_native_abi_contracts
octaryn_validate_native_owner_boundaries
octaryn_validate_native_archive_format
octaryn_validate_dotnet_owners
octaryn_validate_scheduler_contract
octaryn_validate_scheduler_probe
octaryn_validate_world_time_probe
octaryn_validate_server_world_blocks_probe
octaryn_validate_basegame_player_probe
octaryn_validate_basegame_interaction_probe
octaryn_validate_client_world_presentation_probe
octaryn_validate_owner_module_validation_probe
octaryn_validate_hostfxr_bridge_exports
octaryn_validate_owner_launch_probes
```

Research should propose additional validators only when they enforce a real boundary, API contract, capability rule, dependency rule, performance invariant, serialization format, or module activation requirement.

## Current Native Dependency Inventory

These dependencies already exist or are planned through CMake wrapper aliases. Research should evaluate whether each is sufficient, whether it should remain hidden behind Octaryn APIs, and what gaps remain.

### Native Support And Core Utility Dependencies

| Alias | Version/Tag | Current Intended Use | Expected Owner Boundary |
| --- | --- | --- | --- |
| `octaryn::native_threads` | system Threads | native threading primitive for host-owned scheduler implementation | client/server/native support only |
| `octaryn::deps::spdlog` | `v1.17.0` | native logging | native logging support, client/server/tools |
| `octaryn::deps::cpptrace` | `v1.0.4` | stack traces and crash diagnostics | diagnostics/executable crash paths |
| `octaryn::deps::mimalloc` | `v3.3.1` | allocator backend | native memory support only |
| `octaryn::deps::tracy` | `v0.13.1` | profiling | client/server/tools/native support |
| `octaryn::deps::taskflow` | `v4.0.0` | job graph and worker execution | host-owned scheduler/native jobs |
| `octaryn::deps::eigen` | `5.0.0` | math/linear algebra | shared value math or owner-local optimized math |
| `octaryn::deps::unordered_dense` | `v4.8.1` | dense maps/sets | owner-local data structures |
| `octaryn::deps::glaze` | `v7.4.0` | JSON/metadata serialization | shared pure contracts, client settings, server persistence, basegame tools, root tools |
| `octaryn::deps::zlib` | `v1.3.2` | compression | server persistence, asset tools, support wrappers |
| `octaryn::deps::lz4` | `v1.10.0` | fast compression | server saves/caches, tools |
| `octaryn::deps::zstd` | `v1.5.7` | stronger compression | server saves/caches, tools |

### Client And Presentation Dependencies

| Alias | Version/Tag | Current Intended Use | Expected Owner Boundary |
| --- | --- | --- | --- |
| `octaryn::deps::sdl3` | `release-3.4.4` | windowing, input, SDL GPU, platform events | client only, isolated tools if needed |
| `octaryn::deps::sdl3_image` | `3.4.2` tarball | image loading | client UI/assets and asset import tools |
| `octaryn::deps::sdl3_ttf` | `release-3.2.2` | text rendering | client UI/overlays/tools |
| `octaryn::deps::imgui` | commit `285b38e2a7cfb2850ef27385f4e70df0f74f6b97` | immediate-mode debug/editor UI | client debug UI/tools |
| `octaryn::deps::implot` | commit `e6c36daf587b5eafebb533af1826b6d114b45421` | debug/profiling plots | client debug UI/tools |
| `octaryn::deps::implot3d` | commit `eb4ccd75f34b07646dfefb13b14f2df728bfd7ca` | debug 3D plots | client debug UI/tools |
| `octaryn::deps::imgui_node_editor` | commit `432c515535f4755c89235d58e71343c7c62ed317` | node/editor tooling | tools first, explicit client debug/editor only |
| `octaryn::deps::imguizmo` | commit `bbf06a1b0a1f18668acc6687ae283d6a12368271` | transform gizmos | tools first, explicit client debug/editor only |
| `octaryn::deps::imanim` | commit `51b78e795cf4d64f7d016d148b46a02e837e4023` | UI/editor animation helpers | tools first |
| `octaryn::deps::imfiledialog` | commit `c9819dd90450262efe7682839bb751c38173e1d8` | debug/editor file dialogs | tools/client debug only |
| `octaryn::deps::openal` | `1.25.1` | audio | client audio |
| `octaryn::deps::miniaudio` | `0.11.25` | audio helpers | client audio |
| `octaryn::deps::ozz_animation` | `0.16.0` | runtime skeletal animation | client animation runtime, basegame content data |

### Shader, Asset, And Tool Dependencies

| Alias | Version/Tag | Current Intended Use | Expected Owner Boundary |
| --- | --- | --- | --- |
| `octaryn::deps::shaderc` | `v2026.2` | shader compilation | shader tooling only |
| `octaryn::deps::shadercross` | commit `6b06e55c7c5d7e7a09a8a14f76e866dcfad5ab99` | SDL shader cross-compile path | shader tooling only |
| `octaryn::deps::spirv_tools` | `vulkan-sdk-1.4.341.0` | SPIR-V validation/optimization | shader tooling only |
| `octaryn::deps::spirv_cross` | `vulkan-sdk-1.4.341.0` | shader reflection/cross-compile | shader tooling only |
| `SPIRV-Headers` | `vulkan-sdk-1.4.341.0` | shader tool dependency | shader tooling only |
| `glslang` | `vulkan-sdk-1.4.341.0` | shader tool dependency | shader tooling only |
| `octaryn::deps::fastgltf` | `v0.9.0` | glTF import | asset import tools, client runtime only if intentional |
| `octaryn::deps::ktx` | `v4.4.2` | texture containers/GPU texture pipeline | asset tools and client texture loading |
| `octaryn::deps::meshoptimizer` | `v1.1.1` | mesh optimization | import processing tools, client intentional runtime |

## Current Managed Dependency Inventory

Pinned package versions:

| Package | Version | Current Policy |
| --- | --- | --- |
| `Arch` | `2.1.0` | approved module package for ECS; basegame/private owner-local; never shared public API |
| `Arch.LowLevel` | `1.1.5` | pinned transitive/support package; must be explicitly classified before module runtime use |
| `Arch.System` | `1.1.0` | approved module package for system update patterns |
| `Arch.System.SourceGenerator` | `2.1.0` | analyzer/private only where defining Arch systems |
| `Arch.EventBus` | `1.0.2` | approved module package for gameplay events, basegame/private owner-local |
| `Arch.Relationships` | `1.0.1` | approved module package for ECS relationships, basegame/private owner-local |
| `LiteNetLib` | `2.1.3` | host-only client/server transport; never basegame/shared/modules |
| `LiteEntitySystem` | `1.2.2` | host-only client/server replication/sync; never basegame/shared/modules |
| `Collections.Pooled` | `2.0.0-preview.27` | pinned support package; classify carefully before module use |
| `CommunityToolkit.HighPerformance` | `8.2.2` | pinned support package; classify carefully before module use |
| `Humanizer.Core` | `2.2.0` | pinned support package; classify carefully before module use |
| `Microsoft.Bcl.AsyncInterfaces` | `5.0.0` | pinned support package; classify carefully before module use |
| `Microsoft.CodeAnalysis.*` | `4.1.0` | analyzer/tooling support; not runtime module API |
| `Microsoft.CodeAnalysis.Analyzers` | `3.3.3` | analyzer/tooling support |
| `Microsoft.Extensions.ObjectPool` | `7.0.0` | pinned support package; classify carefully before module use |
| `Microsoft.NETCore.Platforms` | `1.1.0` | build/runtime metadata support |
| `NETStandard.Library` | `1.6.1` | compatibility/build support |
| `System.Composition.*` | `1.0.31` | composition/tooling support; deny broad module discovery unless explicitly approved |
| `ZeroAllocJobScheduler` | `1.1.2` | pinned support package; modules still cannot own threads/schedulers |

Current package policy:

- Allowed module direct packages: `Arch`, `Arch.EventBus`, `Arch.Relationships`, `Arch.System`, `Arch.System.SourceGenerator`.
- Allowed host direct packages: allowed module packages plus `LiteNetLib` and `LiteEntitySystem`.
- Host-only packages: `LiteNetLib`, `LiteEntitySystem`.
- `octaryn-shared` should stay package-free/BCL-only unless a contract-only dependency is deliberately approved.
- `Directory.Packages.props` pins versions only; it is not permission for module use.
- Any transitive package reachable from modules needs explicit allow/deny rules, owner, purpose, scope, and enforcement location.

## Capability Model To Preserve

Current and planned capabilities should stay explicit and deny-by-default. Research should expand this list where needed.

Known capability areas:

```text
content.blocks
content.items
content.entities
content.ui
gameplay.systems
gameplay.interactions
world.queries.read
world.blocks.edit.intent
entities.spawn.intent
inventory.mutate.intent
ui.contribute
input.actions
math.core
geometry.queries
random.deterministic
time.tick
diagnostics.module
physics.declare
physics.query
physics.intent
network.replicated_components
network.messages
persistence.components
native.systems
```

Research should identify missing capabilities for:

- fluids
- gases
- recipes
- loot
- tags
- worldgen
- biomes/features
- audio contributions
- animation contributions
- UI style/theme contributions
- localization
- assets
- data migrations
- save schema declarations
- debug surfaces
- editor tools
- tests/probes
- multiplayer compatibility declarations
- mod dependency declarations
- content override/extension rules
- server admin commands
- permission/security model

## Required Research Output Format

Produce a structured report with these sections:

1. Executive architecture summary.
2. Complete system inventory: every major engine/game/module/mod system that must be planned.
3. Owner map: client, server, shared, basegame, tools, cmake.
4. Library map: current libraries, missing libraries, recommended libraries, rejected libraries, and owner placement.
5. ECS backend design options and recommendation.
6. C# API ergonomics design, with examples.
7. Component model, entity model, item model, block model, and UI model.
8. Module/game/mod boundary model.
9. Capability and sandbox model.
10. Networking and replication model.
11. Physics model.
12. Persistence/save model.
13. UI system model for screen-space and world-space UI.
14. Input/action model.
15. World model, chunking, 512-block height, coordinate system, and world bounds.
16. Fluid/gas/liquid/block interaction model.
17. Asset/content pipeline model.
18. Tooling and editor model.
19. Build/package/dependency model.
20. Validation/probe model.
21. Performance targets and data-oriented design risks.
22. Security, mod trust, package enforcement, and binary inspection.
23. Milestones and migration phases.
24. Gaps, unresolved decisions, and high-risk unknowns.
25. Research references to inspect next.

For every system, include:

- owner
- source of truth
- public API surface
- native backend responsibility
- managed/frontend responsibility
- data formats
- capabilities needed
- validation required
- dependencies used
- risks
- open questions
- suggested milestone

For every proposed dependency, include:

- exact purpose
- owner placement
- whether it is module-facing or host-hidden
- why existing dependencies are insufficient
- alternatives compared
- license/risk questions to verify
- build/platform concerns
- validation required
- reason to reject it if it does not fit

## Systems That Must Be Planned

Do not skip these. Add any missing systems you identify.

### 1. Platform, Host, And Process Lifecycle

Research:

- process startup/shutdown
- owner host bootstrap
- client app lifecycle
- server lifecycle
- tool lifecycle
- config loading
- command line
- environment policy
- crash handling
- logging
- diagnostics
- hot reload boundaries if any
- developer debug mode
- release mode
- module activation order
- owner bundle discovery
- hostfxr/native bridge startup
- ABI version checks
- failure-path reporting

Hard boundary:

- no generic runtime bucket
- no `Engine` namespace
- host internals are not module APIs

### 2. ECS Backend

Research:

- native C++ ECS backend vs managed Arch frontend split
- archetype storage
- chunked storage
- sparse sets
- entity IDs
- component IDs
- stable handles
- generation counters
- structural changes
- deferred command buffers
- deterministic iteration
- query caching
- read/write access declarations
- system graph scheduling
- server authority world
- client presentation world
- prediction ECS world
- replicated component storage
- persistent component storage
- component schema registration
- ABI-safe component data
- C# declaration to C++ storage mapping
- reflection-free registration
- code generation options
- binary component layout validation
- debug inspection
- profiler markers

Compare:

- custom native ECS
- EnTT
- Flecs
- Arch-only managed ECS
- hybrid Arch declarations with native execution/storage

Do not expose raw ECS storage to modules.

### 3. Scheduler And Threading

Research:

- one main thread
- one coordinator thread
- worker pool with at least two workers and scalable core count
- task graph scheduling
- tick phases
- frame phases
- deterministic barriers
- cancellation
- resource read/write tracking
- no hidden global mutable state
- no module-created threads
- no `Task.Run` from modules
- no private timers or schedulers from modules
- server tick commits
- client presentation handoff
- tool/offline jobs
- Tracy validation and profiling

Current planned native scheduler dependency:

- `taskflow v4.0.0`

Research whether Taskflow is enough or if a custom thin scheduler layer is needed above it.

### 4. Module, Game, And Mod Loading

Research:

- game module manifest schema
- mod manifest schema
- module API versioning
- dependency declarations
- load order
- content registration order
- capability requests
- package allowlists
- framework API allowlists
- binary inspection
- source validation
- asset declaration validation
- multiplayer compatibility declaration
- save compatibility declaration
- module signing/trust options
- server-required mod negotiation
- client-only mod policy
- server-only module policy
- basegame as bundled module
- external games using same path
- no broad reflection/service discovery
- no arbitrary NuGet dependencies

### 5. Entity System

Research:

- entity definition API
- components
- systems
- lifecycle
- spawn/despawn intent
- authority ownership
- transform
- hierarchy/relationships
- physics body attachment
- inventory attachment
- health/damage/status effects
- AI
- movement
- animation state
- persistence
- replication
- prediction
- interpolation
- interest management
- ownership transfer
- custom module components
- custom module systems
- native systems for high-performance logic
- script/system ordering
- events
- commands
- queries
- debugging and inspection

Target authoring style should be very easy:

```csharp
entities.Define("octaryn.basegame.entity.player")
    .Component<Transform>()
    .Component<Health>()
    .Component<PlayerInput>()
    .Component<PhysicsBody>()
    .Replicate<Transform>(ReplicationMode.Interpolated)
    .Persist<Health>();
```

The host should compile declarations into backend storage, networking, persistence, and validation.

### 6. Blocks And World Interaction

Research:

- block IDs
- block definitions
- block states
- block components
- block tags
- block collision
- block hardness
- block drops
- block interaction
- block ticking
- random ticks
- block entities/tile entities if needed
- block replacement
- block fluids/gases interaction
- server authority for edits
- client presentation and mesh generation
- world queries
- chunk snapshots
- neighborhood snapshots
- packed mesh plans
- atlas/material presentation data
- save format
- replication format
- content override/extension rules
- migration from old architecture

Core terrain baseline is flat blank terrain. Basegame adds real content and rules.

### 7. Items, Inventory, Equipment, And Recipes

Research:

- item IDs
- item definitions
- item components
- stack rules
- durability
- use actions
- cooldowns
- tags
- tools
- containers
- inventory APIs
- equipment slots
- crafting
- recipes
- fuel
- loot
- drops
- creative/dev inventories
- server authority
- UI bindings
- persistence
- replication
- mod extension and replacement rules

### 8. UI System

Research:

- retained-mode vs immediate-mode module UI API
- declarative UI model
- data binding
- actions
- commands
- focus
- input routing
- screen-space layout
- world-space layout
- render-to-texture
- 2D UI on 3D quads
- text rendering
- font fallback
- text shaping
- localization
- accessibility
- controller navigation
- keyboard/mouse capture
- drag/drop
- tooltips
- animation/transitions
- styling/themes
- invalidation/diffing
- UI state as ECS components
- UI model persistence where needed
- client-owned rendering
- server-validated UI actions
- debug UI vs product UI split

Evaluate whether SDL3_ttf is enough and whether Octaryn needs additional text/layout libraries such as HarfBuzz, FreeType policy, Yoga/Taffy-style layout, or a custom layout engine. Any dependency recommendation must preserve owner boundaries.

### 9. Input And Action Mapping

Research:

- raw input collection
- action maps
- contexts
- rebinding
- gamepad/controller support
- keyboard/mouse
- text input
- UI focus
- world-space UI pointer/raycast routing
- input commands
- prediction inputs
- server validation
- replay/determinism
- basegame action declarations
- mod action contributions
- accessibility bindings

### 10. Game State

Research:

- global game state ECS components
- world rules
- difficulty/progression
- teams/factions
- time/day state
- weather placeholder
- dimension/world state if needed
- game phase
- menu/front-end state
- save migration
- replication
- authority
- module-owned game state
- conflict rules between mods

### 11. Physics

Research:

- physics backend choice
- C++ host integration
- collision layers
- rigid bodies
- character movement
- constraints/joints
- triggers/sensors
- ray casts
- shape casts
- overlap queries
- broadphase
- block/world collision
- fluid/gas interaction hooks
- entity/world interaction
- server authority
- client prediction
- interpolation
- determinism expectations
- rollback/reconciliation
- save/restore
- debug draw
- profiling

Compare:

- Jolt
- PhysX
- Bullet
- ReactPhysics3D
- custom voxel collision plus narrow physics library

Modules must only see Octaryn physics declarations, queries, events, and intents. No raw backend world access.

### 12. Networking And Replication

Research:

- LiteNetLib host-only transport role
- LiteEntitySystem host-only replication role
- message schema
- component replication declaration
- reliable/unreliable channels
- command frames
- snapshots
- delta compression
- interpolation
- prediction
- reconciliation
- rollback if needed
- interest management
- ownership
- authority handoff
- entity spawn/despawn replication
- block edit replication
- inventory replication
- UI action messages
- anti-cheat/validation
- server browser/session future hooks
- mod compatibility handshake
- content hash negotiation
- protocol versioning
- serialization
- packet budgets
- bandwidth profiling

Transport code belongs in client/server. Shared defines message contracts only. Modules do not see sockets, LiteNetLib types, or transport sessions.

### 13. Persistence And Save Format

Research:

- world save layout
- player save layout
- entity component persistence
- block/chunk persistence
- item/inventory persistence
- game state persistence
- module save namespaces
- schema versioning
- migrations
- compression choices
- transactional writes
- corruption handling
- backups
- async save jobs
- server ownership
- client cache boundaries
- content/module compatibility checks
- save open/close lifecycle
- deterministic serialization
- binary vs JSON vs hybrid formats

Current compression dependencies include zlib, lz4, and zstd. Research how each should be used or rejected.

### 14. Fluids, Liquids, Gases, And Environmental Simulation

Research:

- liquid blocks
- gases
- spread rules
- pressure rules
- falling/flowing rules
- source blocks
- interactions with entities
- interactions with blocks
- fire/smoke/steam if planned
- server authoritative simulation
- native C++ kernels
- chunk boundaries
- scheduling
- replication
- persistence
- client presentation
- mod-defined fluid/gas behavior
- validation of custom behavior
- performance budgets

Important: core world is blank/flat. Basegame defines real liquid/gas/content rules. Heavy simulation should run in C++ backend through explicit APIs.

### 15. World Generation And Terrain

Research:

- flat blank core world
- basegame worldgen modules
- chunk generation
- feature placement
- biome rules if needed
- deterministic RNG
- noise API
- content-driven generation
- server authority
- async generation jobs
- save interaction
- streaming
- world height 512
- vertical centering
- coordinate systems
- dimensions/world spaces if needed
- mod extension rules
- validation and profiling

### 16. Rendering And Client Presentation

Research:

- SDL3 GPU/Vulkan path
- render graph or frame graph needs
- shader pipeline
- shader variants
- material system
- texture atlas
- KTX texture flow
- mesh upload
- chunk mesh presentation
- fluid rendering
- entity rendering
- animation rendering
- particles
- UI rendering
- world-space UI
- debug rendering
- visibility/culling
- LOD
- asset streaming
- frame pacing
- profiling
- RenderDoc capture workflow

Do not work on DDGI/skylight until the dedicated plan exists. Research may identify future lighting plan topics only.

### 17. Assets, Content Pipeline, And Data

Research:

- content IDs
- data schema
- module asset declarations
- content validation
- block/item/entity JSON or binary schemas
- texture atlas build
- model import
- animation import
- glTF policy
- KTX texture policy
- meshoptimizer use
- shader compile pipeline
- basegame tools
- root tools
- generated source
- asset hashes
- package layout
- hot reload if allowed
- editor importers
- mod asset validation
- missing asset handling

Current asset/tool dependencies include fastgltf, KTX, meshoptimizer, shaderc, shadercross, SPIR-V tools, SPIRV-Cross, SDL3_image, and ozz-animation.

### 18. Audio

Research:

- OpenAL Soft role
- miniaudio role
- clip/streaming audio
- spatial audio
- mixer
- entity audio components
- UI audio
- basegame sound declarations
- mod audio assets
- attenuation
- occlusion if planned
- music/state transitions
- client-only playback
- server-authoritative sound events if needed

### 19. Animation

Research:

- ozz-animation role
- skeletal animation
- item/block animation
- entity animation state
- client-owned animation runtime
- basegame animation declarations
- mod assets
- animation events
- networking of animation state
- prediction/interpolation
- tooling/import pipeline

### 20. Diagnostics, Profiling, And Debugging

Research:

- structured logging
- module diagnostics API
- crash reports
- stack traces
- Tracy zones
- GPU captures
- performance counters
- validation reports
- module error reporting
- server console diagnostics
- client debug overlays
- debug UI boundaries
- logs under `logs/<owner>/`
- no module direct console/file logging unless approved

Current dependencies include spdlog, cpptrace, and Tracy.

### 21. Tools And Editors

Research:

- workspace control UI
- shader compiler
- atlas builder
- content validators
- module validators
- package policy validators
- save validators
- schema generators
- ECS/component codegen
- API docs generator
- editor/debug world tools
- performance capture wrappers
- package/mod build tooling
- external game template tooling

Basegame-specific tools stay in `octaryn-basegame/Tools/`. Repo-wide tools stay in root `tools/`.

### 22. Security, Sandbox, And Dependency Enforcement

Research:

- package allowlist
- transitive package validation
- framework API group allowlist
- denied namespaces/types/members
- binary inspection
- source inspection
- native interop denial
- reflection/dynamic loading denial
- filesystem/process/network/threading denial
- module trust levels
- signed packages
- server/client mod compatibility
- content hash validation
- manifest validation
- failure UX
- admin overrides if any

Default stance is deny-by-default.

### 23. Developer Experience And API Ergonomics

Research:

- fluent APIs
- builder patterns
- source generation
- analyzer errors
- excellent error messages
- component declaration examples
- system declaration examples
- UI examples
- physics examples
- networking examples
- item/block/entity examples
- mod templates
- documentation generation
- examples that do not leak internals
- API naming conventions
- capability request ergonomics
- debugging tools for mod authors

The API should feel elegant, powerful, and safer than typical game scripting APIs while still mapping to fast native systems.

### 24. Performance And Data-Oriented Design

Research:

- native backend layout
- ECS memory layout
- cache efficiency
- job scheduling overhead
- bridge overhead
- serialization overhead
- networking bandwidth
- chunk streaming
- mesh generation cost
- physics cost
- fluid/gas simulation cost
- UI update cost
- profiling targets
- benchmarks
- failure thresholds
- debug vs release build behavior

### 25. Build, Packaging, And Platform Support

Research:

- owner-partitioned build output
- owner bundles
- native archive format
- .NET managed outputs
- hostfxr bridge packaging
- Linux toolchain
- Windows cross toolchain
- dependency source cache
- third-party download policy
- reproducibility
- CI-ready commands later
- package layout
- distribution layout
- mod package layout
- game package layout
- logs and crash dumps per owner

### 26. Multiplayer And Mod Compatibility

Research:

- multiplayer compatibility manifests
- required/optional mods
- server-enforced module lists
- client-only UI/cosmetic mods
- server-only admin modules
- version ranges
- content ID conflicts
- content override policy
- save compatibility
- network protocol compatibility
- capability mismatches
- asset mismatches
- downgrade/upgrade policy

### 27. Future DDGI/Lighting Plan Placeholder

Research may list future questions for a dedicated DDGI/lighting plan:

- DDGI probe volumes
- skylight representation
- voxel GI inputs
- block opacity metadata
- client/server split
- content-driven material/opacity declarations
- performance capture plan
- migration from old CPU skylight

But do not include active DDGI/skylight implementation tasks in the current AAA plan.

## Research Questions To Answer

Answer these directly:

1. Are the current native and managed libraries enough for an AAA-quality modular voxel platform?
2. Which missing libraries should be considered, and which should be avoided?
3. Should Octaryn use custom native ECS storage, EnTT, Flecs, or another ECS backend while preserving C# authoring through explicit APIs?
4. What physics backend is best for this architecture, and how should the API hide it?
5. What UI layout/text stack is needed for screen-space and world-space UI?
6. Is SDL3_ttf enough for product UI, or is text shaping/font fallback/localization support missing?
7. What networking model should sit above LiteNetLib/LiteEntitySystem without exposing them?
8. What save format should support voxel chunks, entities, module data, migration, compression, and corruption safety?
9. How should basegame, games, and mods define custom components and systems without exposing unsafe internals?
10. How should native C++ systems be allowed for performance while keeping modules capability-scoped and safe?
11. What validations must exist before external mods can run?
12. What data formats should be JSON, binary, generated source, or hybrid?
13. What editor/tools should exist before content scale grows?
14. What system boundaries are most likely to rot into a monolith if not planned now?
15. What is the staged migration plan from current code to the full target architecture?

## Libraries To Specifically Evaluate As Missing Or Optional

Research these only as candidates. Do not assume they should be added.

Native ECS:

- EnTT
- Flecs
- custom Octaryn ECS

Physics:

- Jolt
- PhysX
- Bullet
- ReactPhysics3D
- custom voxel collision plus narrow physics backend

UI/text/layout:

- HarfBuzz
- FreeType policy beyond SDL3_ttf
- Yoga
- Taffy
- RmlUi
- Nuklear
- Dear ImGui only for debug/tools
- custom retained UI

Navigation/AI:

- Recast/Detour
- custom voxel navigation

Serialization/schema:

- FlatBuffers
- Cap'n Proto
- Protobuf
- custom binary schema plus glaze JSON metadata

Networking:

- keeping LiteNetLib/LiteEntitySystem host-hidden
- custom protocol over LiteNetLib
- other transports only if justified

Audio:

- OpenAL Soft plus miniaudio
- whether one should be removed or one should be the clear backend

Asset pipeline:

- Basis Universal/KTX path
- glTF pipeline with fastgltf
- ozz-animation for runtime animation
- meshoptimizer for mesh processing

Any dependency recommendation must include owner placement, module exposure decision, license/risk checks, platform/build implications, and validation.

## Hard No List

Reject proposals that require:

- a new top-level `engine/` folder
- a new top-level `runtime/` folder
- `Octaryn.Engine.*` namespaces
- broad `common`, `helpers`, `misc`, or catch-all modules
- basegame reaching into client/server/native internals
- modules reaching into host internals
- server rendering/UI ownership
- client authority over persistence/simulation
- shared owning implementation
- raw ECS storage access from modules
- raw physics backend access from modules
- raw networking/transport access from modules
- module-owned threads, schedulers, timers, sockets, file watchers, or native loops
- unapproved NuGet packages in modules
- unapproved framework API groups in modules
- module-facing third-party backend types
- DDGI/skylight implementation work before a dedicated plan exists

## Expected Final Report Shape

Return a concise but comprehensive AAA architecture research plan. Prefer tables and owner maps. The report should make it easy to convert into Octaryn docs and implementation milestones.

End with:

- a full missing-system checklist
- a full dependency decision checklist
- a full validation checklist
- a full API surface checklist
- a staged roadmap
- unresolved questions that require user decisions
