# API Guide

This page describes the current public-facing API direction for the Octaryn AAA modular port. It is documentation for the active source tree, not a promise that every planned system is complete.

## Shared Contracts

`octaryn-shared/` owns contracts that can be referenced by client, server, basegame, and future modules:

- `Octaryn.Shared.GameModules`: manifests, module registration, module instances, validation reports, and frame contexts.
- `Octaryn.Shared.ApiExposure`: capability IDs and host API IDs exposed to modules.
- `Octaryn.Shared.FrameworkAllowlist`: approved package and framework API groups.
- `Octaryn.Shared.Host`: scheduling declarations, resource access, host frame snapshots, and command sinks.
- `Octaryn.Shared.Networking`: client command frames, server snapshot headers, replication IDs, and replication changes.
- `Octaryn.Shared.World`: block IDs, block positions, chunk constants, chunk positions, and chunk snapshots.
- `Octaryn.Shared.Time`: world time value contracts.

Shared code must stay implementation-free. It should not know about client rendering, server persistence internals, transport implementation, product-specific gameplay rules, or native owner bridge internals.

## Module Registration

Modules expose `IGameModuleRegistration`:

```csharp
public interface IGameModuleRegistration
{
    GameModuleManifest Manifest { get; }

    IGameModuleInstance CreateInstance(ModuleHostContext context);
}
```

`GameModuleManifest` declares identity, compatibility, requested capabilities, host APIs, allowed packages, framework API groups, dependencies, content, assets, and scheduled systems.

Basegame currently registers as `octaryn.basegame` and requests capabilities for blocks, items, gameplay interactions, gameplay rules, and world block edits.

## Deny-By-Default Validation

`GameModuleValidator` validates manifests before activation. It checks:

- required identity and compatibility fields
- version format and host API version range
- duplicate capabilities, dependencies, content, assets, and scheduled systems
- module-owned declaration IDs
- allowed module capabilities
- allowed host APIs
- allowed runtime and build packages
- allowed framework API groups
- denied framework API groups
- schedule graph shape and resource conflicts
- required capability and host API relationships

The current policy is deny-by-default. Adding a dependency to `Directory.Packages.props` does not grant module permission by itself.

## Scheduling

Modules declare scheduled systems with exact read/write access:

```csharp
public sealed record ScheduledSystemDeclaration(
    string SystemId,
    HostWorkPhase Phase,
    string FrameOrTickOwner,
    IReadOnlyList<ScheduledResourceAccess> Reads,
    IReadOnlyList<ScheduledResourceAccess> Writes,
    IReadOnlyList<string> RunsAfter,
    IReadOnlyList<string> RunsBefore,
    HostWorkScheduleFlags Flags,
    string CommitBarrier);
```

The host owns threads and scheduling. Modules declare work; they do not create private worker pools, raw threads, timers, or custom schedulers.

## Commands And Snapshots

Client and server exchange explicit contract shapes:

- `ClientCommandFrame` describes a versioned command batch for a tick.
- `ServerSnapshotHeader` describes a versioned server snapshot header with replication and change counts.
- `ReplicationChange` carries compact change data.
- `ReplicationId` identifies replicated state.

Transport implementation belongs to client and server owners. Shared only defines message shapes and stable value contracts.

## Owner Rules

- Client presentation/rendering never belongs in server.
- Server authority/persistence/simulation never belongs in client.
- Gameplay/content belongs in basegame or another module.
- Shared contracts remain minimal and implementation-free.
- Module APIs must be explicit, capability-scoped, and validated before activation.
