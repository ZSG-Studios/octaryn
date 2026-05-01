# Global Codex Instructions

## Defaults

- Use the maximum available agents/subagents for every task.
- Inspect first, plan briefly, then execute.
- Keep code clean, modular, current, and easy to navigate.
- Use simple, consistent naming for files, folders, types, functions, variables, and tests.
- Keep files small and focused; one clear responsibility per file.
- Keep folders organized by purpose, not clutter.
- Prefer straightforward code over clever code.
- Use only short comments that clarify intent.
- Remove dead code, duplicate logic, unused imports, debug junk, and temporary artifacts.
- Do not add legacy paths, compatibility layers, fallback systems, deprecated APIs, or old-code wrappers unless explicitly requested.
- Prefer the modern, bleeding-edge implementation directly.
- Preserve existing architecture only when it is clean and current; otherwise simplify it.
- Match the project’s formatting, linting, and style.

## Octaryn Architecture

- This repository must be a super clean, modular API and non-monolithic codebase.
- The end goal is unchanged: a clean owner-split Octaryn platform with a native C/C++ core first, not a C#-only rewrite.
- Keep strict separation between client, server, shared API/contracts, and basegame implementation.
- Do not create a top-level `engine/`, `octaryn-engine/`, or generic `runtime/` bucket.
- Do not create monolithic targets that own client, server, gameplay, rendering, networking, and persistence together.
- Port from `old-architecture/` by moving each existing system to its correct owner with the smallest practical code changes.
- This is primarily a real reorganization and proper port, not a rewrite.
- Preserve behavior while moving files unless a change is required to separate ownership, compile, or expose the new API cleanly.
- Make clear source-to-destination maps before moving code so every old file has an intentional landing zone or a documented removal reason.
- Do not copy old folder shapes blindly, but do keep existing implementation logic intact when it is already correct.
- Treat `old-architecture/` as source material only. It is not the destination architecture.
- Keep support code as focused named libs such as logging, diagnostics, jobs, memory, shader tooling, or dependency wrappers.
- Do not use vague catch-all folders or targets for platform/runtime/support code.

## Porting Strategy

- Start each porting pass by inventorying the old files in scope and assigning each one to client, server, basegame, shared, cmake, tools, or removal.
- Prefer mechanical moves and namespace/target renames before behavioral edits.
- Keep diffs reviewable: separate pure moves from logic changes whenever possible.
- If a file mixes responsibilities, split it along the new ownership boundary instead of placing the whole file in a generic shared location.
- Do not introduce compatibility shims back to old paths unless the user explicitly asks.
- Do not keep old `Engine` API names as wrappers. Replace them with the new client/server/basegame/shared API names directly.
- Preserve current rendering, world, player, persistence, and shader behavior until a later task explicitly changes behavior.
- Use `old-architecture/` only as the source of truth during the port. New code should live in the new roots.

## Ownership Boundaries

- `octaryn-client/` owns presentation: windowing, input, rendering, shaders, GPU upload, audio, UI, overlays, local prediction, and client host code.
- `octaryn-server/` owns authority: simulation, validation, persistence, world saves, server ticks, replication, transport hosting, and server-side physics.
- `octaryn-shared/` owns the clean C# API and shared contracts used by client, server, and basegame: host interfaces, tick contracts, commands, snapshots, registries, queries, IDs, positions, replication contracts, and pure shared constants.
- `octaryn-basegame/` is the default bundled game module that implements high-level gameplay rules and content on top of shared APIs: blocks, items, materials, recipes, tags, loot, feature/biome rules, player rules, interactions, and base content data.
- Treat `octaryn-basegame/` as the first bundled game module, not a privileged engine-internals bucket. Future games, mods, and modules must use the same explicit API and validation path.
- `octaryn-basegame/Tools/` owns tools that are specific to basegame content, such as texture atlas building, content import, content validation, block/item data generation, and demo-game asset processing.
- Root `tools/` owns repo-wide developer operations, build orchestration, profiling capture wrappers, and utilities that are not specific to one game/content package.
- `cmake/` owns build/dependency tooling.
- Old build helpers, old CMake modules, old desktop helper tools, and old profiling wrappers belong under `old-architecture/` until they are intentionally ported.
- The old atlas builder is basegame-specific content tooling and belongs under `octaryn-basegame/Tools/`, not root `tools/`.
- Networking packages and transport implementation belong in client/server layers, not in `octaryn-basegame/`.
- Persistence implementation belongs to server unless a file is a pure shared data contract.
- Rendering, GPU upload, shader pipelines, windowing, audio, and UI never belong to server.
- Authoritative edits, validation, save ownership, and simulation never belong to client.
- Product-specific gameplay behavior never belongs in `octaryn-shared/`; it belongs in `octaryn-basegame/` or another game project.
- Product-specific asset/content tooling never belongs in root `tools/` when it only serves `octaryn-basegame/`; move it under `octaryn-basegame/Tools/`.
- C# ECS/gameplay and client/server networking are intentionally used where they fit best; they are not legacy or fallback paths.
- C/C++ owner code may drive managed ECS or networking through explicit client/server owner bridges. Basegame is reached through shared contracts and validated module entry points only; game modules and mods must never see bridge internals.

