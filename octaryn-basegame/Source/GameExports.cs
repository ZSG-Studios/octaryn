using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Octaryn.Engine.Api;
using Octaryn.Game.Gameplay;

namespace Octaryn.Game;

public static class GameExports
{
    private static GameContext? s_context;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe int Initialize(OctarynNativeApi* nativeApi)
    {
        if (s_context is not null)
        {
            return 0;
        }

        var nativeBridge = OctarynNativeBridge.Create(nativeApi);
        if (!nativeBridge.IsValid)
        {
            return -1;
        }

        s_context = GameContext.Create(nativeBridge);
        Console.WriteLine($"Octaryn managed gameplay migration remaining={GameplayMigrationMap.RemainingNativeGameplaySurfaceCount}");
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void Tick(OctarynFrameSnapshot* frameSnapshot)
    {
        if (frameSnapshot is null ||
            frameSnapshot->Version != OctarynFrameSnapshot.VersionValue ||
            frameSnapshot->Size != (uint)sizeof(OctarynFrameSnapshot))
        {
            return;
        }

        s_context?.Tick(in *frameSnapshot);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void Shutdown()
    {
        s_context?.Dispose();
        s_context = null;
    }
}
