# Build Matrix

Last updated during the AAA port loop.

## Canonical Commands

```sh
dotnet restore Octaryn.DotNet.sln
dotnet build Octaryn.DotNet.sln --no-restore
cmake --preset debug
cmake --preset debug-linux-gcc
cmake --preset debug-linux-clang
cmake --preset debug-windows-mingw
cmake --preset release
cmake --preset release-linux-gcc
cmake --preset release-linux-clang
cmake --preset release-windows-mingw
cmake --build --preset debug
cmake --build --preset debug-validate
cmake --build --preset debug-linux-gcc
cmake --build --preset debug-linux-gcc-validate
cmake --build --preset debug-linux-clang
cmake --build --preset debug-linux-clang-validate
cmake --build --preset debug-windows-mingw
cmake --build --preset debug-windows-mingw-validate
cmake --build --preset release
cmake --build --preset release-validate
cmake --build --preset release-linux-gcc
cmake --build --preset release-linux-gcc-validate
cmake --build --preset release-linux-clang
cmake --build --preset release-linux-clang-validate
cmake --build --preset release-windows-mingw
cmake --build --preset release-windows-mingw-validate
```

## Expanded Debug Validators

```sh
cmake --build --preset debug --target octaryn_client_managed_bridge
cmake --build --preset debug --target octaryn_server_managed_bridge
cmake --build --preset debug --target octaryn_server_bundle
cmake --build --preset debug --target octaryn_run_client_launch_probe
cmake --build --preset debug --target octaryn_run_server_launch_probe
cmake --build --preset debug --target octaryn_validate_cmake_targets
cmake --build --preset debug --target octaryn_validate_cmake_policy_separation
cmake --build --preset debug --target octaryn_validate_package_policy_sync
cmake --build --preset debug --target octaryn_validate_project_references
cmake --build --preset debug --target octaryn_validate_module_manifest_packages
cmake --build --preset debug --target octaryn_validate_module_manifest_files
cmake --build --preset debug --target octaryn_validate_module_manifest_probe
cmake --build --preset debug --target octaryn_validate_bundle_module_payload
cmake --build --preset debug --target octaryn_validate_module_source_api
cmake --build --preset debug --target octaryn_validate_module_binary_sandbox
cmake --build --preset debug --target octaryn_validate_module_layout
cmake --build --preset debug --target octaryn_validate_dotnet_package_assets
cmake --build --preset debug --target octaryn_validate_native_abi_contracts
cmake --build --preset debug --target octaryn_validate_native_owner_boundaries
cmake --build --preset debug --target octaryn_validate_native_archive_format
cmake --build --preset debug --target octaryn_validate_dotnet_owners
cmake --build --preset debug --target octaryn_validate_scheduler_contract
cmake --build --preset debug --target octaryn_validate_scheduler_probe
cmake --build --preset debug --target octaryn_validate_owner_module_validation_probe
cmake --build --preset debug --target octaryn_validate_hostfxr_bridge_exports
cmake --build --preset debug --target octaryn_validate_owner_launch_probes
cmake --build --preset debug --target octaryn_validate_all
python3 tools/validation/validate_all_project_reference_boundaries.py --repo-root .
dotnet run --project tools/validation/Octaryn.ModuleApiProbe/Octaryn.ModuleApiProbe.csproj --configuration Debug -- octaryn-basegame
dotnet run --project tools/validation/Octaryn.ModuleBinarySandboxProbe/Octaryn.ModuleBinarySandboxProbe.csproj --configuration Debug -- --assembly build/basegame/local/dotnet/bin/Octaryn.Basegame/Debug/net10.0/Octaryn.Basegame.dll --assets-file build/basegame/local/dotnet/obj/Octaryn.Basegame/project.assets.json
dotnet run --project tools/validation/Octaryn.ModuleManifestProbe/Octaryn.ModuleManifestProbe.csproj --configuration Debug -- octaryn-basegame --dump-manifest build/basegame/debug/generated/octaryn.basegame.manifest.json
python3 -m json.tool octaryn-basegame/Data/Module/octaryn.basegame.module.json >/dev/null
dotnet run --project tools/validation/Octaryn.OwnerModuleValidationProbe/Octaryn.OwnerModuleValidationProbe.csproj --configuration Debug
dotnet run --project tools/validation/Octaryn.SchedulerProbe/Octaryn.SchedulerProbe.csproj --configuration Debug
python3 tools/validation/validate_module_layout.py --module-root octaryn-basegame
python3 tools/validation/validate_dotnet_package_assets.py --assets-file build/client/local/dotnet/obj/Octaryn.Client/project.assets.json --owner client
python3 tools/validation/validate_dotnet_package_assets.py --assets-file build/server/local/dotnet/obj/Octaryn.Server/project.assets.json --owner server
python3 tools/validation/validate_dotnet_package_assets.py --assets-file build/basegame/local/dotnet/obj/Octaryn.Basegame/project.assets.json --owner basegame
python3 tools/validation/validate_module_manifest_packages.py --module-root octaryn-basegame --project-file octaryn-basegame/Octaryn.Basegame.csproj --manifest-json build/basegame/debug/generated/octaryn.basegame.manifest.json
python3 tools/validation/validate_module_manifest_files.py --module-root octaryn-basegame --manifest-json build/basegame/debug/generated/octaryn.basegame.manifest.json
python3 tools/validation/validate_bundle_module_payload.py --bundle-root build/client/debug/bundle --module-id octaryn.basegame
python3 tools/validation/validate_bundle_module_payload.py --bundle-root build/server/debug/bundle --module-id octaryn.basegame
python3 tools/validation/validate_package_policy_sync.py
python3 tools/validation/validate_cmake_policy_separation.py --repo-root .
python3 tools/validation/validate_native_abi_contracts.py
python3 tools/validation/validate_native_owner_boundaries.py --repo-root .
python3 tools/validation/validate_scheduler_contract.py --repo-root .
python3 tools/validation/validate_cmake_target_inventory.py --build-dir build/tools/cmake/debug
python3 tools/validation/validate_hostfxr_bridge_exports.py --owner client --bundle-dir build/client/debug/bundle --bridge build/client/debug/lib/liboctaryn_client_managed_bridge.so
python3 tools/validation/validate_hostfxr_bridge_exports.py --owner server --bundle-dir build/server/debug/bundle --bridge build/server/debug/lib/liboctaryn_server_managed_bridge.so
python3 tools/validation/validate_owner_launch_probe_logs.py --owner client --log-file logs/client/octaryn_client_launch_probe-debug.log
python3 tools/validation/validate_owner_launch_probe_logs.py --owner server --log-file logs/server/octaryn_server_launch_probe-debug.log
```

