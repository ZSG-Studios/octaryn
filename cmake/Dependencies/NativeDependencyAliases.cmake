include_guard(GLOBAL)

include(Dependencies/DotNetHosting)

find_package(Threads REQUIRED)

add_library(octaryn_native_threads INTERFACE)
add_library(octaryn::native_threads ALIAS octaryn_native_threads)
target_link_libraries(octaryn_native_threads INTERFACE Threads::Threads)

add_library(octaryn_native_spdlog INTERFACE)
add_library(octaryn::deps::spdlog ALIAS octaryn_native_spdlog)

find_package(spdlog CONFIG QUIET)
if(TARGET spdlog::spdlog_header_only)
    target_link_libraries(octaryn_native_spdlog INTERFACE spdlog::spdlog_header_only)
elseif(TARGET spdlog::spdlog)
    target_link_libraries(octaryn_native_spdlog INTERFACE spdlog::spdlog)
else()
    message(STATUS "Target-compatible spdlog package unavailable; octaryn_native_logging will use stdio fallback for this configure.")
endif()

add_library(octaryn_native_cpptrace INTERFACE)
add_library(octaryn::deps::cpptrace ALIAS octaryn_native_cpptrace)

find_package(cpptrace CONFIG QUIET)
if(TARGET cpptrace::cpptrace)
    target_link_libraries(octaryn_native_cpptrace INTERFACE cpptrace::cpptrace)
endif()
