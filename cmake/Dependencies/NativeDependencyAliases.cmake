include_guard(GLOBAL)

include(Dependencies/DotNetHosting)
include(Dependencies/SourceDependencyCache)

find_package(Threads REQUIRED)

octaryn_add_dependency_wrapper(octaryn_native_threads octaryn::native_threads)
target_link_libraries(octaryn_native_threads INTERFACE Threads::Threads)

set(OCTARYN_NATIVE_SPDLOG_AVAILABLE OFF)
set(OCTARYN_NATIVE_CPPTRACE_AVAILABLE OFF)
set(OCTARYN_NATIVE_MIMALLOC_AVAILABLE OFF)
set(OCTARYN_NATIVE_TRACY_AVAILABLE OFF)

octaryn_add_dependency_wrapper(octaryn_native_spdlog octaryn::deps::spdlog)

find_package(spdlog CONFIG QUIET)
if(TARGET spdlog::spdlog_header_only)
    target_link_libraries(octaryn_native_spdlog INTERFACE spdlog::spdlog_header_only)
    set(OCTARYN_NATIVE_SPDLOG_AVAILABLE ON)
elseif(TARGET spdlog::spdlog)
    target_link_libraries(octaryn_native_spdlog INTERFACE spdlog::spdlog)
    set(OCTARYN_NATIVE_SPDLOG_AVAILABLE ON)
else()
    octaryn_fetch_source_dependency(
        spdlog
        GITHUB_REPOSITORY gabime/spdlog
        GIT_TAG v1.17.0
        OPTIONS
            "SPDLOG_BUILD_SHARED OFF"
            "SPDLOG_BUILD_TESTS OFF"
            "SPDLOG_BUILD_EXAMPLE OFF"
            "SPDLOG_BUILD_BENCH OFF"
            "SPDLOG_FMT_EXTERNAL OFF"
            "SPDLOG_USE_STD_FORMAT OFF")
    if(TARGET spdlog::spdlog_header_only)
        target_link_libraries(octaryn_native_spdlog INTERFACE spdlog::spdlog_header_only)
        set(OCTARYN_NATIVE_SPDLOG_AVAILABLE ON)
    elseif(TARGET spdlog::spdlog)
        target_link_libraries(octaryn_native_spdlog INTERFACE spdlog::spdlog)
        set(OCTARYN_NATIVE_SPDLOG_AVAILABLE ON)
    else()
        message(STATUS "Target-compatible spdlog package unavailable; octaryn_native_logging will use stdio fallback for this configure.")
    endif()
endif()

octaryn_add_dependency_wrapper(octaryn_native_cpptrace octaryn::deps::cpptrace)

find_package(cpptrace CONFIG QUIET)
if(TARGET cpptrace::cpptrace)
    target_link_libraries(octaryn_native_cpptrace INTERFACE cpptrace::cpptrace)
    set(OCTARYN_NATIVE_CPPTRACE_AVAILABLE ON)
else()
    octaryn_fetch_source_dependency(
        cpptrace
        GITHUB_REPOSITORY jeremy-rifkin/cpptrace
        GIT_TAG v1.0.4
        OPTIONS
            "CPPTRACE_BUILD_TESTING OFF"
            "CPPTRACE_GET_SYMBOLS_WITH_LIBDWARF OFF"
            "CPPTRACE_GET_SYMBOLS_WITH_ADDR2LINE ON"
            "CPPTRACE_ADDR2LINE_SEARCH_SYSTEM_PATH ON"
            "BUILD_SHARED_LIBS OFF")
    if(TARGET cpptrace::cpptrace)
        target_link_libraries(octaryn_native_cpptrace INTERFACE cpptrace::cpptrace)
        set(OCTARYN_NATIVE_CPPTRACE_AVAILABLE ON)
    endif()
endif()

octaryn_add_dependency_wrapper(octaryn_native_mimalloc octaryn::deps::mimalloc)

find_package(mimalloc CONFIG QUIET)
if(TARGET mimalloc-static)
    target_link_libraries(octaryn_native_mimalloc INTERFACE mimalloc-static)
    set(OCTARYN_NATIVE_MIMALLOC_AVAILABLE ON)
elseif(TARGET mimalloc)
    target_link_libraries(octaryn_native_mimalloc INTERFACE mimalloc)
    set(OCTARYN_NATIVE_MIMALLOC_AVAILABLE ON)
else()
    octaryn_fetch_source_dependency(
        mimalloc
        GITHUB_REPOSITORY microsoft/mimalloc
        GIT_TAG v3.3.1
        OPTIONS
            "MI_BUILD_TESTS OFF"
            "MI_BUILD_SHARED OFF")
    if(TARGET mimalloc-static)
        target_link_libraries(octaryn_native_mimalloc INTERFACE mimalloc-static)
        set(OCTARYN_NATIVE_MIMALLOC_AVAILABLE ON)
    elseif(TARGET mimalloc)
        target_link_libraries(octaryn_native_mimalloc INTERFACE mimalloc)
        set(OCTARYN_NATIVE_MIMALLOC_AVAILABLE ON)
    endif()
endif()

octaryn_add_dependency_wrapper(octaryn_native_tracy octaryn::deps::tracy)

find_package(Tracy CONFIG QUIET)
if(TARGET Tracy::TracyClient)
    target_link_libraries(octaryn_native_tracy INTERFACE Tracy::TracyClient)
    set(OCTARYN_NATIVE_TRACY_AVAILABLE ON)
elseif(TARGET TracyClient)
    target_link_libraries(octaryn_native_tracy INTERFACE TracyClient)
    set(OCTARYN_NATIVE_TRACY_AVAILABLE ON)