## Notes

- Root CMake currently builds managed owner targets, the shared native ABI library, client/server native bridge facades, native owner aggregate targets, and client/server publish bundles. `octaryn_all` is build-only; `octaryn_validate_all` runs direct module policy validators, bundle payload validation, .NET owner validation, CMake target inventory validation, scheduler contract validation, hostfxr bridge validation, and owner launch probe validation.
- `Octaryn.DotNet.sln` includes shared/client/server/basegame plus the validation probe projects, so solution restore/build covers the managed validators used by CMake.
- Owner launch probe validation runs native client/server probe executables, calls valid bridge initialize/tick/command/snapshot/shutdown paths, and writes logs under `logs/client/` and `logs/server/`.
- Old architecture CMake is not part of the active new-architecture validation matrix. Run it only when a task explicitly touches the transitional native host.
- Scheduler structure validation and a compiled scheduler probe are active for the owner schedulers. Runtime/profiling acceptance must still prove frame/tick work routing, barrier behavior, and worker scaling under real gameplay load.
- Module sandbox validation has both source-level Roslyn checks and post-build binary metadata checks. The binary probe rejects generated or binary-only module references to denied framework surfaces such as filesystem, networking, process, reflection/dynamic loading, native interop, threading, console, and environment APIs.
- Basegame has checked-in package descriptor metadata at `octaryn-basegame/Data/Module/octaryn.basegame.module.json`. `Octaryn.ModuleManifestProbe` compares that descriptor with `BasegameModuleRegistration.Manifest` and writes the generated validation manifest under `build/basegame/<preset>/generated/octaryn.basegame.manifest.json`.
- Bundle payload validation reads the bundled descriptor from client/server bundles and verifies every manifest-declared content and asset file is present under the same bundle root.
- Native C/C++ bridge loader validation is active for current facades. The matrix builds client/server managed outputs and native bridge facades, then verifies hostfxr startup, runtimeconfig/deps discovery, exact managed method resolution, exported owner ABI names, and invalid-input managed return paths for client and server bridge entry points.
- Toolchain configure coverage is explicit for default debug/release, Linux GCC debug/release, Linux Clang debug/release, and Windows MinGW debug/release. MinGW configure disables hostfxr bridge/probe targets when target-compatible .NET native hosting assets are not present under `OCTARYN_DOTNET_ROOT`; Linux host validation still builds and runs those bridge/probe targets.
- BSD and macOS platform files are scaffold markers only until targeted configure presets and platform checks are added. Validation requires those scaffold markers so file presence cannot be mistaken for implemented platform coverage.
- .NET native hosting discovery is target-RID constrained. Arch Linux uses `arch-x64`; MinGW declares `win-x64`; host pack lookup only searches `Microsoft.NETCore.App.Host.<rid>/.../runtimes/<rid>/native`, and hostfxr lookup uses the target platform extension.
- `octaryn_validate_cmake_targets` validates every configured preset graph listed in `CMakePresets.json` and rejects stale generated root buckets outside the documented owner layout.
- MinGW native archive validation proves target ABI explicitly: `octaryn_validate_native_archive_format` checks the shared host ABI archive with `x86_64-w64-mingw32-objdump` and requires PE/COFF `pe-x86-64` output.
- Direct MSBuild output is preset-partitioned by `OctarynBuildPresetName`, defaulting to `local` outside CMake. CMake sets it to the active preset so package assets, intermediate files, and managed binaries do not leak across debug/release/toolchain graphs.
