include_guard(GLOBAL)

include(Owners/DotNetOwner)
include(Dependencies/ToolDependencies)

add_custom_target(octaryn_tools)

octaryn_owner_build_root(tool_client_build_root client)
octaryn_owner_build_root(tool_server_build_root server)
octaryn_owner_build_root(tool_basegame_build_root basegame)
octaryn_owner_build_root(octaryn_debug_tool_root tools)
octaryn_owner_log_root(tool_client_log_root client)
octaryn_owner_log_root(tool_server_log_root server)
octaryn_owner_log_root(octaryn_debug_tool_log_root tools)
set(octaryn_tool_client_bundle_dir "${tool_client_build_root}/bundle")
set(octaryn_tool_server_bundle_dir "${tool_server_build_root}/bundle")
set(octaryn_tool_client_bundle_output "${octaryn_tool_client_bundle_dir}/Octaryn.Client.dll")
set(octaryn_tool_client_bundle_runtime_config "${octaryn_tool_client_bundle_dir}/Octaryn.Client.runtimeconfig.json")
set(octaryn_tool_client_bundle_deps "${octaryn_tool_client_bundle_dir}/Octaryn.Client.deps.json")
set(octaryn_tool_server_bundle_output "${octaryn_tool_server_bundle_dir}/Octaryn.Server.dll")
set(octaryn_tool_server_bundle_runtime_config "${octaryn_tool_server_bundle_dir}/Octaryn.Server.runtimeconfig.json")
set(octaryn_tool_server_bundle_deps "${octaryn_tool_server_bundle_dir}/Octaryn.Server.deps.json")
set(octaryn_tool_client_probe_log "${tool_client_log_root}/octaryn_client_launch_probe-${OCTARYN_BUILD_PRESET_NAME}.log")
set(octaryn_tool_server_probe_log "${tool_server_log_root}/octaryn_server_launch_probe-${OCTARYN_BUILD_PRESET_NAME}.log")
set(octaryn_tool_basegame_project "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-basegame/Octaryn.Basegame.csproj")
set(octaryn_tool_client_assets "${tool_client_build_root}/managed-obj/project.assets.json")
set(octaryn_tool_server_assets "${tool_server_build_root}/managed-obj/project.assets.json")
set(octaryn_tool_basegame_assets "${tool_basegame_build_root}/managed-obj/project.assets.json")
set(octaryn_tool_basegame_manifest_json "${tool_basegame_build_root}/generated/octaryn.basegame.manifest.json")

set(octaryn_debug_tool_files
    "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/run_workspace_ui.sh"
    "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/run_workspace_ui.bat"
    "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/build/podman_build.sh"
    "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/build/podman_build.bat"
    "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/setup/linux_build_environment.sh"
    "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/setup/windows_build_environment.bat"
    "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/podman/Containerfile.arch-build"
    "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/podman/arch_packages.txt"
    "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/tooling/tool_environment.sh"
    "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/profiling/tracy_tool.sh"
    "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/bootstrap/workspace_bootstrap.sh"
    "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/ui/workspace_control_app.py")

