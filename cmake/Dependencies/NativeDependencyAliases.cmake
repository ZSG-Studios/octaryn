include_guard(GLOBAL)

include(Dependencies/DotNetHosting)

find_package(Threads REQUIRED)

add_library(octaryn_native_threads INTERFACE)
add_library(octaryn::native_threads ALIAS octaryn_native_threads)
target_link_libraries(octaryn_native_threads INTERFACE Threads::Threads)
