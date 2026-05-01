#!/usr/bin/env python3
import argparse
import json
import pathlib
import re
import sys


REQUIRED_TARGETS = {
    "octaryn_shared",
    "octaryn_shared_native",
    "octaryn_shared_host_abi",
    "octaryn_native_logging",
    "octaryn_native_diagnostics",
    "octaryn_native_memory",
    "octaryn_native_profiling",
    "octaryn_native_jobs",
    "octaryn_basegame",
    "octaryn_basegame_native",
    "octaryn_server",
    "octaryn_server_bundle",
    "octaryn_server_native",
    "octaryn_server_managed_bridge",
    "octaryn_server_launch_probe",
    "octaryn_client_managed",
    "octaryn_client_native",
    "octaryn_client_asset_paths",
    "octaryn_client_app_settings",
    "octaryn_client_camera_matrix",
    "octaryn_client_display_catalog",
    "octaryn_client_display_menu",
    "octaryn_client_display_settings",
    "octaryn_client_fullscreen_display_mode",
    "octaryn_client_frame_metrics",
    "octaryn_client_hidden_block_uniforms",
    "octaryn_client_lighting_settings",
    "octaryn_client_renderdoc_capture",
    "octaryn_client_render_distance",
    "octaryn_client_shader_creation",
    "octaryn_client_shader_metadata_contract",
    "octaryn_client_visibility_flags",
    "octaryn_client_window_frame_statistics",
    "octaryn_client_managed_bridge",
    "octaryn_client_launch_probe",
    "octaryn_client_bundle",
    "octaryn_tools",
    "octaryn_all",
    "octaryn_validate_all",
    "octaryn_validate_cmake_targets",
    "octaryn_validate_cmake_policy_separation",
    "octaryn_validate_cmake_dependency_aliases",
    "octaryn_validate_package_policy_sync",
    "octaryn_validate_project_references",
    "octaryn_validate_module_manifest_packages",
    "octaryn_validate_module_manifest_files",
    "octaryn_validate_module_manifest_probe",
    "octaryn_validate_bundle_module_payload",
    "octaryn_validate_module_source_api",
    "octaryn_validate_module_binary_sandbox",
    "octaryn_validate_module_layout",
    "octaryn_validate_dotnet_package_assets",
    "octaryn_validate_native_abi_contracts",
    "octaryn_validate_native_owner_boundaries",
    "octaryn_validate_native_archive_format",
    "octaryn_validate_dotnet_owners",
    "octaryn_validate_scheduler_contract",
    "octaryn_validate_scheduler_probe",
    "octaryn_validate_owner_module_validation_probe",
    "octaryn_validate_hostfxr_bridge_exports",
    "octaryn_validate_owner_launch_probes",
    "octaryn_run_client_launch_probe",
    "octaryn_run_server_launch_probe",
}

FORBIDDEN_TARGET_PATTERNS = (
    "octaryn_engine",
    "engine_runtime",
)

REQUIRED_CMAKE_STRUCTURE = (
    "cmake/Shared/ProjectDefaults.cmake",
    "cmake/Shared/BuildOutputs.cmake",
    "cmake/Shared/OwnerBuildLayout.cmake",
    "cmake/Shared/CompilerWarnings.cmake",
    "cmake/Owners/SharedTargets.cmake",
    "cmake/Owners/BasegameTargets.cmake",
    "cmake/Owners/ServerTargets.cmake",
    "cmake/Owners/ClientTargets.cmake",
    "cmake/Owners/ToolTargets.cmake",
    "cmake/Owners/DotNetOwner.cmake",
    "cmake/Owners/NativeOwner.cmake",
    "cmake/Dependencies/BasegameDependencies.cmake",
    "cmake/Dependencies/ClientDependencies.cmake",
    "cmake/Dependencies/DependencyPolicy.cmake",
    "cmake/Dependencies/DotNetHosting.cmake",
    "cmake/Dependencies/NativeDependencyAliases.cmake",
    "cmake/Dependencies/ServerDependencies.cmake",
    "cmake/Dependencies/ToolDependencies.cmake",
    "cmake/Platforms/PlatformDispatch.cmake",
    "cmake/Platforms/Windows/WindowsPlatform.cmake",
    "cmake/Platforms/Windows/MinGWPlatform.cmake",
    "cmake/Platforms/Linux/LinuxPlatform.cmake",
    "cmake/Platforms/Linux/ArchFamily.cmake",
    "cmake/Platforms/Linux/DebianFamily.cmake",
    "cmake/Platforms/Linux/FedoraFamily.cmake",
    "cmake/Platforms/Linux/SuseFamily.cmake",
    "cmake/Platforms/BSD/BSDPlatform.cmake",
    "cmake/Platforms/BSD/FreeBSDPlatform.cmake",
    "cmake/Platforms/MacOS/MacOSPlatform.cmake",
    "cmake/Toolchains/Linux/clang.cmake",
    "cmake/Toolchains/Windows/MinGW/x86_64-w64-mingw32.cmake",
    "cmake/Toolchains/BSD/clang.cmake",
    "cmake/Toolchains/MacOS/apple-clang.cmake",
)

