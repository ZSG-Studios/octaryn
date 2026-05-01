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

file(GLOB OCTARYN_DOTNET_HOST_PACK_NATIVE_DIRS
    LIST_DIRECTORIES true
    "${OCTARYN_DOTNET_ROOT}/packs/Microsoft.NETCore.App.Host.${OCTARYN_TARGET_DOTNET_RID}/[0-9]*/runtimes/${OCTARYN_TARGET_DOTNET_RID}/native")

if(OCTARYN_TARGET_PLATFORM STREQUAL "Windows")
    set(OCTARYN_DOTNET_HOSTFXR_LIBRARY_GLOB "${OCTARYN_DOTNET_ROOT}/host/fxr/*/hostfxr.dll")
elseif(OCTARYN_TARGET_PLATFORM STREQUAL "Darwin")
    set(OCTARYN_DOTNET_HOSTFXR_LIBRARY_GLOB "${OCTARYN_DOTNET_ROOT}/host/fxr/*/libhostfxr.dylib")
else()
    set(OCTARYN_DOTNET_HOSTFXR_LIBRARY_GLOB "${OCTARYN_DOTNET_ROOT}/host/fxr/*/libhostfxr.so")
endif()

find_path(OCTARYN_DOTNET_HOSTING_INCLUDE_DIR
    NAMES nethost.h hostfxr.h coreclr_delegates.h
    PATHS ${OCTARYN_DOTNET_HOST_PACK_NATIVE_DIRS}
    NO_DEFAULT_PATH)

find_library(OCTARYN_DOTNET_NETHOST_LIBRARY
    NAMES nethost libnethost
    PATHS ${OCTARYN_DOTNET_HOST_PACK_NATIVE_DIRS}
    NO_DEFAULT_PATH)

file(GLOB OCTARYN_DOTNET_HOSTFXR_LIBRARIES
    "${OCTARYN_DOTNET_HOSTFXR_LIBRARY_GLOB}")

if(OCTARYN_DOTNET_HOSTFXR_LIBRARIES)
    list(SORT OCTARYN_DOTNET_HOSTFXR_LIBRARIES COMPARE NATURAL ORDER DESCENDING)
    list(GET OCTARYN_DOTNET_HOSTFXR_LIBRARIES 0 OCTARYN_DOTNET_HOSTFXR_LIBRARY)
endif()

set(OCTARYN_DOTNET_HOSTING_AVAILABLE OFF)
if(OCTARYN_DOTNET_HOSTING_INCLUDE_DIR AND OCTARYN_DOTNET_NETHOST_LIBRARY AND OCTARYN_DOTNET_HOSTFXR_LIBRARY)
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

target_link_libraries(octaryn_dotnet_hosting
    INTERFACE
        "${OCTARYN_DOTNET_NETHOST_LIBRARY}")

if(UNIX AND NOT APPLE)
    target_link_libraries(octaryn_dotnet_hosting INTERFACE dl)
endif()

target_compile_definitions(octaryn_dotnet_hosting
    INTERFACE
        OCTARYN_DOTNET_HOSTFXR_PATH="${OCTARYN_DOTNET_HOSTFXR_LIBRARY}")
