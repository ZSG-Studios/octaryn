using Octaryn.Shared.Host;
using Octaryn.Shared.World;

namespace Octaryn.Server.World.Blocks;

internal sealed class ServerClientBlockCommandQueue(ServerBlockCommandSink blockCommands, IBlockAuthorityRules authorityRules)
{
    public const int MaxPendingCommands = 4096;

    private readonly Queue<HostCommand> _commands = new(MaxPendingCommands);

    public int PendingCount => _commands.Count;

    public bool Enqueue(HostCommand command)
    {
        if (_commands.Count >= MaxPendingCommands || !CanQueue(command))
        {
            return false;
        }

        _commands.Enqueue(command);
        return true;
    }

    public int Drain()
    {
        var applied = 0;
        while (_commands.TryDequeue(out var command))
        {
            if (blockCommands.Enqueue(command))
            {
                applied++;
            }
        }

        return applied;
    }

    internal bool CanQueue(HostCommand command)
    {
        return command.IsCurrent &&
            command.Kind == HostCommandKind.SetBlock &&
            command.D is >= ushort.MinValue and <= ushort.MaxValue &&
            (command.D == BlockId.Air.Value || authorityRules.IsClientPlaceable(new BlockId((ushort)command.D))) &&
            blockCommands.CanEnqueue(command);
    }
}
