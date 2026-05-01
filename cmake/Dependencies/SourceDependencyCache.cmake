include_guard(GLOBAL)

include(FetchContent)

if(POLICY CMP0097)
    cmake_policy(SET CMP0097 NEW)
endif()

option(OCTARYN_ENABLE_SOURCE_DEPENDENCIES "Fetch approved third-party dependencies into build/dependencies when system packages are unavailable." ON)

set(OCTARYN_SOURCE_DEPENDENCY_ROOT
    "${OCTARYN_DEPENDENCY_BUILD_ROOT}/${OCTARYN_BUILD_PRESET_NAME}"
    CACHE PATH
    "Preset-scoped root for fetched third-party dependency sources and builds.")

set(FETCHCONTENT_BASE_DIR "${OCTARYN_SOURCE_DEPENDENCY_ROOT}/fetchcontent" CACHE PATH "Octaryn FetchContent cache root." FORCE)
set(FETCHCONTENT_QUIET OFF CACHE BOOL "Show FetchContent dependency progress." FORCE)

function(octaryn_dependency_github_url output_var repository)
    set(${output_var} "https://github.com/${repository}.git" PARENT_SCOPE)
endfunction()

function(octaryn_add_dependency_wrapper wrapper_target alias_target)
    if(NOT TARGET ${wrapper_target})
        add_library(${wrapper_target} INTERFACE)
    endif()

    if(NOT TARGET ${alias_target})
        add_library(${alias_target} ALIAS ${wrapper_target})
    endif()
endfunction()

function(octaryn_link_first_available_dependency wrapper_target output_var)
    set(found_dependency OFF)
    foreach(dependency_target IN LISTS ARGN)
        if(TARGET ${dependency_target})
            target_link_libraries(${wrapper_target} INTERFACE ${dependency_target})
            set(found_dependency ON)
            break()
        endif()
    endforeach()

    set(${output_var} "${found_dependency}" PARENT_SCOPE)
endfunction()

function(octaryn_fetch_source_dependency dependency_name)
    if(NOT OCTARYN_ENABLE_SOURCE_DEPENDENCIES)
        return()
    endif()

    cmake_parse_arguments(
        OCTARYN_FETCH
        "NO_GIT_SUBMODULES"
        "GITHUB_REPOSITORY;GIT_REPOSITORY;GIT_TAG;URL;URL_HASH;VERSION;SOURCE_SUBDIR;GIT_SUBMODULES"
        "OPTIONS"
        ${ARGN})

    foreach(option_assignment IN LISTS OCTARYN_FETCH_OPTIONS)
        string(REPLACE " " ";" option_parts "${option_assignment}")
        list(LENGTH option_parts option_part_count)
        if(option_part_count GREATER 1)
            list(GET option_parts 0 option_name)
            list(REMOVE_AT option_parts 0)
            string(REPLACE ";" " " option_value "${option_parts}")
            set(${option_name} "${option_value}" CACHE STRING "Option for ${dependency_name}" FORCE)
        endif()
    endforeach()

    if(OCTARYN_FETCH_URL)
        set(declare_args URL "${OCTARYN_FETCH_URL}")
        if(OCTARYN_FETCH_URL_HASH)
            list(APPEND declare_args URL_HASH "${OCTARYN_FETCH_URL_HASH}")
        endif()
    else()
        if(OCTARYN_FETCH_GIT_REPOSITORY)
            set(dependency_repository "${OCTARYN_FETCH_GIT_REPOSITORY}")
        elseif(OCTARYN_FETCH_GITHUB_REPOSITORY)
            octaryn_dependency_github_url(dependency_repository "${OCTARYN_FETCH_GITHUB_REPOSITORY}")
        else()
            message(FATAL_ERROR "Source dependency ${dependency_name} must declare URL, GIT_REPOSITORY, or GITHUB_REPOSITORY.")
        endif()

        if(OCTARYN_FETCH_GIT_TAG)
            set(dependency_tag "${OCTARYN_FETCH_GIT_TAG}")
        elseif(OCTARYN_FETCH_VERSION)
            set(dependency_tag "${OCTARYN_FETCH_VERSION}")
        else()
            message(FATAL_ERROR "Source dependency ${dependency_name} must declare GIT_TAG or VERSION.")
        endif()

        set(declare_args
            GIT_REPOSITORY "${dependency_repository}"
            GIT_TAG "${dependency_tag}"
            GIT_SHALLOW TRUE
            GIT_PROGRESS TRUE)
    endif()

    if(OCTARYN_FETCH_NO_GIT_SUBMODULES)
        list(APPEND declare_args GIT_SUBMODULES "")
    elseif(DEFINED OCTARYN_FETCH_GIT_SUBMODULES)
        list(APPEND declare_args GIT_SUBMODULES "${OCTARYN_FETCH_GIT_SUBMODULES}")
    endif()

    if(OCTARYN_FETCH_SOURCE_SUBDIR)
        list(APPEND declare_args SOURCE_SUBDIR "${OCTARYN_FETCH_SOURCE_SUBDIR}")
    endif()

    FetchContent_Declare(${dependency_name} ${declare_args})
    FetchContent_MakeAvailable(${dependency_name})
