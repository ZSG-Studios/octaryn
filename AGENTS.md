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
- `octaryn-basegame/` is the default/demo game project that implements gameplay rules and content on top of shared APIs: blocks, items, materials, recipes, tags, loot, worldgen rules, player rules, interactions, and base content data.
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
- Use `build/shared/` for shared API/contract builds and shared dependency caches that are truly cross-owner.
- Use `build/old-architecture/` only for old architecture builds while porting.
- Use `build/tools/` for repo-wide tool builds and `build/dependencies/` for dependency caches when they are not owner-specific.
- Logs are generated and ignored. Keep them organized under `logs/<owner>/`.
- Use `logs/client/`, `logs/server/`, `logs/basegame/`, `logs/shared/`, `logs/build/`, `logs/tools/`, and `logs/old-architecture/` instead of dumping logs at root.
- Old architecture scripts must write to `build/old-architecture/` and `logs/old-architecture/`.
- Active root `cmake/` and `tools/` should stay clean for new architecture support only; do not leave old-engine scripts there.

## Multiplayer And C# Basegame API Direction

- Organize the port so multiplayer is a first-class future target, even before transport is fully implemented.
- Server must become authoritative for world edits, simulation, validation, persistence, and replication.
- Client should be prepared for local prediction and presentation of server snapshots without owning authority.
- Shared networking contracts should stay explicit and API-shaped: client commands, server snapshots, replication IDs, tick IDs, stable value types, and interfaces needed by client/server/basegame.
- Transport code belongs in client/server projects; shared only defines message shapes and IDs.
- The C# API likely belongs in `octaryn-shared/`, because `octaryn-basegame/` is a default/demo game implementation rather than the platform API.
- `octaryn-shared/` should expose clean contracts for ticks, commands, world queries, block/item/content registration, interactions, snapshots, and host services.
- Native host bridges should be renamed away from `Octaryn.Engine.Api` and shaped around client host, server host, shared APIs/contracts, and basegame implementations.
- Basegame logic should not depend on client rendering, server persistence internals, or transport implementation details.
- When a native system must call into C# basegame logic, define the smallest explicit API needed rather than exposing broad native internals.

## File And API Shape

- Each file should expose one focused API surface.
- Prefer small files with clear names over broad files that mix responsibilities.
- Name modules after exact ownership and behavior, not generic technical buckets.
- Avoid names like `manager`, `helpers`, `misc`, `common`, `stuff`, `data`, or `utils` unless there is already a strict local convention requiring them.
- Public APIs must make the ownership boundary obvious from their namespace, folder, target, and file name.
- Shared APIs must stay minimal, stable, and implementation-free. Do not leak client rendering, server persistence internals, transport implementation, or product-specific gameplay policy into shared contracts.
- When porting old code, split mixed files before adding new behavior.
- Remove dead, duplicate, debug, temporary, or compatibility code while porting.
- Prefer one API file per concept: command, snapshot, registry, query, host, tick, or system.

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
- Old-architecture files touched in the task were mapped to explicit destination owners.
- Old tools/CMake/build helpers stayed under `old-architecture/` unless intentionally ported.
- Build and log outputs are owner-partitioned under `build/<owner>/` and `logs/<owner>/`.
- Behavior was preserved unless a necessary boundary/API change was explained.
- Networking/multiplayer and C# basegame API boundaries were kept ready for future implementation.
- Builds, targeted checks, profiling runs, or structure checks were executed when practical, or explain why not.
