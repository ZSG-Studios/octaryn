namespace Octaryn.Game.Gameplay;

internal static class GameplayMigrationMap
{
    public static readonly GameplayMigrationEntry[] Entries =
    {
        new("source/app/player/physics/movement.cpp", "player_move", "Input/PlayerInputSystem.cs", "frame input snapshot"),
        new("source/app/player/physics/physics.cpp", "player_move_walk", "Player/PlayerMovementIntentSystem.cs", "CharacterMovementIntent command"),
        new("source/app/player/physics/physics.cpp", "player_move_fly", "Player/PlayerMovementIntentSystem.cs", "Camera movement intent command"),
        new("source/app/player/player.cpp", "player_toggle_controller", "Player/PlayerControlState.cs", "SetPlayerControlMode command"),
        new("source/app/player/player.cpp", "player_rotate", "Input/PlayerInputSystem.cs", "mouse delta snapshot"),
        new("source/app/player/player.cpp", "player_update_query", "World/BlockInteractionSystem.cs", "raycast hit snapshot"),
        new("source/app/player/blocks.cpp", "player_place_block", "World/BlockInteractionSystem.cs", "SetBlock command"),
        new("source/app/player/blocks.cpp", "player_break_block", "World/BlockInteractionSystem.cs", "SetBlock command"),
        new("source/app/player/blocks.cpp", "player_select_block", "Player/PlayerInventorySystem.cs", "target block snapshot"),
        new("source/app/player/blocks.cpp", "player_change_block", "Player/PlayerInventorySystem.cs", "input edge snapshot"),
        new("source/app/player/spawn.cpp", "player_save_or_load", "Entities/PlayerPersistenceSystem.cs", "player persistence snapshot"),
        new("source/app/runtime/events.cpp", "gameplay input actions", "Input/PlayerInputSystem.cs", "input edge snapshot"),
        new("source/world/runtime/query.cpp", "world_raycast", "native query service", "raycast hit snapshot"),
        new("source/world/edit/queue.cpp", "world_queue_block_edit", "native command applier", "SetBlock result status"),
        new("source/world/edit/apply.cpp", "world_try_apply_block_edit", "native final validator", "SetBlock result status")
    };

    public static int RemainingNativeGameplaySurfaceCount => Entries.Length;
}

internal readonly record struct GameplayMigrationEntry(
    string NativeFile,
    string NativeSurface,
    string ManagedOwner,
    string RequiredBridge);
