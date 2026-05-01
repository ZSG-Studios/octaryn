using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Octaryn.Shared.Host;

namespace Octaryn.Client;

internal static class ClientHostExports
{
    private static BasegameModuleActivator? s_basegame;
    private static bool s_initialized;

    [UnmanagedCallersOnly(EntryPoint = "octaryn_client_initialize", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe int Initialize(ClientNativeHostApi* nativeApi)
    {
        var commandSink = NativeHostCommandSink.Create(nativeApi);
        if (!commandSink.IsValid)
        {
            return -1;
        }

        if (s_initialized)
        {
            ShutdownCore();
        }

        s_basegame ??= new BasegameModuleActivator();
        var activateResult = s_basegame.Activate(commandSink);
        if (activateResult != 0)
        {
            return activateResult;
        }

        s_initialized = true;
        return 0;
    }

    [UnmanagedCallersOnly(EntryPoint = "octaryn_client_tick", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe int Tick(HostFrameSnapshot* frameSnapshot)
    {
        if (!s_initialized ||
            s_basegame is null ||
            frameSnapshot is null ||
            frameSnapshot->Version != HostFrameSnapshot.VersionValue ||
            frameSnapshot->Size != HostFrameSnapshot.SizeValue)
        {
            return -1;
        }

        if (frameSnapshot->Input.Size != HostInputSnapshot.SizeValue ||
            frameSnapshot->Timing.Size != HostFrameTimingSnapshot.SizeValue)
        {
            return -1;
        }

        s_basegame?.Tick(in *frameSnapshot);
        return 0;
    }

    [UnmanagedCallersOnly(EntryPoint = "octaryn_client_shutdown", CallConvs = [typeof(CallConvCdecl)])]
    public static void Shutdown()
    {
        ShutdownCore();
    }

    private static void ShutdownCore()
    {
        s_basegame?.Dispose();
        s_basegame = null;
        s_initialized = false;
    }
}
