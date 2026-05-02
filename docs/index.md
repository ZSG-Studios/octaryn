# Octaryn API Documentation

Octaryn is moving to a strict owner-split architecture:

- `octaryn-client/` presents the world.
- `octaryn-server/` owns authority and persistence.
- `octaryn-shared/` defines contracts.
- `octaryn-basegame/` implements bundled gameplay and content.

The API boundary is intentionally narrow. Game modules and mods use explicit shared contracts, capability IDs, host API IDs, content declarations, scheduled systems, commands, and snapshots. They do not reach into client rendering, server persistence, native bridges, transport internals, or broad host services.

## Guides

- [API guide](api.md)
- [Examples](examples.md)
- [AAA port structure](architecture/aaa-port-structure.md)
- [Old architecture map](migration/old-architecture-map.md)
- [Build matrix](validation/build-matrix.md)

## Current API Focus

The current shared API surface covers:

- module registration through `IGameModuleRegistration`
- module execution through `IGameModuleInstance`
- deny-by-default manifest validation through `GameModuleValidator`
- capability IDs in `ModuleCapabilityIds`
- host API IDs in `HostApiIds`
- scheduled work declarations through `ScheduledSystemDeclaration`
- command and snapshot contracts under `Octaryn.Shared.Networking`
- world value types under `Octaryn.Shared.World`

Basegame is the first bundled module that uses this path.
