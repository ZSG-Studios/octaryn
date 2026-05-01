include_guard(GLOBAL)

include(Dependencies/SourceDependencyCache)

set(OCTARYN_SERVER_DEPENDENCIES_SCAFFOLD ON)

if(NOT TARGET octaryn::deps::fastnoise2)
    octaryn_add_dependency_wrapper(octaryn_server_fastnoise2 octaryn::deps::fastnoise2)
    octaryn_fetch_source_dependency(
        FastNoise2
        GITHUB_REPOSITORY Auburn/FastNoise2
        GIT_TAG v1.1.1
        OPTIONS
            "FASTNOISE2_NOISETOOL OFF")
    octaryn_link_first_available_dependency(octaryn_server_fastnoise2 fastnoise2_available FastNoise FastNoise2)
endif()

if(NOT TARGET octaryn::deps::jolt)
    octaryn_add_dependency_wrapper(octaryn_server_jolt octaryn::deps::jolt)
    octaryn_fetch_source_dependency(
        JoltPhysics
        GITHUB_REPOSITORY jrouwe/JoltPhysics
        GIT_TAG v5.5.0
        SOURCE_SUBDIR Build
        OPTIONS
            "GENERATE_DEBUG_SYMBOLS OFF"
            "INTERPROCEDURAL_OPTIMIZATION OFF"
            "CPP_RTTI_ENABLED ON")
    octaryn_link_first_available_dependency(octaryn_server_jolt jolt_available Jolt)
endif()
