include_guard(GLOBAL)

include(Owners/DotNetOwner)
include(Owners/NativeOwner)
include(Dependencies/ClientDependencies)

octaryn_owner_build_root(client_build_root client)
octaryn_owner_log_root(client_log_root client)
set(octaryn_client_bundle_dir "${client_build_root}/bundle")
set(octaryn_client_bundle_stamp "${client_build_root}/stamps/octaryn_client_bundle.stamp")
set(octaryn_client_bundle_output "${octaryn_client_bundle_dir}/Octaryn.Client.dll")

octaryn_add_native_owner(octaryn_client_native)
add_dependencies(octaryn_client_native octaryn_shared_native)

octaryn_add_native_static_library(
    octaryn_client_asset_paths
    client
    SOURCES
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/AssetPaths/octaryn_client_asset_path.cpp"
    PUBLIC_INCLUDE_DIRS
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/AssetPaths"
    PRIVATE_LINKS
        octaryn::deps::sdl3)

if(TARGET SDL3::SDL3)
    target_compile_definitions(octaryn_client_asset_paths
        PRIVATE
            OCTARYN_CLIENT_ASSET_PATHS_USE_SDL3)
endif()

add_dependencies(octaryn_client_native octaryn_client_asset_paths)

octaryn_add_native_static_library(
    octaryn_client_render_distance
    client
    SOURCES
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Settings/RenderDistance/octaryn_client_render_distance.cpp"
    PUBLIC_INCLUDE_DIRS
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Settings/RenderDistance")

add_dependencies(octaryn_client_native octaryn_client_render_distance)

octaryn_add_native_static_library(
    octaryn_client_app_settings
    client
    SOURCES
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Settings/AppSettings/octaryn_client_app_settings.cpp"
    PUBLIC_INCLUDE_DIRS
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Settings/AppSettings"
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Settings/RenderDistance"
    PRIVATE_LINKS
        octaryn_client_render_distance)

add_dependencies(octaryn_client_native octaryn_client_app_settings)

octaryn_add_native_static_library(
    octaryn_client_lighting_settings
    client
    SOURCES
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Settings/LightingSettings/octaryn_client_lighting_settings.cpp"
    PUBLIC_INCLUDE_DIRS
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Settings/LightingSettings")

add_dependencies(octaryn_client_native octaryn_client_lighting_settings)

octaryn_add_native_static_library(
    octaryn_client_display_settings
    client
    SOURCES
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Settings/DisplaySettings/octaryn_client_display_settings.cpp"
    PUBLIC_INCLUDE_DIRS
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Settings/DisplaySettings"
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Settings/AppSettings"
    PRIVATE_LINKS
        octaryn_client_app_settings
        octaryn::deps::sdl3)

add_dependencies(octaryn_client_native octaryn_client_display_settings)

if(TARGET SDL3::SDL3)
    target_compile_definitions(octaryn_client_display_settings
        PUBLIC
            OCTARYN_CLIENT_DISPLAY_SETTINGS_USE_SDL3)
endif()

octaryn_add_native_static_library(
    octaryn_client_display_menu
    client
    SOURCES
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Ui/DisplayMenu/octaryn_client_display_menu.cpp"
    PUBLIC_INCLUDE_DIRS
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Ui/DisplayMenu")

add_dependencies(octaryn_client_native octaryn_client_display_menu)

octaryn_add_native_static_library(
    octaryn_client_camera_matrix
    client
    SOURCES
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Rendering/Camera/octaryn_client_camera_matrix.cpp"
    PUBLIC_INCLUDE_DIRS
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Rendering/Camera")

add_dependencies(octaryn_client_native octaryn_client_camera_matrix)

octaryn_add_native_static_library(
    octaryn_client_visibility_flags
    client
    SOURCES
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Rendering/Visibility/octaryn_client_visibility_flags.cpp"
    PUBLIC_INCLUDE_DIRS
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/Rendering/Visibility")

add_dependencies(octaryn_client_native octaryn_client_visibility_flags)

if(OCTARYN_DOTNET_HOSTING_AVAILABLE)
    octaryn_add_native_shared_library(
        octaryn_client_managed_bridge
        client
        SOURCES
            "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/ManagedBridge/octaryn_client_managed_bridge.c"
        PUBLIC_INCLUDE_DIRS
            "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/ClientHostAbi"
            "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-shared/Source/Native/HostAbi"
            "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-shared/Source/Diagnostics/NativeCrashDiagnostics"
        PRIVATE_LINKS
            octaryn_shared_host_abi
            octaryn_native_diagnostics
            octaryn::dotnet_hosting)

    target_compile_definitions(octaryn_client_managed_bridge
        PRIVATE
            OCTARYN_CLIENT_MANAGED_ASSEMBLY_PATH="${octaryn_client_bundle_dir}/Octaryn.Client.dll"
            OCTARYN_CLIENT_RUNTIME_CONFIG_PATH="${octaryn_client_bundle_dir}/Octaryn.Client.runtimeconfig.json")

    add_dependencies(octaryn_client_native octaryn_client_managed_bridge)

    octaryn_add_native_executable(
        octaryn_client_launch_probe
        client
        SOURCES
            "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/LaunchProbe/octaryn_client_launch_probe.c"
        PUBLIC_INCLUDE_DIRS
            "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Source/Native/ClientHostAbi"
            "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-shared/Source/Native/HostAbi"
            "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-shared/Source/Diagnostics/NativeCrashDiagnostics"
        PRIVATE_LINKS
            octaryn_client_managed_bridge
            octaryn_native_diagnostics)

    target_compile_definitions(octaryn_client_launch_probe
        PRIVATE
            OCTARYN_CLIENT_LAUNCH_PROBE_LOG_PATH="${client_log_root}/octaryn_client_launch_probe-${OCTARYN_BUILD_PRESET_NAME}.log")
else()
    add_custom_target(octaryn_client_managed_bridge
        COMMAND "${CMAKE_COMMAND}" -E echo "Skipping client managed bridge: .NET native hosting unavailable for ${OCTARYN_TARGET_PLATFORM}."
        VERBATIM)
    add_custom_target(octaryn_client_launch_probe
        COMMAND "${CMAKE_COMMAND}" -E echo "Skipping client launch probe binary: .NET native hosting unavailable for ${OCTARYN_TARGET_PLATFORM}."
        VERBATIM)
endif()

octaryn_add_dotnet_owner(
    octaryn_client_managed
    client
    "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Octaryn.Client.csproj")

add_dependencies(octaryn_client_managed octaryn_shared octaryn_basegame)

add_custom_command(
    OUTPUT "${octaryn_client_managed_STAMP}"
    APPEND
    DEPENDS
        "${octaryn_shared_STAMP}"
        "${octaryn_basegame_STAMP}")

file(MAKE_DIRECTORY "${client_build_root}/stamps" "${client_log_root}")

add_custom_command(
    OUTPUT "${octaryn_client_bundle_stamp}"
    BYPRODUCTS
        "${octaryn_client_bundle_output}"
        "${octaryn_client_bundle_dir}/Octaryn.Client.deps.json"
        "${octaryn_client_bundle_dir}/Octaryn.Client.runtimeconfig.json"
        "${octaryn_client_bundle_dir}/Octaryn.Basegame.dll"
        "${octaryn_client_bundle_dir}/Data/Module/octaryn.basegame.module.json"
        "${octaryn_client_bundle_dir}/Data/Blocks/octaryn.basegame.block.air.json"
        "${octaryn_client_bundle_dir}/Data/Items/octaryn.basegame.item.hand.json"
        "${octaryn_client_bundle_dir}/Data/Rules/octaryn.basegame.rule.default_interaction.json"
        "${octaryn_client_bundle_dir}/Assets/Textures/octaryn.basegame.texture.placeholder.txt"
        "${octaryn_client_bundle_dir}/Octaryn.Shared.dll"
        "${octaryn_client_bundle_dir}/Octaryn.Client.pdb"
        "${octaryn_client_bundle_dir}/Octaryn.Basegame.pdb"
        "${octaryn_client_bundle_dir}/Octaryn.Shared.pdb"
        "${octaryn_client_bundle_dir}/Arch.dll"
        "${octaryn_client_bundle_dir}/Arch.EventBus.dll"
        "${octaryn_client_bundle_dir}/Arch.LowLevel.dll"
        "${octaryn_client_bundle_dir}/Arch.Relationships.dll"
        "${octaryn_client_bundle_dir}/Arch.System.dll"
        "${octaryn_client_bundle_dir}/Collections.Pooled.dll"
        "${octaryn_client_bundle_dir}/CommunityToolkit.HighPerformance.dll"
        "${octaryn_client_bundle_dir}/Microsoft.Extensions.ObjectPool.dll"
        "${octaryn_client_bundle_dir}/Schedulers.dll"
    COMMAND "${CMAKE_COMMAND}" -E rm -rf "${octaryn_client_bundle_dir}"
    COMMAND "${CMAKE_COMMAND}" -E make_directory "${octaryn_client_bundle_dir}"
    COMMAND "${CMAKE_COMMAND}" -E env "OctarynBuildPresetName=${OCTARYN_BUILD_PRESET_NAME}"
        "${DOTNET_EXECUTABLE}" publish "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-client/Octaryn.Client.csproj"
        --configuration "${CMAKE_BUILD_TYPE}"
        --framework net10.0
        --output "${octaryn_client_bundle_dir}"
        --no-self-contained
        --no-build
        "-bl:${client_log_root}/octaryn_client_bundle-${OCTARYN_BUILD_PRESET_NAME}.binlog"
    COMMAND "${CMAKE_COMMAND}" -E touch "${octaryn_client_bundle_stamp}"
    DEPENDS
        "${octaryn_client_managed_STAMP}"
        "${octaryn_shared_STAMP}"
        "${octaryn_basegame_STAMP}"
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_client_bundle
    DEPENDS "${octaryn_client_bundle_stamp}")

add_dependencies(octaryn_client_bundle octaryn_client_native)

if(OCTARYN_DOTNET_HOSTING_AVAILABLE)
    add_custom_target(octaryn_run_client_launch_probe
        COMMAND "$<TARGET_FILE:octaryn_client_launch_probe>"
        DEPENDS
            octaryn_client_bundle
            octaryn_client_launch_probe
        WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
        VERBATIM)
else()
    add_custom_target(octaryn_run_client_launch_probe
        COMMAND "${CMAKE_COMMAND}" -E echo "Skipping client launch probe: .NET native hosting unavailable for ${OCTARYN_TARGET_PLATFORM}."
        VERBATIM)
endif()