endfunction()

function(octaryn_fetch_header_dependency dependency_name output_var)
    if(NOT OCTARYN_ENABLE_SOURCE_DEPENDENCIES)
        set(${output_var} "" PARENT_SCOPE)
        return()
    endif()

    cmake_parse_arguments(
        OCTARYN_FETCH
        ""
        "GITHUB_REPOSITORY;GIT_REPOSITORY;GIT_TAG;VERSION"
        ""
        ${ARGN})

    if(OCTARYN_FETCH_GIT_REPOSITORY)
        set(dependency_repository "${OCTARYN_FETCH_GIT_REPOSITORY}")
    elseif(OCTARYN_FETCH_GITHUB_REPOSITORY)
        octaryn_dependency_github_url(dependency_repository "${OCTARYN_FETCH_GITHUB_REPOSITORY}")
    else()
        message(FATAL_ERROR "Header dependency ${dependency_name} must declare GIT_REPOSITORY or GITHUB_REPOSITORY.")
    endif()

    if(OCTARYN_FETCH_GIT_TAG)
        set(dependency_tag "${OCTARYN_FETCH_GIT_TAG}")
    elseif(OCTARYN_FETCH_VERSION)
        set(dependency_tag "${OCTARYN_FETCH_VERSION}")
    else()
        message(FATAL_ERROR "Header dependency ${dependency_name} must declare GIT_TAG or VERSION.")
    endif()

    FetchContent_Declare(
        ${dependency_name}
        GIT_REPOSITORY "${dependency_repository}"
        GIT_TAG "${dependency_tag}"
        GIT_SHALLOW FALSE
        GIT_PROGRESS TRUE)

    FetchContent_GetProperties(${dependency_name})
    if(NOT ${dependency_name}_POPULATED)
        if(POLICY CMP0169)
            cmake_policy(PUSH)
            cmake_policy(SET CMP0169 OLD)
        endif()
        FetchContent_Populate(${dependency_name})
        if(POLICY CMP0169)
            cmake_policy(POP)
        endif()
    endif()

    FetchContent_GetProperties(${dependency_name})
    set(${output_var} "${${dependency_name}_SOURCE_DIR}" PARENT_SCOPE)
endfunction()

function(octaryn_add_header_only_dependency wrapper_target include_dir)
    if(NOT TARGET ${wrapper_target})
        add_library(${wrapper_target} INTERFACE)
    endif()

    if(include_dir)
        target_include_directories(${wrapper_target}
            SYSTEM INTERFACE
                "${include_dir}")
    endif()
endfunction()
