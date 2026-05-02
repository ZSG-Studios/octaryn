include_guard(GLOBAL)

set(OCTARYN_DOTNET_ROOT "$ENV{DOTNET_ROOT}" CACHE PATH "Root of the .NET installation used for native hosting")
if(NOT OCTARYN_DOTNET_ROOT)
    set(OCTARYN_DOTNET_ROOT "/usr/share/dotnet" CACHE PATH "Root of the .NET installation used for native hosting" FORCE)
endif()

if(NOT OCTARYN_TARGET_DOTNET_RID)
    message(STATUS
        "Octaryn .NET target RID is not declared for ${OCTARYN_TARGET_PLATFORM}; "
        "hostfxr bridge targets are disabled for this configure.")
    set(OCTARYN_DOTNET_HOSTING_AVAILABLE OFF)
    return()
endif()

set(octaryn_dotnet_host_pack_name "Microsoft.NETCore.App.Host.${OCTARYN_TARGET_DOTNET_RID}")
string(TOLOWER "${octaryn_dotnet_host_pack_name}" octaryn_dotnet_host_pack_cache_name)

file(GLOB OCTARYN_DOTNET_HOST_PACK_NATIVE_DIRS
    LIST_DIRECTORIES true
    "${OCTARYN_DOTNET_ROOT}/packs/${octaryn_dotnet_host_pack_name}/[0-9]*/runtimes/${OCTARYN_TARGET_DOTNET_RID}/native"
    "${OCTARYN_NUGET_PACKAGES_DIR}/${octaryn_dotnet_host_pack_cache_name}/[0-9]*/runtimes/${OCTARYN_TARGET_DOTNET_RID}/native")

unset(OCTARYN_DOTNET_HOSTING_INCLUDE_DIR CACHE)
unset(OCTARYN_DOTNET_NETHOST_LIBRARY CACHE)

if(NOT OCTARYN_DOTNET_HOST_PACK_NATIVE_DIRS)
    execute_process(
        COMMAND "${DOTNET_EXECUTABLE}" restore
            "${OCTARYN_WORKSPACE_ROOT_DIR}/Octaryn.DotNet.sln"
            --runtime "${OCTARYN_TARGET_DOTNET_RID}"
            --packages "${OCTARYN_NUGET_PACKAGES_DIR}"
        WORKING_DIRECTORY "${OCTARYN_WORKSPACE_ROOT_DIR}"
        RESULT_VARIABLE octaryn_dotnet_host_pack_restore_result
        OUTPUT_QUIET
        ERROR_VARIABLE octaryn_dotnet_host_pack_restore_error)
    if(NOT octaryn_dotnet_host_pack_restore_result EQUAL 0)
        message(STATUS
            "Octaryn .NET target host pack restore failed for ${OCTARYN_TARGET_DOTNET_RID}: "
            "${octaryn_dotnet_host_pack_restore_error}")
    endif()
endif()

file(GLOB OCTARYN_DOTNET_HOST_PACK_NATIVE_DIRS
    LIST_DIRECTORIES true
    "${OCTARYN_DOTNET_ROOT}/packs/${octaryn_dotnet_host_pack_name}/[0-9]*/runtimes/${OCTARYN_TARGET_DOTNET_RID}/native"
    "${OCTARYN_NUGET_PACKAGES_DIR}/${octaryn_dotnet_host_pack_cache_name}/[0-9]*/runtimes/${OCTARYN_TARGET_DOTNET_RID}/native")

if(OCTARYN_TARGET_PLATFORM STREQUAL "Windows")
    set(OCTARYN_DOTNET_HOSTFXR_LIBRARY "hostfxr.dll")
else()
    set(OCTARYN_DOTNET_HOSTFXR_LIBRARY_GLOB "${OCTARYN_DOTNET_ROOT}/host/fxr/*/libhostfxr.so")
endif()

find_path(OCTARYN_DOTNET_HOSTING_INCLUDE_DIR
    NAMES nethost.h hostfxr.h coreclr_delegates.h
    PATHS ${OCTARYN_DOTNET_HOST_PACK_NATIVE_DIRS}
    NO_DEFAULT_PATH
    NO_CMAKE_FIND_ROOT_PATH)

find_library(OCTARYN_DOTNET_NETHOST_LIBRARY
    NAMES nethost libnethost
    PATHS ${OCTARYN_DOTNET_HOST_PACK_NATIVE_DIRS}
    NO_DEFAULT_PATH
    NO_CMAKE_FIND_ROOT_PATH)

if(OCTARYN_DOTNET_HOSTFXR_LIBRARY_GLOB)
    file(GLOB OCTARYN_DOTNET_HOSTFXR_LIBRARIES
        "${OCTARYN_DOTNET_HOSTFXR_LIBRARY_GLOB}")
    list(SORT OCTARYN_DOTNET_HOSTFXR_LIBRARIES COMPARE NATURAL ORDER DESCENDING)
    list(GET OCTARYN_DOTNET_HOSTFXR_LIBRARIES 0 OCTARYN_DOTNET_HOSTFXR_LIBRARY)
endif()

set(OCTARYN_DOTNET_HOSTING_AVAILABLE OFF)
if(OCTARYN_DOTNET_HOSTING_INCLUDE_DIR AND OCTARYN_DOTNET_HOSTFXR_LIBRARY)
    if(OCTARYN_TARGET_PLATFORM STREQUAL "Windows" OR OCTARYN_DOTNET_NETHOST_LIBRARY)
        set(OCTARYN_DOTNET_HOSTING_AVAILABLE ON)
    endif()
endif()

if(OCTARYN_DOTNET_HOSTING_AVAILABLE)
    set(OCTARYN_DOTNET_HOSTING_AVAILABLE ON)
endif()

if(NOT OCTARYN_DOTNET_HOSTING_AVAILABLE)
    message(STATUS
        "Octaryn .NET native hosting assets unavailable for ${OCTARYN_TARGET_PLATFORM}/${OCTARYN_TARGET_DOTNET_RID} under ${OCTARYN_DOTNET_ROOT}; "
        "hostfxr bridge targets are disabled for this configure.")
    return()
endif()

add_library(octaryn_dotnet_hosting INTERFACE)
add_library(octaryn::dotnet_hosting ALIAS octaryn_dotnet_hosting)

target_include_directories(octaryn_dotnet_hosting
    INTERFACE
        "${OCTARYN_DOTNET_HOSTING_INCLUDE_DIR}")

if(NOT OCTARYN_TARGET_PLATFORM STREQUAL "Windows")
    target_link_libraries(octaryn_dotnet_hosting
        INTERFACE
            "${OCTARYN_DOTNET_NETHOST_LIBRARY}")
endif()

if(OCTARYN_TARGET_PLATFORM STREQUAL "Linux")
    target_link_libraries(octaryn_dotnet_hosting INTERFACE dl)
endif()

target_compile_definitions(octaryn_dotnet_hosting
    INTERFACE
        OCTARYN_DOTNET_HOSTFXR_PATH="${OCTARYN_DOTNET_HOSTFXR_LIBRARY}")
