# Runtime Runs

Runtime validation should use direct executable launches and focused logs, not smoke-test wrappers.

## Current New-Architecture Runtime Artifact

The current root build produces managed client and server bundles:

```sh
cmake --build --preset debug --target octaryn_client_bundle
cmake --build --preset debug --target octaryn_server_bundle
```

The bundles are staged under `build/client/debug/bundle/` and `build/server/debug/bundle/` with owner assemblies, `Octaryn.Basegame.dll`, `Octaryn.Shared.dll`, runtimeconfig/deps files, and approved runtime dependencies.

## Transitional Native Runtime

The old native runtime remains under `old-architecture/` as source material and transitional host validation only. Do not run old-architecture targets as part of normal new-architecture validation unless a task explicitly touches that bridge.

## Acceptance Signals

- Client bundle contains `Octaryn.Client.runtimeconfig.json`.
- Native bridge validation resolves all required client/server managed exports through hostfxr before the first owner frame or tick.
- Bridge facades no longer return not-loaded status after successful initialization; invalid inputs must reach the managed validation paths.
- Direct owner launch probes run with `cmake --build --preset debug --target octaryn_validate_owner_launch_probes`.
- Individual owner probe helpers run with `cmake --build --preset debug --target octaryn_run_client_launch_probe` and `cmake --build --preset debug --target octaryn_run_server_launch_probe`.
- Client launch probe logs `crash_marker=/tmp/octaryn-crash-...` before `tick_before_initialize=-1`, `initialize=0`, `tick=0`, `reinitialize=0`, `tick_after_reinitialize=0`, and `shutdown=0` under `logs/client/octaryn_client_launch_probe-debug.log`.
- Server launch probe logs `crash_marker=/tmp/octaryn-crash-...` before `tick_before_initialize=-1`, `initialize=0`, `tick=0`, `reinitialize=0`, `tick_after_reinitialize=0`, `submit_client_commands=0`, `drain_server_snapshots=0`, and `shutdown=0` under `logs/server/octaryn_server_launch_probe-debug.log`.
- Failed hostfxr load, missing runtimeconfig/deps, missing export, ABI version mismatch, and managed initialization failure should be logged under `logs/client/` or `logs/server/` when real owner-native runtime launchers replace the probes.
- Root CMake bundle rebuilds are dirty-correct.
- Direct runtime launch checks should continue to record logs under owner-specific log paths as probes graduate into real client/server runtime targets.
