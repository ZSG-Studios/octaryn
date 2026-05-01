using Octaryn.Shared.Host;

namespace Octaryn.Server;

internal sealed class ServerConsoleCommandSink : IHostCommandSink
{
    public bool Enqueue(HostCommand command)
    {
        _ = command;
        return false;
    }
}