else()
    octaryn_fetch_source_dependency(
        tracy
        GITHUB_REPOSITORY wolfpld/tracy
        GIT_TAG v0.13.1
        OPTIONS
            "TRACY_ENABLE ON"
            "TRACY_ON_DEMAND ON"
            "TRACY_NO_CALLSTACK ON"
            "TRACY_NO_SAMPLING ON"
            "TRACY_NO_SYSTEM_TRACING ON"
            "TRACY_NO_FRAME_IMAGE ON")
    if(TARGET Tracy::TracyClient)
        target_link_libraries(octaryn_native_tracy INTERFACE Tracy::TracyClient)
        set(OCTARYN_NATIVE_TRACY_AVAILABLE ON)
    elseif(TARGET TracyClient)
        target_link_libraries(octaryn_native_tracy INTERFACE TracyClient)
        set(OCTARYN_NATIVE_TRACY_AVAILABLE ON)
    endif()
endif()

octaryn_add_dependency_wrapper(octaryn_native_taskflow octaryn::deps::taskflow)

find_package(Taskflow CONFIG QUIET)
if(TARGET Taskflow::Taskflow)
    target_link_libraries(octaryn_native_taskflow INTERFACE Taskflow::Taskflow)
elseif(TARGET Taskflow)
    target_link_libraries(octaryn_native_taskflow INTERFACE Taskflow)
else()
    octaryn_fetch_header_dependency(
        Taskflow
        taskflow_source_dir
        GITHUB_REPOSITORY taskflow/taskflow
        GIT_TAG v4.0.0)
    if(taskflow_source_dir)
        octaryn_add_header_only_dependency(octaryn_native_taskflow "${taskflow_source_dir}")
    endif()
endif()

if(NOT TARGET octaryn::deps::eigen)
    octaryn_add_dependency_wrapper(octaryn_native_eigen octaryn::deps::eigen)
    find_package(Eigen3 CONFIG QUIET)
    octaryn_link_first_available_dependency(octaryn_native_eigen eigen_available Eigen3::Eigen)
    if(NOT eigen_available)
        octaryn_fetch_source_dependency(
            Eigen3
            GIT_REPOSITORY https://gitlab.com/libeigen/eigen.git
            GIT_TAG 5.0.0
            OPTIONS
                "BUILD_TESTING OFF"
                "EIGEN_BUILD_DOC OFF"
                "EIGEN_BUILD_PKGCONFIG OFF")
        octaryn_link_first_available_dependency(octaryn_native_eigen eigen_available Eigen3::Eigen)
    endif()
endif()

if(NOT TARGET octaryn::deps::unordered_dense)
    octaryn_add_dependency_wrapper(octaryn_native_unordered_dense octaryn::deps::unordered_dense)
    find_package(unordered_dense CONFIG QUIET)
    octaryn_link_first_available_dependency(octaryn_native_unordered_dense unordered_dense_available ankerl::unordered_dense)
    if(NOT unordered_dense_available)
        octaryn_fetch_header_dependency(
            unordered_dense
            unordered_dense_source_dir
            GITHUB_REPOSITORY martinus/unordered_dense
            GIT_TAG v4.8.1)
        if(EXISTS "${unordered_dense_source_dir}/include")
            octaryn_add_header_only_dependency(octaryn_native_unordered_dense "${unordered_dense_source_dir}/include")
        endif()
    endif()
endif()

if(NOT TARGET octaryn::deps::zlib)
    octaryn_add_dependency_wrapper(octaryn_native_zlib octaryn::deps::zlib)
    find_package(ZLIB QUIET)
    octaryn_link_first_available_dependency(octaryn_native_zlib zlib_available ZLIB::ZLIB)
    if(NOT zlib_available)
        octaryn_fetch_source_dependency(
            zlib
            GITHUB_REPOSITORY madler/zlib
            GIT_TAG v1.3.2
            OPTIONS
                "ZLIB_BUILD_EXAMPLES OFF")
        octaryn_link_first_available_dependency(octaryn_native_zlib zlib_available ZLIB::ZLIB zlibstatic zlib)
    endif()
endif()

if(NOT TARGET octaryn::deps::lz4)
    octaryn_add_dependency_wrapper(octaryn_native_lz4 octaryn::deps::lz4)
    find_package(lz4 CONFIG QUIET)
    octaryn_link_first_available_dependency(octaryn_native_lz4 lz4_available lz4::lz4 lz4_static)
    if(NOT lz4_available)
        octaryn_fetch_source_dependency(
            lz4
            GITHUB_REPOSITORY lz4/lz4
            GIT_TAG v1.10.0
            SOURCE_SUBDIR build/cmake)
        octaryn_link_first_available_dependency(octaryn_native_lz4 lz4_available lz4::lz4 lz4_static)
    endif()
endif()

if(NOT TARGET octaryn::deps::zstd)
    octaryn_add_dependency_wrapper(octaryn_native_zstd octaryn::deps::zstd)
    find_package(zstd CONFIG QUIET)
    octaryn_link_first_available_dependency(octaryn_native_zstd zstd_available zstd::libzstd_static libzstd_static zstd::libzstd_shared libzstd_shared)
    if(NOT zstd_available)
        octaryn_fetch_source_dependency(
            zstd
            GITHUB_REPOSITORY facebook/zstd
            GIT_TAG v1.5.7
            SOURCE_SUBDIR build/cmake
            OPTIONS
                "BUILD_SHARED_LIBS OFF"
                "ZSTD_BUILD_PROGRAMS OFF"
                "ZSTD_BUILD_TESTS OFF")
        octaryn_link_first_available_dependency(octaryn_native_zstd zstd_available zstd::libzstd_static libzstd_static zstd::libzstd_shared libzstd_shared)
    endif()
endif()
