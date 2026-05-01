# Build Matrix

Last updated during the AAA port loop.

## Canonical Commands

```sh
dotnet restore Octaryn.DotNet.sln
dotnet build Octaryn.DotNet.sln --no-restore -maxcpucount
cmake --preset debug-linux
cmake --preset release-linux
cmake --preset debug-windows
cmake --preset release-windows
cmake --preset debug-macos
cmake --preset release-macos
tools/build/cmake_build.sh debug-linux
tools/build/cmake_build.sh release-linux
tools/build/cmake_build.sh debug-windows
tools/build/cmake_build.sh release-windows
tools/build/cmake_build.sh debug-macos
tools/build/cmake_build.sh release-macos
tools/build/cmake_build.sh debug-linux --target octaryn_validate_all
tools/build/cmake_build.sh release-linux --target octaryn_validate_all
tools/build/cmake_build.sh debug-windows --target octaryn_validate_all
tools/build/cmake_build.sh release-windows --target octaryn_validate_all
tools/build/cmake_build.sh debug-macos --target octaryn_validate_all
tools/build/cmake_build.sh release-macos --target octaryn_validate_all
tools/build/cmake_build.sh debug-linux --target octaryn_debug_tools
```

## Expanded Debug Validators

```sh
tools/build/cmake_build.sh debug-linux --target octaryn_client_managed_bridge
tools/build/cmake_build.sh debug-linux --target octaryn_client_camera_matrix
tools/build/cmake_build.sh debug-linux --target octaryn_client_render_distance
tools/build/cmake_build.sh debug-linux --target octaryn_server_managed_bridge
tools/build/cmake_build.sh debug-linux --target octaryn_server_bundle
tools/build/cmake_build.sh debug-linux --target octaryn_run_client_launch_probe
tools/build/cmake_build.sh debug-linux --target octaryn_run_server_launch_probe
tools/build/cmake_build.sh debug-linux --target octaryn_validate_cmake_targets
tools/build/cmake_build.sh debug-linux --target octaryn_validate_cmake_policy_separation
tools/build/cmake_build.sh debug-linux --target octaryn_validate_package_policy_sync
tools/build/cmake_build.sh debug-linux --target octaryn_validate_project_references
tools/build/cmake_build.sh debug-linux --target octaryn_validate_module_manifest_packages
tools/build/cmake_build.sh debug-linux --target octaryn_validate_module_manifest_files
tools/build/cmake_build.sh debug-linux --target octaryn_validate_module_manifest_probe
tools/build/cmake_build.sh debug-linux --target octaryn_validate_bundle_module_payload
tools/build/cmake_build.sh debug-linux --target octaryn_validate_module_source_api
tools/build/cmake_build.sh debug-linux --target octaryn_validate_module_binary_sandbox
tools/build/cmake_build.sh debug-linux --target octaryn_validate_module_layout
tools/build/cmake_build.sh debug-linux --target octaryn_validate_dotnet_package_assets
tools/build/cmake_build.sh debug-linux --target octaryn_validate_native_abi_contracts
tools/build/cmake_build.sh debug-linux --target octaryn_validate_native_owner_boundaries
tools/build/cmake_build.sh debug-linux --target octaryn_validate_native_archive_format
tools/build/cmake_build.sh debug-linux --target octaryn_validate_dotnet_owners
tools/build/cmake_build.sh debug-linux --target octaryn_validate_scheduler_contract
tools/build/cmake_build.sh debug-linux --target octaryn_validate_scheduler_probe
tools/build/cmake_build.sh debug-linux --target octaryn_validate_owner_module_validation_probe
tools/build/cmake_build.sh debug-linux --target octaryn_validate_hostfxr_bridge_exports
tools/build/cmake_build.sh debug-linux --target octaryn_validate_owner_launch_probes
tools/build/cmake_build.sh debug-linux --target octaryn_validate_all
python3 tools/validation/validate_all_project_reference_boundaries.py --repo-root .
dotnet run --project tools/validation/Octaryn.ModuleApiProbe/Octaryn.ModuleApiProbe.csproj --configuration Debug -- octaryn-basegame
dotnet run --project tools/validation/Octaryn.ModuleBinarySandboxProbe/Octaryn.ModuleBinarySandboxProbe.csproj --configuration Debug -- --assembly build/debug-linux/basegame/managed/Octaryn.Basegame.dll --assets-file build/debug-linux/basegame/managed-obj/project.assets.json
dotnet run --project tools/validation/Octaryn.ModuleManifestProbe/Octaryn.ModuleManifestProbe.csproj --configuration Debug -- octaryn-basegame --dump-manifest build/debug-linux/basegame/generated/octaryn.basegame.manifest.json
python3 -m json.tool octaryn-basegame/Data/Module/octaryn.basegame.module.json >/dev/null
dotnet run --project tools/validation/Octaryn.OwnerModuleValidationProbe/Octaryn.OwnerModuleValidationProbe.csproj --configuration Debug
dotnet run --project tools/validation/Octaryn.SchedulerProbe/Octaryn.SchedulerProbe.csproj --configuration Debug
python3 tools/validation/validate_module_layout.py --module-root octaryn-basegame
python3 tools/validation/validate_dotnet_package_assets.py --assets-file build/debug-linux/client/managed-obj/project.assets.json --owner client
python3 tools/validation/validate_dotnet_package_assets.py --assets-file build/debug-linux/server/managed-obj/project.assets.json --owner server
python3 tools/validation/validate_dotnet_package_assets.py --assets-file build/debug-linux/basegame/managed-obj/project.assets.json --owner basegame
python3 tools/validation/validate_module_manifest_packages.py --module-root octaryn-basegame --project-file octaryn-basegame/Octaryn.Basegame.csproj --manifest-json build/debug-linux/basegame/generated/octaryn.basegame.manifest.json
python3 tools/validation/validate_module_manifest_files.py --module-root octaryn-basegame --manifest-json build/debug-linux/basegame/generated/octaryn.basegame.manifest.json
python3 tools/validation/validate_bundle_module_payload.py --bundle-root build/debug-linux/client/bundle --module-id octaryn.basegame
python3 tools/validation/validate_bundle_module_payload.py --bundle-root build/debug-linux/server/bundle --module-id octaryn.basegame
python3 tools/validation/validate_package_policy_sync.py
python3 tools/validation/validate_cmake_policy_separation.py --repo-root .
python3 tools/validation/validate_native_abi_contracts.py
python3 tools/validation/validate_native_owner_boundaries.py --repo-root .
python3 tools/validation/validate_scheduler_contract.py --repo-root .
python3 tools/validation/validate_cmake_target_inventory.py --build-dir build/debug-linux/cmake
python3 tools/validation/validate_hostfxr_bridge_exports.py --owner client --bundle-dir build/debug-linux/client/bundle --bridge build/debug-linux/client/native/lib/liboctaryn_client_managed_bridge.so
python3 tools/validation/validate_hostfxr_bridge_exports.py --owner server --bundle-dir build/debug-linux/server/bundle --bridge build/debug-linux/server/native/lib/liboctaryn_server_managed_bridge.so
python3 tools/validation/validate_owner_launch_probe_logs.py --owner client --log-file logs/client/octaryn_client_launch_probe-debug-linux.log
python3 tools/validation/validate_owner_launch_probe_logs.py --owner server --log-file logs/server/octaryn_server_launch_probe-debug-linux.log
```

