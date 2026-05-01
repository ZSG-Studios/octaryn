include_guard(GLOBAL)

set(OCTARYN_CLIENT_DEPENDENCIES_SCAFFOLD ON)

add_library(octaryn_client_sdl3 INTERFACE)
add_library(octaryn::deps::sdl3 ALIAS octaryn_client_sdl3)

find_package(SDL3 CONFIG QUIET)
if(TARGET SDL3::SDL3)
    target_link_libraries(octaryn_client_sdl3 INTERFACE SDL3::SDL3)
endif()
