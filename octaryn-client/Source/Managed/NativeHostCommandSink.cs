using System.Runtime.InteropServices;
using Octaryn.Shared.Host;

namespace Octaryn.Client;

internal unsafe readonly struct NativeHostCommandSink : IHostCommandSink
{
    private readonly delegate* unmanaged[Cdecl]<HostCommand*, int> _enqueueCommand;

    private NativeHostCommandSink(delegate* unmanaged[Cdecl]<HostCommand*, int> enqueueCommand)
    {
        _enqueueCommand = enqueueCommand;
    }

    public bool IsValid => _enqueueCommand is not null;

    public static NativeHostCommandSink Create(ClientNativeHostApi* api)
    {
        if (api is null || api->Version != ClientNativeHostApi.VersionValue || api->Size != ClientNativeHostApi.SizeValue)
        {
            return default;
        }

        return new NativeHostCommandSink(api->EnqueueCommand);
    }

    public bool Enqueue(HostCommand command)
    {
        if (_enqueueCommand is null)
        {
            return false;
        }

        return _enqueueCommand(&command) != 0;
    }
}
