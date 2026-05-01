include_guard(GLOBAL)

set(OCTARYN_TOOL_DEPENDENCIES_SCAFFOLD ON)

if(NOT TARGET octaryn::deps::glaze)
    add_library(octaryn_tool_glaze INTERFACE)
    add_library(octaryn::deps::glaze ALIAS octaryn_tool_glaze)

    find_package(glaze CONFIG QUIET)
    if(TARGET glaze::glaze)
        target_link_libraries(octaryn_tool_glaze INTERFACE glaze::glaze)
    endif()
endif()

if(NOT TARGET octaryn::deps::shadercross)
    add_library(octaryn_tool_shadercross INTERFACE)
    add_library(octaryn::deps::shadercross ALIAS octaryn_tool_shadercross)

    find_package(SDL3_shadercross CONFIG QUIET)
    if(TARGET SDL3_shadercross::SDL3_shadercross)
        target_link_libraries(octaryn_tool_shadercross INTERFACE SDL3_shadercross::SDL3_shadercross)
    elseif(TARGET SDL3_shadercross-static)
        target_link_libraries(octaryn_tool_shadercross INTERFACE SDL3_shadercross-static)
    endif()
endif()

if(NOT TARGET octaryn::deps::shaderc)
    add_library(octaryn_tool_shaderc INTERFACE)
    add_library(octaryn::deps::shaderc ALIAS octaryn_tool_shaderc)

    find_package(shaderc CONFIG QUIET)
    if(TARGET shaderc_combined)
        target_link_libraries(octaryn_tool_shaderc INTERFACE shaderc_combined)
    elseif(TARGET shaderc_shared)
        target_link_libraries(octaryn_tool_shaderc INTERFACE shaderc_shared)
    elseif(TARGET shaderc)
        target_link_libraries(octaryn_tool_shaderc INTERFACE shaderc)
    endif()
endif()

if(NOT TARGET octaryn::deps::spirv_tools)
    add_library(octaryn_tool_spirv_tools INTERFACE)
    add_library(octaryn::deps::spirv_tools ALIAS octaryn_tool_spirv_tools)

    find_package(SPIRV-Tools CONFIG QUIET)
    if(TARGET SPIRV-Tools)
        target_link_libraries(octaryn_tool_spirv_tools INTERFACE SPIRV-Tools)
    elseif(TARGET SPIRV-Tools-static)
        target_link_libraries(octaryn_tool_spirv_tools INTERFACE SPIRV-Tools-static)
    elseif(TARGET SPIRV-Tools-opt)
        target_link_libraries(octaryn_tool_spirv_tools INTERFACE SPIRV-Tools-opt)
    endif()
endif()

if(NOT TARGET octaryn::deps::spirv_cross)
    add_library(octaryn_tool_spirv_cross INTERFACE)
    add_library(octaryn::deps::spirv_cross ALIAS octaryn_tool_spirv_cross)

    find_package(spirv_cross_core CONFIG QUIET)
    find_package(spirv_cross_reflect CONFIG QUIET)
    find_package(spirv_cross_msl CONFIG QUIET)
    foreach(octaryn_spirv_cross_target
        spirv-cross-core
        spirv-cross-reflect
        spirv-cross-msl
        SPIRV-Cross::spirv-cross-core
        SPIRV-Cross::spirv-cross-reflect
        SPIRV-Cross::spirv-cross-msl)
        if(TARGET "${octaryn_spirv_cross_target}")
            target_link_libraries(octaryn_tool_spirv_cross INTERFACE "${octaryn_spirv_cross_target}")
        endif()
    endforeach()
endif()
