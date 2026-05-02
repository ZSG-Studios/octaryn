include_guard(GLOBAL)

find_program(DOTNET_EXECUTABLE dotnet REQUIRED)

set(OCTARYN_NUGET_PACKAGES_DIR
    "${OCTARYN_WORKSPACE_ROOT_DIR}/build/dependencies/nuget"
    CACHE PATH
    "Workspace-managed NuGet package cache")

set(OCTARYN_DOTNET_TARGET_RUNTIME_ARGS)
if(OCTARYN_TARGET_DOTNET_RID)
    list(APPEND OCTARYN_DOTNET_TARGET_RUNTIME_ARGS
        --runtime "${OCTARYN_TARGET_DOTNET_RID}")
endif()
