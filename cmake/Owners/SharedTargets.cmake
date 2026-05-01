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

add_dependencies(octaryn_shared_native octaryn_shared_host_abi)

octaryn_add_dotnet_owner(
    octaryn_shared
    shared
    "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-shared/Octaryn.Shared.csproj")

add_dependencies(octaryn_shared octaryn_shared_native)