if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    set(octaryn_debug_tool_build_commands
        COMMAND "${CMAKE_COMMAND}" -E make_directory
            "${octaryn_debug_tool_root}/build"
            "${octaryn_debug_tool_root}/tooling"
            "${octaryn_debug_tool_root}/setup"
            "${octaryn_debug_tool_root}/podman"
            "${octaryn_debug_tool_root}/profiling"
            "${octaryn_debug_tool_root}/bootstrap"
            "${octaryn_debug_tool_root}/ui"
            "${octaryn_debug_tool_log_root}"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/run_workspace_ui.sh"
            "${octaryn_debug_tool_root}/run_workspace_ui.sh"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/run_workspace_ui.bat"
            "${octaryn_debug_tool_root}/run_workspace_ui.bat"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/build/podman_build.sh"
            "${octaryn_debug_tool_root}/build/podman_build.sh"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/build/podman_build.bat"
            "${octaryn_debug_tool_root}/build/podman_build.bat"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/setup/linux_build_environment.sh"
            "${octaryn_debug_tool_root}/setup/linux_build_environment.sh"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/setup/windows_build_environment.bat"
            "${octaryn_debug_tool_root}/setup/windows_build_environment.bat"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/podman/Containerfile.arch-build"
            "${octaryn_debug_tool_root}/podman/Containerfile.arch-build"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/podman/arch_packages.txt"
            "${octaryn_debug_tool_root}/podman/arch_packages.txt"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/tooling/tool_environment.sh"
            "${octaryn_debug_tool_root}/tooling/tool_environment.sh"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/profiling/tracy_tool.sh"
            "${octaryn_debug_tool_root}/profiling/tracy_tool.sh"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/bootstrap/workspace_bootstrap.sh"
            "${octaryn_debug_tool_root}/bootstrap/workspace_bootstrap.sh"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/ui/workspace_control_app.py"
            "${octaryn_debug_tool_root}/ui/workspace_control_app.py"
        COMMAND chmod 755
            "${octaryn_debug_tool_root}/run_workspace_ui.sh"
            "${octaryn_debug_tool_root}/build/podman_build.sh"
            "${octaryn_debug_tool_root}/setup/linux_build_environment.sh"
            "${octaryn_debug_tool_root}/tooling/tool_environment.sh"
            "${octaryn_debug_tool_root}/profiling/tracy_tool.sh"
            "${octaryn_debug_tool_root}/bootstrap/workspace_bootstrap.sh"
            "${octaryn_debug_tool_root}/ui/workspace_control_app.py"
        COMMAND "${CMAKE_COMMAND}" -E env "OCTARYN_WORKSPACE_ROOT=${OCTARYN_WORKSPACE_ROOT_DIR}"
            "${octaryn_debug_tool_root}/profiling/tracy_tool.sh"
            --preset "${OCTARYN_BUILD_PRESET_NAME}"
            build
    )

    add_custom_target(octaryn_debug_tools
        ${octaryn_debug_tool_build_commands}
        DEPENDS
            ${octaryn_debug_tool_files}
        WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
        VERBATIM)
else()
    add_custom_target(octaryn_debug_tools
        COMMAND "${CMAKE_COMMAND}" -E echo "Skipping debug tool staging for ${OCTARYN_BUILD_PRESET_NAME}: debug tools ship with Debug presets."
        VERBATIM)
endif()