FORBIDDEN_CMAKE_PATHS = (
    "cmake/toolchains",
    "cmake/engine",
    "cmake/runtime",
)

REQUIRED_CONFIGURE_PRESETS = (
    "debug",
    "release",
    "debug-linux-clang",
    "debug-windows-mingw",
    "debug-macos-apple-clang",
    "release-linux-clang",
    "release-macos-apple-clang",
    "release-windows-mingw",
)

REQUIRED_CONFIGURED_GRAPH_PRESETS = (
    "debug",
    "release",
    "debug-linux-clang",
    "debug-windows-mingw",
    "release-linux-clang",
    "release-windows-mingw",
)

REQUIRED_BUILD_PRESETS = (
    "debug",
    "debug-validate",
    "release",
    "release-validate",
    "debug-linux-clang",
    "debug-linux-clang-validate",
    "debug-windows-mingw",
    "debug-windows-mingw-validate",
    "debug-macos-apple-clang",
    "debug-macos-apple-clang-validate",
    "release-linux-clang",
    "release-linux-clang-validate",
    "release-macos-apple-clang",
    "release-macos-apple-clang-validate",
    "release-windows-mingw",
    "release-windows-mingw-validate",
)

ALLOWED_BUILD_ROOTS = (
    "basegame",
    "client",
    "dependencies",
    "old-architecture",
    "server",
    "shared",
    "tools",
)

ALLOWED_LOG_ROOTS = (
    "basegame",
    "build",
    "client",
    "old-architecture",
    "server",
    "shared",
    "tools",
)

FORBIDDEN_OWNER_BUILD_SUBROOT_NAMES = (
    "_deps",
    "cpm-cache",
    "deps",
)

ALLOWED_OWNER_BUILD_SUBROOTS = (
    "local",
)

HOSTFXR_REAL_OUTPUTS = (
    "liboctaryn_client_managed_bridge.so",
    "liboctaryn_server_managed_bridge.so",
    "bin/octaryn_client_launch_probe",
    "bin/octaryn_server_launch_probe",
)

HOSTFXR_SKIP_MESSAGES = (
    "Skipping client managed bridge: .NET native hosting unavailable",
    "Skipping server managed bridge: .NET native hosting unavailable",
    "Skipping client launch probe binary: .NET native hosting unavailable",
    "Skipping server launch probe binary: .NET native hosting unavailable",
    "Skipping hostfxr bridge export validation: .NET native hosting unavailable",
    "Skipping owner launch probes: .NET native hosting unavailable",
)

REQUIRED_BUILD_COMMAND_SNIPPETS = (
    "validate_bundle_module_payload.py",
    "--expected-manifest",
    "validate_native_owner_boundaries.py",
    "validate_native_abi_contracts.py",
)


def load_targets(build_file):
    targets = set()
    target_pattern = re.compile(r"^build\s+([^:| ]+)")
    for line in build_file.read_text(encoding="utf-8").splitlines():
        match = target_pattern.match(line)
        if not match:
            continue
        target = match.group(1)
        if target.startswith("/") or target.startswith("CMakeFiles/"):
            continue
        targets.add(target)
    return targets


def validate_hostfxr_target_state(build_text):
    errors = []
    is_skipped = "Skipping hostfxr bridge export validation" in build_text

    if is_skipped:
        missing_messages = [message for message in HOSTFXR_SKIP_MESSAGES if message not in build_text]
        if missing_messages:
            errors.append(f"hostfxr skip state is missing skip commands: {missing_messages}")

        real_outputs = [output for output in HOSTFXR_REAL_OUTPUTS if output in build_text]
        if real_outputs:
            errors.append(f"hostfxr skip state still references real outputs: {real_outputs}")
        return errors

    missing_outputs = [output for output in HOSTFXR_REAL_OUTPUTS if output not in build_text]
    if missing_outputs:
        errors.append(f"hostfxr real state is missing bridge/probe outputs: {missing_outputs}")

    missing_real_commands = [
        target
        for target in (
            "validate_hostfxr_bridge_exports.py",
            "validate_owner_launch_probe_logs.py",
        )
        if target not in build_text
    ]
    if missing_real_commands:
        errors.append(f"hostfxr real state is missing validation commands: {missing_real_commands}")

    forbidden_skip_messages = [message for message in HOSTFXR_SKIP_MESSAGES if message in build_text]
    if forbidden_skip_messages:
        errors.append(f"hostfxr real state contains skip commands: {forbidden_skip_messages}")

    return errors


