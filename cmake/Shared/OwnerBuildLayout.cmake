include_guard(GLOBAL)

function(octaryn_owner_build_root output_var owner)
    set(${output_var} "${OCTARYN_BUILD_ROOT}/${owner}/${OCTARYN_BUILD_PRESET_NAME}" PARENT_SCOPE)
endfunction()

function(octaryn_owner_log_root output_var owner)
    set(${output_var} "${OCTARYN_LOG_ROOT}/${owner}" PARENT_SCOPE)
endfunction()

function(octaryn_apply_owner_layout target_name owner)
    octaryn_owner_build_root(owner_build_root "${owner}")
    octaryn_owner_log_root(owner_log_root "${owner}")

    file(MAKE_DIRECTORY
        "${owner_build_root}/bin"
        "${owner_build_root}/lib"
        "${owner_build_root}/generated"
        "${owner_log_root}")

    set_target_properties(${target_name} PROPERTIES
        ARCHIVE_OUTPUT_DIRECTORY "${owner_build_root}/lib"
        LIBRARY_OUTPUT_DIRECTORY "${owner_build_root}/lib"
        RUNTIME_OUTPUT_DIRECTORY "${owner_build_root}/bin")
endfunction()