## Module Folder Shape

- Each main module root should use the same top-level folder vocabulary where applicable: `Source/`, `Assets/`, `Shaders/`, `Tools/`, `Data/`, and project/build files.
- `Source/` owns code for that module.
- `Source/Native/` is the preferred landing zone for C and C++ implementation files when a module has mixed native/managed code or native support libraries.
- `Source/Managed/` is the preferred landing zone for C# implementation files when a module has mixed native/managed code.
- `Source/Libraries/` owns small module-local C/C++ library targets before they are promoted to clearer domain folders.
- `Assets/` owns runtime assets for that module.
- `Shaders/` owns shader source owned by that module.
- `Tools/` owns tools specific to that module.
- `Data/` owns structured content/config/data specific to that module.
- Keep domain subfolders under `Source/` instead of scattering implementation folders at the module root.
- This codebase is primarily C, C++, and native libraries. Do not design the roots as if the C# projects are the only product.
- For `octaryn-shared/`, `Source/` owns API/contracts/value types. `Assets/`, `Shaders/`, and `Data/` should stay empty unless a real shared, implementation-free need appears.
- For `octaryn-server/`, `Shaders/` should stay empty unless a real server-owned compute/offline shader need appears.
- For `octaryn-basegame/`, content-specific shader and atlas inputs may live under `Shaders/`, `Assets/`, `Data/`, and `Tools/` because basegame is the default/demo content package.

## Build And Log Layout

- Build outputs are generated and ignored. Keep them organized under `build/<owner>/`.
- Use `build/client/` for client builds, generated client assets, and client native artifacts.
- Use `build/server/` for dedicated server builds and server native artifacts.
- Use `build/basegame/` for basegame managed/content build outputs.
- Use `build/shared/` for shared API/contract builds only.
- Use `build/old-architecture/` only for old architecture builds while porting.
- Use `build/tools/` for repo-wide tool builds and `build/dependencies/` for dependency caches when they are not owner-specific.
- Logs are generated and ignored. Keep them organized under `logs/<owner>/`.
- Use `logs/client/`, `logs/server/`, `logs/basegame/`, `logs/shared/`, `logs/build/`, `logs/tools/`, and `logs/old-architecture/` instead of dumping logs at root.
- Old architecture scripts must write to `build/old-architecture/` and `logs/old-architecture/`.
- Active root `cmake/` and `tools/` should stay clean for new architecture support only; do not leave old-engine scripts there.

## CMake And Platform Build Rules

- Keep root `cmake/` split by responsibility: `Shared/` for repo-wide build policy, `Owners/` for owner target construction, `Dependencies/` for dependency aliases/groups, `Platforms/` for host/platform facts, and `Toolchains/` for compiler/target files.
- Do not copy old monolithic CMake modules into root `cmake/`. Port old `old-architecture/cmake/` behavior by splitting it into shared policy, owner targets, dependency wrappers, platform modules, and toolchains.
- Toolchain files must describe compilers, target triples, sysroots, find-root behavior, and target platform knobs only. They must not create Octaryn targets, fetch dependencies, or set gameplay/render/server policy.
- Keep platform logic isolated: Windows policy under `cmake/Platforms/Windows/`, MinGW specifics under Windows/MinGW platform and toolchain files, Linux distro-family policy under `cmake/Platforms/Linux/`, BSD policy under `cmake/Platforms/BSD/`, and macOS SDK/framework/signing policy under `cmake/Platforms/MacOS/`.
- Linux distro differences should be represented as family modules only when real package/tool behavior differs, such as Arch-family, Debian-family, and Fedora-family dependency hints.
- Windows cross-builds from Linux must use explicit MinGW toolchains under `cmake/Toolchains/Windows/MinGW/`; do not hide MinGW behavior inside generic Windows or Linux logic.
- Owner CMake modules may call shared helpers and dependency aliases, but must not contain host platform detection. Platform modules report capabilities; owner targets decide whether to use them.
- New root presets must target owner outputs such as `octaryn_client_bundle`, `octaryn_server`, `octaryn_basegame`, `octaryn_shared`, and tools. Old `octaryn_engine_*` presets remain only under `old-architecture/` until retired.
- Build and dependency outputs must stay owner-partitioned: owner builds under `build/<owner>/`, third-party caches under `build/dependencies/`, and logs under `logs/<owner>/` or `logs/build/`.
- Active root `cmake/` placeholder folders are not implementation. Do not claim Windows, MinGW, Linux, BSD, macOS, owner target, dependency, or preset coverage until the concrete CMake module exists and has a targeted configure check when practical.
- Native Windows/MSVC support is separate from MinGW cross-build support. Old Windows presets are MinGW cross-builds unless a native Windows preset/toolchain path is explicitly added.
- Include distro-family modules only for real package/tool differences; current planned families include Arch, Debian, Fedora, and Suse/openSUSE because the old dependency installer has distinct logic for them.