## Notes

- Root CMake currently builds managed owner targets, the shared native ABI library, client/server native bridge facades, native owner aggregate targets, and client/server publish bundles. `octaryn_all` is build-only; `octaryn_validate_all` runs direct module policy validators, bundle payload validation, .NET owner validation, CMake target inventory validation, scheduler contract validation, hostfxr bridge validation, and owner launch probe validation.
- Debug builds stage first-class root tools through `octaryn_debug_tools`: the PySide workspace control app, Tracy wrapper, RenderDoc wrapper, shared tool environment, and workspace bootstrap/Podman entrypoint under `build/<preset>/tools/`.
- `Octaryn.DotNet.sln` includes shared/client/server/basegame plus the validation probe projects, so solution restore/build covers the managed validators used by CMake.
- Owner launch probe validation runs native client/server probe executables, calls valid bridge initialize/tick/command/snapshot/shutdown paths, and writes logs under `logs/client/` and `logs/server/`.
- Old architecture CMake is not part of the active new-architecture validation matrix. Run it only when a task explicitly touches the transitional native host.
- Scheduler structure validation and a compiled scheduler probe are active for the owner schedulers. Runtime/profiling acceptance must still prove frame/tick work routing, barrier behavior, and worker scaling under real gameplay load.
- Module sandbox validation has both source-level Roslyn checks and post-build binary metadata checks. The binary probe rejects generated or binary-only module references to denied framework surfaces such as filesystem, networking, process, reflection/dynamic loading, native interop, threading, console, and environment APIs.
- Basegame has checked-in package descriptor metadata at `octaryn-basegame/Data/Module/octaryn.basegame.module.json`. `Octaryn.ModuleManifestProbe` compares that descriptor with `BasegameModuleRegistration.Manifest` and writes the generated validation manifest under `build/<preset>/basegame/generated/octaryn.basegame.manifest.json`.
- Bundle payload validation reads the bundled descriptor from client/server bundles and verifies every manifest-declared content and asset file is present under the same bundle root.
- Native C/C++ bridge loader validation is active for current facades. The matrix builds client/server managed outputs and native bridge facades, then verifies hostfxr startup, runtimeconfig/deps discovery, exact managed method resolution, exported owner ABI names, and invalid-input managed return paths for client and server bridge entry points.
- Toolchain configure coverage is exactly Linux, Windows, and macOS, each with debug/release presets. Linux native builds are Clang-only in active lanes; GCC is not an active preset lane. All active presets are expected to run from Linux/Arch now and later through Podman wrappers that provide the same toolchain roots. Cross presets disable hostfxr bridge/probe targets when target-compatible .NET native hosting assets are not present under `OCTARYN_DOTNET_ROOT`; Linux host validation still builds and runs those bridge/probe targets.
- .NET native hosting discovery is target-RID constrained. Arch Linux uses `arch-x64`, Windows Clang declares `win-x64`, and macOS declares `osx-x64`; host pack lookup only searches `Microsoft.NETCore.App.Host.<rid>/.../runtimes/<rid>/native`, and hostfxr lookup uses the target platform extension.
- `octaryn_validate_cmake_targets` validates every configured preset graph listed in `CMakePresets.json` and rejects stale generated root buckets outside the documented owner layout.
- Windows Clang native archive validation proves target ABI explicitly: `octaryn_validate_native_archive_format` checks the shared host ABI archive with `x86_64-w64-mingw32-objdump` and requires PE/COFF `pe-x86-64` output.
- Direct MSBuild output is preset-partitioned by `OctarynBuildPresetName`, defaulting to `debug-linux` outside CMake. CMake sets it to the active preset so package assets, intermediate files, and managed binaries do not leak across debug/release/toolchain graphs.
