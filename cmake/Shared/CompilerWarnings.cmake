include_guard(GLOBAL)

function(octaryn_enable_default_warnings target_name)
    target_compile_options(${target_name} PRIVATE -Wall -Wextra -Wpedantic -Wconversion -Wshadow)
endfunction()