## Multiplayer And C# Basegame API Direction

- Organize the port so multiplayer is a first-class future target, even before transport is fully implemented.
- Server must become authoritative for world edits, simulation, validation, persistence, and replication.
- Client should be prepared for local prediction and presentation of server snapshots without owning authority.
- Shared networking contracts should stay explicit and API-shaped: client commands, server snapshots, replication IDs, tick IDs, stable value types, and interfaces needed by client/server/basegame.
- Transport code belongs in client/server projects; shared only defines message shapes and IDs.
- The C# API belongs in `octaryn-shared/`, because `octaryn-basegame/` is a default/demo game implementation rather than the platform API.
- `octaryn-shared/` should expose clean contracts for ticks, commands, world queries, block/item/content registration, interactions, snapshots, and host services.
- Native host bridges should be renamed away from `Octaryn.Engine.Api` and shaped around client host, server host, shared APIs/contracts, and basegame implementations.
- Basegame logic should not depend on client rendering, server persistence internals, or transport implementation details.
- When a native system must call into C# basegame logic, define the smallest explicit API needed rather than exposing broad native internals.
- C# ECS or host networking may be exposed to C/C++ only through explicit client/server host bridges when that is the cleanest route; the core engine direction remains C/C++ first, and those bridges are not module permissions.
- Game modules and mods may only depend on explicit, approved APIs exposed by `octaryn-shared/` and approved host interfaces. Do not expose broad client, server, native, reflection, filesystem, rendering, persistence, transport, or internal service access to gameplay code.
- Any new game/module/mod API surface must be intentionally named, documented by its contract shape, capability-scoped, and reviewed as part of the shared API boundary before use.
- Module loading is deny-by-default: validate module manifests, requested APIs, requested .NET packages/framework API groups, dependencies, content registrations, assets, and multiplayer compatibility before activation.
- Current migration debt does not weaken the target boundary. Treat old `Octaryn.Engine.Api`, unmanaged exports, unsafe native bridges, and any basegame host-only package references as Phase 0 blockers to remove, not as allowed patterns.
- Threading is host-owned: one main thread, one coordinator thread, and a worker pool with at least two workers that can scale to available cores.
- All computation systems and gameplay logic must run through the coordinator-scheduled worker pool or through approved host APIs backed by that pool.
- Basegame, game modules, and mods must not create raw threads, custom task schedulers, timers, unmanaged worker loops, or private worker pools.
- New scheduled systems must be thread-safe, declare read/write access and ordering dependencies, avoid hidden global mutable state, and never block the main thread for worker completion except at explicit host barriers.

## File And API Shape

- Each file should expose one focused API surface.
- Prefer small files with clear names over broad files that mix responsibilities.
- Name modules after exact ownership and behavior, not generic technical buckets.
- Avoid names like `manager`, `helpers`, `misc`, `common`, `stuff`, `data`, or `utils` unless there is already a strict local convention requiring them.
- Public APIs must make the ownership boundary obvious from their namespace, folder, target, and file name.
- Shared APIs must stay minimal, stable, and implementation-free. Do not leak client rendering, server persistence internals, transport implementation, or product-specific gameplay policy into shared contracts.
- Public APIs for game modules and mods must be allowlisted by contract, not discovered from implementation assemblies or internal namespaces.
- Keep mod-facing APIs narrow and capability-based: expose only the specific commands, queries, registries, events, and host services that gameplay code is allowed to use.
- When porting old code, split mixed files before adding new behavior.
- Remove dead, duplicate, debug, temporary, or compatibility code while porting.
- Prefer one API file per concept: command, snapshot, registry, query, host, tick, or system.

## .NET Ecosystem Library Policy

