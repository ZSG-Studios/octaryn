include_guard(GLOBAL)

set(OCTARYN_HOST_PLATFORM "${CMAKE_HOST_SYSTEM_NAME}")
set(OCTARYN_TARGET_PLATFORM "${CMAKE_SYSTEM_NAME}")

if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    include(Platforms/Linux/LinuxPlatform)
elseif(CMAKE_SYSTEM_NAME STREQUAL "Windows")
    include(Platforms/Windows/WindowsPlatform)
elseif(CMAKE_SYSTEM_NAME STREQUAL "Darwin")
    include(Platforms/MacOS/MacOSPlatform)
else()
    message(WARNING "No Octaryn platform module for target platform ${CMAKE_SYSTEM_NAME}.")
endif()