def validate_presets(repo_root):
    presets_path = repo_root / "CMakePresets.json"
    if not presets_path.exists():
        return [f"{presets_path}: missing CMake presets"]

    presets = json.loads(presets_path.read_text(encoding="utf-8"))
    configure = {preset["name"] for preset in presets.get("configurePresets", [])}
    build = {preset["name"] for preset in presets.get("buildPresets", [])}
    errors = []

    missing_configure = sorted(set(REQUIRED_CONFIGURE_PRESETS) - configure)
    if missing_configure:
        errors.append(f"missing required configure presets: {missing_configure}")

    missing_build = sorted(set(REQUIRED_BUILD_PRESETS) - build)
    if missing_build:
        errors.append(f"missing required build presets: {missing_build}")

    for preset in presets.get("configurePresets", []):
        expected_binary_dir = "${sourceDir}/build/tools/cmake/${presetName}"
        if preset.get("binaryDir") != expected_binary_dir:
            errors.append(
                f"configure preset {preset['name']} must use binaryDir {expected_binary_dir}, got {preset.get('binaryDir')}")

    for preset in presets.get("buildPresets", []):
        configure_preset = preset.get("configurePreset")
        if configure_preset and configure_preset not in configure:
            errors.append(f"build preset {preset['name']} references missing configure preset {configure_preset}")
        if preset["name"].endswith("-validate"):
            targets = preset.get("targets", [])
            if targets != ["octaryn_validate_all"]:
                errors.append(
                    f"validate build preset {preset['name']} must target only octaryn_validate_all, got {targets}")
        else:
            targets = preset.get("targets", [])
            if targets != ["octaryn_all"]:
                errors.append(
                    f"build preset {preset['name']} must target only octaryn_all, got {targets}")

    return errors


def configure_preset_build_dirs(repo_root):
    presets_path = repo_root / "CMakePresets.json"
    if not presets_path.exists():
        return []

    presets = json.loads(presets_path.read_text(encoding="utf-8"))
    build_dirs = []
    for preset in presets.get("configurePresets", []):
        name = preset["name"]
        binary_dir = preset.get("binaryDir")
        if not binary_dir:
            continue

        resolved = binary_dir.replace("${sourceDir}", str(repo_root)).replace("${presetName}", name)
        build_dirs.append((name, pathlib.Path(resolved)))

    return build_dirs


def validate_aggregate_dependencies(build_text):
    errors = []
    validate_all_lines = [
        line
        for line in build_text.splitlines()
        if line.startswith("build octaryn_validate_all:") or line.startswith("build CMakeFiles/octaryn_validate_all ")
    ]
    if not validate_all_lines:
        return ["missing octaryn_validate_all aggregate target lines"]

    aggregate_text = "\n".join(validate_all_lines)
    required_dependencies = sorted(
        target
        for target in REQUIRED_TARGETS
        if target.startswith("octaryn_validate_") and target != "octaryn_validate_all"
    )
    for dependency in required_dependencies:
        if dependency not in aggregate_text:
            errors.append(f"octaryn_validate_all missing dependency {dependency}")

    return errors


def validate_critical_command_snippets(build_text):
    errors = []
    for snippet in REQUIRED_BUILD_COMMAND_SNIPPETS:
        if snippet not in build_text:
            errors.append(f"configured build graph is missing critical command snippet {snippet}")
    return errors