add_custom_target(octaryn_validate_cmake_targets
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_cmake_target_inventory.py"
        --build-dir "${CMAKE_BINARY_DIR}"
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_cmake_policy_separation
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_cmake_policy_separation.py"
        --repo-root "${OCTARYN_WORKSPACE_ROOT_DIR}"
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_cmake_dependency_aliases
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_cmake_dependency_aliases.py"
        --repo-root "${OCTARYN_WORKSPACE_ROOT_DIR}"
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_package_policy_sync
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_package_policy_sync.py"
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_project_references
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_all_project_reference_boundaries.py"
        --repo-root "${OCTARYN_WORKSPACE_ROOT_DIR}"
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_module_manifest_packages
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_module_manifest_packages.py"
        --module-root "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-basegame"
        --project-file "${octaryn_tool_basegame_project}"
        --allowed-package-ids-file "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-shared/Source/FrameworkAllowlist/AllowedPackageIds.cs"
        --manifest-json "${octaryn_tool_basegame_manifest_json}"
    DEPENDS
        octaryn_validate_module_manifest_probe
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_module_manifest_files
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_module_manifest_files.py"
        --module-root "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-basegame"
        --manifest-json "${octaryn_tool_basegame_manifest_json}"
    DEPENDS
        octaryn_validate_module_manifest_probe
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_module_manifest_probe
    COMMAND "${CMAKE_COMMAND}" -E env "NUGET_PACKAGES=${OCTARYN_NUGET_PACKAGES_DIR}" "OctarynBuildPresetName=${OCTARYN_BUILD_PRESET_NAME}"
        "${DOTNET_EXECUTABLE}" restore
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/Octaryn.ModuleManifestProbe/Octaryn.ModuleManifestProbe.csproj"
    COMMAND "${CMAKE_COMMAND}" -E env "NUGET_PACKAGES=${OCTARYN_NUGET_PACKAGES_DIR}" "OctarynBuildPresetName=${OCTARYN_BUILD_PRESET_NAME}"
        "${DOTNET_EXECUTABLE}" run
        --project "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/Octaryn.ModuleManifestProbe/Octaryn.ModuleManifestProbe.csproj"
        --configuration "${CMAKE_BUILD_TYPE}"
        --no-restore
        -- "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-basegame"
        --dump-manifest "${octaryn_tool_basegame_manifest_json}"
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_module_source_api
    COMMAND "${CMAKE_COMMAND}" -E env "NUGET_PACKAGES=${OCTARYN_NUGET_PACKAGES_DIR}" "OctarynBuildPresetName=${OCTARYN_BUILD_PRESET_NAME}"
        "${DOTNET_EXECUTABLE}" restore
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/Octaryn.ModuleApiProbe/Octaryn.ModuleApiProbe.csproj"
    COMMAND "${CMAKE_COMMAND}" -E env "NUGET_PACKAGES=${OCTARYN_NUGET_PACKAGES_DIR}" "OctarynBuildPresetName=${OCTARYN_BUILD_PRESET_NAME}"
        "${DOTNET_EXECUTABLE}" run
        --project "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/Octaryn.ModuleApiProbe/Octaryn.ModuleApiProbe.csproj"
        --configuration "${CMAKE_BUILD_TYPE}"
        --no-restore
        -- --source-root "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-basegame"
        --assets-file "${octaryn_tool_basegame_assets}"
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_module_binary_sandbox
    COMMAND "${CMAKE_COMMAND}" -E env "NUGET_PACKAGES=${OCTARYN_NUGET_PACKAGES_DIR}" "OctarynBuildPresetName=${OCTARYN_BUILD_PRESET_NAME}"
        "${DOTNET_EXECUTABLE}" restore
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/Octaryn.ModuleBinarySandboxProbe/Octaryn.ModuleBinarySandboxProbe.csproj"
    COMMAND "${CMAKE_COMMAND}" -E env "NUGET_PACKAGES=${OCTARYN_NUGET_PACKAGES_DIR}" "OctarynBuildPresetName=${OCTARYN_BUILD_PRESET_NAME}"
        "${DOTNET_EXECUTABLE}" run
        --project "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/Octaryn.ModuleBinarySandboxProbe/Octaryn.ModuleBinarySandboxProbe.csproj"
        --configuration "${CMAKE_BUILD_TYPE}"
        --no-restore
        -- --assembly "${tool_basegame_build_root}/managed/Octaryn.Basegame.dll"
        --assets-file "${octaryn_tool_basegame_assets}"
    DEPENDS
        octaryn_basegame
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_module_layout
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_module_layout.py"
        --module-root "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-basegame"
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_dotnet_package_assets
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_dotnet_package_assets.py"
        --assets-file "${octaryn_tool_client_assets}"
        --owner client
        --policy-file "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/package-policy/module-packages.json"
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_dotnet_package_assets.py"
        --assets-file "${octaryn_tool_server_assets}"
        --owner server
        --policy-file "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/package-policy/module-packages.json"
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_dotnet_package_assets.py"
        --assets-file "${octaryn_tool_basegame_assets}"
        --owner basegame
        --policy-file "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/package-policy/module-packages.json"
    DEPENDS
        octaryn_client_managed
        octaryn_server
        octaryn_basegame
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_bundle_module_payload
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_bundle_module_payload.py"
        --bundle-root "${octaryn_tool_client_bundle_dir}"
        --module-id "octaryn.basegame"
        --expected-manifest "${octaryn_tool_basegame_manifest_json}"
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_bundle_module_payload.py"
        --bundle-root "${octaryn_tool_server_bundle_dir}"
        --module-id "octaryn.basegame"
        --expected-manifest "${octaryn_tool_basegame_manifest_json}"
    DEPENDS
        "${octaryn_tool_client_bundle_output}"
        "${octaryn_tool_server_bundle_output}"
        octaryn_client_bundle
        octaryn_server_bundle
        octaryn_validate_module_manifest_probe
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_native_abi_contracts
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_native_abi_contracts.py"
        --repo-root "${OCTARYN_WORKSPACE_ROOT_DIR}"
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_native_owner_boundaries
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_native_owner_boundaries.py"
        --repo-root "${OCTARYN_WORKSPACE_ROOT_DIR}"
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

if(OCTARYN_TARGET_NATIVE_ARCHIVE_FORMAT)
    add_custom_target(octaryn_validate_native_archive_format
        COMMAND python3
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_native_archive_format.py"
            --archive "${OCTARYN_BUILD_PRESET_ROOT}/shared/native/lib/liboctaryn_shared_host_abi.a"
            --expected-format "${OCTARYN_TARGET_NATIVE_ARCHIVE_FORMAT}"
            --objdump "${OCTARYN_TARGET_OBJDUMP}"
        DEPENDS
            octaryn_shared_host_abi
        WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
        VERBATIM)
else()
    add_custom_target(octaryn_validate_native_archive_format
        COMMAND "${CMAKE_COMMAND}" -E echo "Skipping native archive format validation: no target archive format declared for ${OCTARYN_TARGET_PLATFORM}."
        VERBATIM)
endif()

add_custom_target(octaryn_validate_dotnet_owners
    COMMAND "${CMAKE_COMMAND}" -E env "NUGET_PACKAGES=${OCTARYN_NUGET_PACKAGES_DIR}" "OctarynBuildPresetName=${OCTARYN_BUILD_PRESET_NAME}"
        "${DOTNET_EXECUTABLE}" restore
        "${OCTARYN_WORKSPACE_ROOT_DIR}/Octaryn.DotNet.sln"
    COMMAND "${CMAKE_COMMAND}" -E env "NUGET_PACKAGES=${OCTARYN_NUGET_PACKAGES_DIR}" "OctarynBuildPresetName=${OCTARYN_BUILD_PRESET_NAME}"
        "${DOTNET_EXECUTABLE}" build
        "${OCTARYN_WORKSPACE_ROOT_DIR}/Octaryn.DotNet.sln"
        --configuration "${CMAKE_BUILD_TYPE}"
        --no-restore
    DEPENDS
        octaryn_shared
        octaryn_basegame
        octaryn_client_managed
        octaryn_server
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_scheduler_contract
    COMMAND python3
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_scheduler_contract.py"
        --repo-root "${OCTARYN_WORKSPACE_ROOT_DIR}"
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_scheduler_probe
    COMMAND "${CMAKE_COMMAND}" -E env "NUGET_PACKAGES=${OCTARYN_NUGET_PACKAGES_DIR}" "OctarynBuildPresetName=${OCTARYN_BUILD_PRESET_NAME}"
        "${DOTNET_EXECUTABLE}" restore
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/Octaryn.SchedulerProbe/Octaryn.SchedulerProbe.csproj"
    COMMAND "${CMAKE_COMMAND}" -E env "NUGET_PACKAGES=${OCTARYN_NUGET_PACKAGES_DIR}" "OctarynBuildPresetName=${OCTARYN_BUILD_PRESET_NAME}"
        "${DOTNET_EXECUTABLE}" run
        --project "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/Octaryn.SchedulerProbe/Octaryn.SchedulerProbe.csproj"
        --configuration "${CMAKE_BUILD_TYPE}"
        --no-restore
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_custom_target(octaryn_validate_owner_module_validation_probe
    COMMAND "${CMAKE_COMMAND}" -E env "NUGET_PACKAGES=${OCTARYN_NUGET_PACKAGES_DIR}" "OctarynBuildPresetName=${OCTARYN_BUILD_PRESET_NAME}"
        "${DOTNET_EXECUTABLE}" restore
        "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/Octaryn.OwnerModuleValidationProbe/Octaryn.OwnerModuleValidationProbe.csproj"
    COMMAND "${CMAKE_COMMAND}" -E env "NUGET_PACKAGES=${OCTARYN_NUGET_PACKAGES_DIR}" "OctarynBuildPresetName=${OCTARYN_BUILD_PRESET_NAME}"
        "${DOTNET_EXECUTABLE}" run
        --project "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/Octaryn.OwnerModuleValidationProbe/Octaryn.OwnerModuleValidationProbe.csproj"
        --configuration "${CMAKE_BUILD_TYPE}"
        --no-restore
    WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
    VERBATIM)

add_dependencies(octaryn_validate_module_manifest_probe octaryn_validate_dotnet_owners)
add_dependencies(octaryn_validate_module_source_api octaryn_validate_dotnet_owners)
add_dependencies(octaryn_validate_module_binary_sandbox octaryn_validate_dotnet_owners)
add_dependencies(octaryn_validate_scheduler_probe octaryn_validate_dotnet_owners)
add_dependencies(octaryn_validate_owner_module_validation_probe octaryn_validate_dotnet_owners)

if(OCTARYN_DOTNET_HOSTING_AVAILABLE)
    add_custom_target(octaryn_validate_hostfxr_bridge_exports
        COMMAND python3
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_hostfxr_bridge_exports.py"
            --owner client
            --bundle-dir "${octaryn_tool_client_bundle_dir}"
            --bridge "$<TARGET_FILE:octaryn_client_managed_bridge>"
        COMMAND python3
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_hostfxr_bridge_exports.py"
            --owner server
            --bundle-dir "${octaryn_tool_server_bundle_dir}"
            --bridge "$<TARGET_FILE:octaryn_server_managed_bridge>"
        DEPENDS
            "${octaryn_tool_client_bundle_output}"
            "${octaryn_tool_client_bundle_runtime_config}"
            "${octaryn_tool_client_bundle_deps}"
            "${octaryn_tool_server_bundle_output}"
            "${octaryn_tool_server_bundle_runtime_config}"
            "${octaryn_tool_server_bundle_deps}"
            octaryn_client_bundle
            octaryn_server_bundle
            octaryn_client_managed_bridge
            octaryn_server_managed_bridge
        WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
        VERBATIM)

    add_custom_target(octaryn_validate_owner_launch_probes
        COMMAND "$<TARGET_FILE:octaryn_client_launch_probe>"
        COMMAND "$<TARGET_FILE:octaryn_server_launch_probe>"
        COMMAND python3
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_owner_launch_probe_logs.py"
            --owner client
            --log-file "${octaryn_tool_client_probe_log}"
        COMMAND python3
            "${OCTARYN_WORKSPACE_ROOT_DIR}/tools/validation/validate_owner_launch_probe_logs.py"
            --owner server
            --log-file "${octaryn_tool_server_probe_log}"
        DEPENDS
            "${octaryn_tool_client_bundle_output}"
            "${octaryn_tool_client_bundle_runtime_config}"
            "${octaryn_tool_server_bundle_output}"
            "${octaryn_tool_server_bundle_runtime_config}"
            octaryn_client_bundle
            octaryn_server_bundle
            octaryn_client_launch_probe
            octaryn_server_launch_probe
            octaryn_validate_hostfxr_bridge_exports
        WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
        VERBATIM)
else()
    add_custom_target(octaryn_validate_hostfxr_bridge_exports
        COMMAND "${CMAKE_COMMAND}" -E echo "Skipping hostfxr bridge export validation: .NET native hosting unavailable for ${OCTARYN_TARGET_PLATFORM}."
        VERBATIM)

    add_custom_target(octaryn_validate_owner_launch_probes
        COMMAND "${CMAKE_COMMAND}" -E echo "Skipping owner launch probes: .NET native hosting unavailable for ${OCTARYN_TARGET_PLATFORM}."
        VERBATIM)
endif()

add_dependencies(octaryn_tools
    octaryn_debug_tools
    octaryn_validate_cmake_targets
    octaryn_validate_cmake_policy_separation
    octaryn_validate_cmake_dependency_aliases
    octaryn_validate_package_policy_sync
    octaryn_validate_project_references
    octaryn_validate_module_manifest_packages
    octaryn_validate_module_manifest_files
    octaryn_validate_module_manifest_probe
    octaryn_validate_module_source_api
    octaryn_validate_module_binary_sandbox
    octaryn_validate_module_layout
    octaryn_validate_dotnet_package_assets
    octaryn_validate_bundle_module_payload
    octaryn_validate_native_abi_contracts
    octaryn_validate_native_owner_boundaries
    octaryn_validate_native_archive_format
    octaryn_validate_dotnet_owners
    octaryn_validate_scheduler_contract
    octaryn_validate_scheduler_probe
    octaryn_validate_owner_module_validation_probe
    octaryn_validate_hostfxr_bridge_exports
    octaryn_validate_owner_launch_probes)
