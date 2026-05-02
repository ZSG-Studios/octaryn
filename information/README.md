# Octaryn Information

This folder is the quick human index for the active Octaryn workspace. The root [README](../README.md) is the main entry point; this directory points to the deeper architecture, API, and validation material.

## Start Here

- [Root README](../README.md): current repository overview and build commands.
- [API guide](../docs/api.md): current shared API boundary and module contract shape.
- [API examples](../docs/examples.md): manifest, validation, scheduling, and snapshot examples.
- [AAA port structure](../docs/architecture/aaa-port-structure.md): active modular port plan.

## Owner Boundaries

- `octaryn-client/` owns presentation and client host behavior.
- `octaryn-server/` owns authority, persistence, simulation, validation, and replication.
- `octaryn-shared/` owns implementation-free contracts and API policy.
- `octaryn-basegame/` owns bundled gameplay and content.
- `tools/` owns repo-wide developer operations.
- `cmake/` owns build policy and toolchains.

`old-architecture/` is source material for the port, not a destination owner.

## Validation Links

- [Build matrix](../docs/validation/build-matrix.md)
- [Runtime runs](../docs/validation/runtime-runs.md)
- [Tracy capture notes](../docs/validation/tracy.md)

Use targeted builds, owner launch probes, direct runtime checks, or structure validators. Do not treat smoke tests or `ctest` as the default validation path.