def validate_generated_layout(repo_root):
    errors = []
    configured_preset_names = {
        preset_name
        for preset_name, _build_dir in configure_preset_build_dirs(repo_root)
    }
    for root_name, allowed in (("build", ALLOWED_BUILD_ROOTS), ("logs", ALLOWED_LOG_ROOTS)):
        root = repo_root / root_name
        if not root.exists():
            continue

        forbidden = [
            path.name
            for path in root.iterdir()
            if path.is_dir() and path.name not in allowed
        ]
        if forbidden:
            errors.append(f"forbidden generated {root_name}/ roots: {sorted(forbidden)}")

        root_files = sorted(path.name for path in root.iterdir() if path.is_file())
        if root_files:
            errors.append(f"forbidden generated {root_name}/ files: {root_files}")

        if root_name == "logs":
            nested_log_dirs = []
            for owner in ALLOWED_LOG_ROOTS:
                owner_root = root / owner
                if not owner_root.exists():
                    continue

                nested_log_dirs.extend(
                    path.relative_to(repo_root).as_posix()
                    for path in owner_root.rglob("*")
                    if path.is_dir())
            if nested_log_dirs:
                errors.append(f"forbidden nested generated log roots: {sorted(nested_log_dirs)}")

    build_root = repo_root / "build"
    if build_root.exists():
        forbidden_owner_subroots = []
        stale_owner_subroots = []
        for owner in ("basegame", "client", "server", "shared", "tools"):
            owner_root = build_root / owner
            if not owner_root.exists():
                continue

            allowed_subroots = set(ALLOWED_OWNER_BUILD_SUBROOTS)
            allowed_subroots.update(configured_preset_names)
            if owner == "tools":
                allowed_subroots.add("cmake")

            for path in owner_root.rglob("*"):
                if path.is_dir() and path.name in FORBIDDEN_OWNER_BUILD_SUBROOT_NAMES:
                    forbidden_owner_subroots.append(path.relative_to(repo_root).as_posix())

            for path in owner_root.iterdir():
                if path.is_dir() and path.name not in allowed_subroots:
                    stale_owner_subroots.append(path.relative_to(repo_root).as_posix())

        if forbidden_owner_subroots:
            errors.append(
                "dependency/cache build roots must live under build/dependencies: "
                f"{sorted(forbidden_owner_subroots)}")
        if stale_owner_subroots:
            errors.append(
                "owner build roots must be configured presets or approved generated roots: "
                f"{sorted(stale_owner_subroots)}")

    return errors


def validate_configured_preset_graphs(repo_root, current_build_dir):
    errors = []
    current = current_build_dir.resolve()
    required_presets = set(REQUIRED_CONFIGURED_GRAPH_PRESETS)
    for preset_name, build_dir in configure_preset_build_dirs(repo_root):
        build_file = build_dir / "build.ninja"
        if not build_file.exists():
            if preset_name in required_presets:
                errors.append(f"preset {preset_name}: {build_file}: missing configured Ninja graph")
            continue

        if build_dir.resolve() == current:
            continue

        preset_errors = validate_single_build_dir(build_dir, repo_root)
        errors.extend(f"preset {preset_name}: {error}" for error in preset_errors)

    return errors


def derive_repo_root(build_dir):
    resolved = build_dir.resolve()
    for parent in [resolved, *resolved.parents]:
        if (parent / "CMakeLists.txt").exists() and (parent / "cmake").exists():
            return parent
    return resolved


def validate_single_build_dir(build_dir, repo_root):
    build_file = build_dir / "build.ninja"
    if not build_file.exists():
        return [f"{build_file}: missing configured Ninja graph"]

    build_text = build_file.read_text(encoding="utf-8")
    targets = load_targets(build_file)
    errors = []

    missing = REQUIRED_TARGETS - targets
    if missing:
        errors.append(f"missing active CMake targets: {sorted(missing)}")

    forbidden = [
        target
        for target in sorted(targets)
        if any(pattern in target for pattern in FORBIDDEN_TARGET_PATTERNS)
    ]
    if forbidden:
        errors.append(f"forbidden active CMake targets: {forbidden}")

    missing_structure = [path for path in REQUIRED_CMAKE_STRUCTURE if not (repo_root / path).exists()]
    if missing_structure:
        errors.append(f"missing required CMake owner/platform/dependency files: {missing_structure}")

    forbidden_paths = [path for path in FORBIDDEN_CMAKE_PATHS if (repo_root / path).exists()]
    if forbidden_paths:
        errors.append(f"forbidden active CMake structure paths: {forbidden_paths}")

    errors.extend(validate_hostfxr_target_state(build_text))
    errors.extend(validate_presets(repo_root))
    errors.extend(validate_aggregate_dependencies(build_text))
    errors.extend(validate_critical_command_snippets(build_text))

    return errors


def validate(build_dir, repo_root):
    errors = validate_single_build_dir(build_dir, repo_root)
    errors.extend(validate_generated_layout(repo_root))
    errors.extend(validate_configured_preset_graphs(repo_root, build_dir))
    return errors


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--build-dir", required=True)
    parser.add_argument("--repo-root")
    args = parser.parse_args()

    build_dir = pathlib.Path(args.build_dir)
    repo_root = pathlib.Path(args.repo_root).resolve() if args.repo_root else derive_repo_root(build_dir)
    errors = validate(build_dir, repo_root)
    if errors:
        for error in errors:
            print(f"cmake target inventory: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