- Enforce an explicit allowlist for all .NET packages and framework APIs used by `octaryn-shared/`, `octaryn-basegame/`, game modules, and mods.
- `octaryn-shared/` should stay package-free or BCL-only unless a contract-only dependency is deliberately approved. Do not expose third-party package types in shared public APIs.
- Game modules and mods compile against `octaryn-shared` plus approved package/framework API groups only. They must not bring their own unapproved NuGet dependencies.
- Approved module packages today: `Arch`, `Arch.System`, `Arch.System.SourceGenerator` as analyzer/private only, `Arch.EventBus`, and `Arch.Relationships`.
- Host-only packages today: `LiteNetLib` and `LiteEntitySystem`; these belong in `octaryn-client/` and `octaryn-server/`, not basegame, mods, game modules, or shared contracts.
- `Directory.Packages.props` pins versions only; it is not permission for a module or project to reference a package.
- Approved module package entries must include owner, purpose, version policy, permitted runtime scope, validation rule, and enforcement location before use.
- Deny by default for module code: reflection/dynamic loading, scripting hosts, runtime code generation, dependency injection containers, networking stacks, filesystem access, filesystem watchers, native interop, process control, raw threading/task scheduling, environment variables, and direct host service discovery.
- Approved threading access for modules is only through host scheduling contracts; raw `System.Threading`, `Task.Run`, custom timers, and private worker pools remain denied unless the architecture plan and allowlists are updated first.
- Transitive packages are not automatically allowed. Any transitive runtime or build/analyzer package reachable from a module must have an explicit allow/deny rule, owner, purpose, scope, and enforcement location before the module is considered validated.
- Safe BCL access must be concrete enough for analyzer/source checks and assembly inspection. Broad labels like "safe BCL" are planning shorthand, not enforcement.
- Module diagnostics must go through approved host diagnostics APIs; direct console/stdout/stderr writes are transitional debt while the old unmanaged bridge exists.
- If a dependency or framework API group is needed, add it to the allowlist with owner, purpose, version policy, permitted runtime scope, and validation checks before use.

## Reference Implementations

- Before building, replacing, or redesigning a system that is meant to match another engine, game, shader pack, framework, or original implementation, inspect the available reference source first.
- Use local `refrances/` checkouts, original source trees, decompiled/source-visible implementations, official docs, and exact upstream repositories before inventing behavior.
- For Minecraft-parity work, check Minecraft/Iris/shader-pack/reference code and assets first, then map the behavior into this codebase’s architecture.
- Identify what the reference does at the mesh/data level, texture/asset level, shader level, runtime/update level, and edge-case level before writing the new implementation.
- Do not substitute a visually similar or guessed system when the goal is 1:1 behavior. If the reference cannot be inspected, say what is missing and keep the implementation scoped to verified behavior.

## Validation

- Do not use smoke tests as a validation path unless the user explicitly asks for a smoke test.
- Do not run `ctest` unless the user explicitly asks for `ctest`.
- For performance work, validate with direct runtime runs, targeted benchmarks, RenderDoc captures, Tracy captures, and focused profiling logs instead of smoke or `ctest` wrappers.
- For architecture-only blank structure work, verify with file tree inspection and empty-file checks instead of pretending a build is meaningful.

## Naming

- Use names that describe exact purpose.
- Avoid vague names like `helpers`, `misc`, `stuff`, `manager`, `data`, or `utils` unless the project already requires them.
- Keep naming consistent across source, headers, tests, folders, build files, and project files.
- Do not use `Engine` in new namespaces, folders, targets, or product names.
- Do not use `Runtime` as a new top-level product bucket. Use exact names like `ClientHost`, `ServerTick`, `NativeLogging`, or `ShaderCompiler` instead.

## Finish Check

Before final response, confirm:
- Max agents/subagents were used where applicable.
- The result is clean, modular, and organized.
- Naming is simple and consistent.
- Comments are minimal and useful.
- No legacy, compatibility, deprecated, duplicate, dead, or temporary code remains.
- No generic `engine/`, `octaryn-engine/`, or top-level `runtime/` structure was added.
- Client, server, shared API/contracts, and basegame implementation ownership stayed separate.
- Game modules and mods only see explicit approved APIs, not internal client/server/native implementation surfaces.
- .NET package and framework API usage follows the approved allowlist and no unapproved dependencies were introduced.
- Old-architecture files touched in the task were mapped to explicit destination owners.
- Old tools/CMake/build helpers stayed under `old-architecture/` unless intentionally ported.
- CMake changes kept shared policy, owner targets, dependency wrappers, platform modules, and toolchains separate; placeholder folders were not counted as implemented support.
- Build and log outputs are owner-partitioned under `build/<owner>/` and `logs/<owner>/`.
- Behavior was preserved unless a necessary boundary/API change was explained.
- Networking/multiplayer and C# basegame API boundaries were kept ready for future implementation.
- Builds, targeted checks, profiling runs, or structure checks were executed when practical, or explain why not.
