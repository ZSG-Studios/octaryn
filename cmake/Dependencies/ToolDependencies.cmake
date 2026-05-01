include_guard(GLOBAL)

include(Dependencies/SourceDependencyCache)

set(OCTARYN_TOOL_DEPENDENCIES_SCAFFOLD ON)

if(NOT TARGET octaryn::deps::glaze)
    octaryn_add_dependency_wrapper(octaryn_tool_glaze octaryn::deps::glaze)

    find_package(glaze CONFIG QUIET)
    if(TARGET glaze::glaze)
        target_link_libraries(octaryn_tool_glaze INTERFACE glaze::glaze)
    else()
        octaryn_fetch_source_dependency(
            glaze
            GITHUB_REPOSITORY stephenberry/glaze
            GIT_TAG v7.4.0
            OPTIONS
                "glaze_BUILD_TESTS OFF")
        if(TARGET glaze::glaze)
            target_link_libraries(octaryn_tool_glaze INTERFACE glaze::glaze)
        endif()
    endif()
endif()

if(NOT TARGET octaryn::deps::shadercross)
    octaryn_add_dependency_wrapper(octaryn_tool_shadercross octaryn::deps::shadercross)

    find_package(SDL3_shadercross CONFIG QUIET)
    if(TARGET SDL3_shadercross::SDL3_shadercross)
        target_link_libraries(octaryn_tool_shadercross INTERFACE SDL3_shadercross::SDL3_shadercross)
    elseif(TARGET SDL3_shadercross-static)
        target_link_libraries(octaryn_tool_shadercross INTERFACE SDL3_shadercross-static)
    endif()
endif()

if(NOT TARGET octaryn::deps::shaderc)
    octaryn_add_dependency_wrapper(octaryn_tool_shaderc octaryn::deps::shaderc)

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
    octaryn_add_dependency_wrapper(octaryn_tool_spirv_tools octaryn::deps::spirv_tools)

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
    octaryn_add_dependency_wrapper(octaryn_tool_spirv_cross octaryn::deps::spirv_cross)

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

if(NOT TARGET octaryn::deps::fastgltf)
    octaryn_add_dependency_wrapper(octaryn_tool_fastgltf octaryn::deps::fastgltf)
    find_package(fastgltf CONFIG QUIET)
    octaryn_link_first_available_dependency(octaryn_tool_fastgltf fastgltf_available fastgltf::fastgltf)
    if(NOT fastgltf_available)
        octaryn_fetch_source_dependency(
            fastgltf
            GITHUB_REPOSITORY spnda/fastgltf
            GIT_TAG v0.9.0
            OPTIONS
                "FASTGLTF_DOWNLOAD_SIMDJSON OFF"
                "FASTGLTF_TESTS OFF")
        octaryn_link_first_available_dependency(octaryn_tool_fastgltf fastgltf_available fastgltf::fastgltf)
    endif()
endif()

if(NOT TARGET octaryn::deps::ktx)
    octaryn_add_dependency_wrapper(octaryn_tool_ktx octaryn::deps::ktx)
    find_package(KTX CONFIG QUIET)
    octaryn_link_first_available_dependency(octaryn_tool_ktx ktx_available KTX::ktx)
    if(NOT ktx_available)
        octaryn_fetch_source_dependency(
            KTX-Software
            GITHUB_REPOSITORY KhronosGroup/KTX-Software
            GIT_TAG v4.4.2
            OPTIONS
                "BUILD_SHARED_LIBS OFF"
                "KTX_FEATURE_TESTS OFF"
                "KTX_FEATURE_TOOLS OFF"
                "KTX_FEATURE_LOADTEST_APPS OFF"
                "KTX_FEATURE_GL_UPLOAD OFF"
                "ASTCENC_INVARIANCE OFF")
        octaryn_link_first_available_dependency(octaryn_tool_ktx ktx_available KTX::ktx)
    endif()
endif()

if(NOT TARGET octaryn::deps::meshoptimizer)
    octaryn_add_dependency_wrapper(octaryn_tool_meshoptimizer octaryn::deps::meshoptimizer)
    find_package(meshoptimizer CONFIG QUIET)
    octaryn_link_first_available_dependency(octaryn_tool_meshoptimizer meshoptimizer_available meshoptimizer)
    if(NOT meshoptimizer_available)
        octaryn_fetch_source_dependency(
            meshoptimizer
            GITHUB_REPOSITORY zeux/meshoptimizer
            GIT_TAG v1.1.1)
        octaryn_link_first_available_dependency(octaryn_tool_meshoptimizer meshoptimizer_available meshoptimizer)
    endif()
endif()
