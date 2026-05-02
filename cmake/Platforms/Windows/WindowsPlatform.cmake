include_guard(GLOBAL)

include(Shared/TargetArchitecture)

octaryn_arch_select(OCTARYN_TARGET_NATIVE_ARCHIVE_FORMAT pe-x86-64 coff-arm64)
octaryn_arch_select(OCTARYN_TARGET_DOTNET_RID win-x64 win-arm64)
octaryn_arch_select(OCTARYN_WINDOWS_OBJDUMP_NAME x86_64-w64-mingw32-objdump aarch64-w64-mingw32-objdump)
find_program(OCTARYN_TARGET_OBJDUMP ${OCTARYN_WINDOWS_OBJDUMP_NAME}
    PATHS "$ENV{OCTARYN_WINDOWS_CLANG_ROOT}/bin" "/opt/llvm-mingw/bin"
    NO_DEFAULT_PATH)
if(OCTARYN_TARGET_ARCH STREQUAL "arm64")
    find_program(OCTARYN_TARGET_READOBJ llvm-readobj
        PATHS "$ENV{OCTARYN_WINDOWS_CLANG_ROOT}/bin" "/opt/llvm-mingw/bin"
        NO_DEFAULT_PATH)
    if(OCTARYN_TARGET_READOBJ)
        set(OCTARYN_TARGET_OBJDUMP "${OCTARYN_TARGET_READOBJ}")
    endif()
endif()
