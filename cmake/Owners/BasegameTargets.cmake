include_guard(GLOBAL)

include(Owners/DotNetOwner)
include(Owners/NativeOwner)
include(Dependencies/BasegameDependencies)

octaryn_add_native_owner(octaryn_basegame_native)
add_dependencies(octaryn_basegame_native octaryn_shared_native)

octaryn_add_dotnet_owner(
    octaryn_basegame
    basegame
    "${OCTARYN_WORKSPACE_ROOT_DIR}/octaryn-basegame/Octaryn.Basegame.csproj")

add_dependencies(octaryn_basegame octaryn_shared octaryn_basegame_native)

add_custom_command(
    OUTPUT "${octaryn_basegame_STAMP}"
    APPEND
    DEPENDS
        "${octaryn_shared_STAMP}")
