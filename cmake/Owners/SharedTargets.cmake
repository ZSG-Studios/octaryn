include_guard(GLOBAL)

include(Owners/DotNetOwner)
include(Owners/NativeOwner)

octaryn_add_native_owner(octaryn_shared_native)

octaryn_add_native_static_library(
    octaryn_shared_host_abi
    shared
    SOURCES
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-shared/Source/Native/HostAbi/octaryn_host_abi.c"
    PUBLIC_INCLUDE_DIRS
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-shared/Source/Native/HostAbi")

target_compile_definitions(octaryn_shared_host_abi
    PRIVATE
        OCTARYN_ABI_BUILD)

octaryn_add_native_static_library(
    octaryn_native_logging
    shared
    SOURCES
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-shared/Source/Diagnostics/NativeLogging/octaryn_native_log.cpp"
    PUBLIC_INCLUDE_DIRS
        "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-shared/Source/Diagnostics/NativeLogging"
    PRIVATE_LINKS
        octaryn::deps::spdlog)

if(TARGET spdlog::spdlog_header_only OR TARGET spdlog::spdlog)
    target_compile_definitions(octaryn_native_logging
        PRIVATE
            OCTARYN_NATIVE_LOGGING_USE_SPDLOG)
endif()

add_dependencies(octaryn_shared_native
    octaryn_shared_host_abi
    octaryn_native_logging)

octaryn_add_dotnet_owner(
    octaryn_shared
    shared
    "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-shared/Octaryn.Shared.csproj")

add_dependencies(octaryn_shared octaryn_shared_native)
