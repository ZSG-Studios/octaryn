using Octaryn.Shared.Host;
using Octaryn.Shared.World;

namespace Octaryn.Server.World.Blocks;

internal sealed class ServerBlockCommandSink(
    ServerBlockEditService blockEdits,
    ServerBlockChangeQueue? blockChanges = null,
    Action<IReadOnlyList<BlockEdit>>? changedEdits = null,
    IHostCommandSink? fallback = null) : IHostCommandSink
{
    public bool Enqueue(HostCommand command)
    {
        if (!CanEnqueue(command))
        {
            return false;
        }

        return command.Kind switch
        {
            HostCommandKind.SetBlock => ApplySetBlock(command),
            _ => fallback?.Enqueue(command) ?? false
        };
    }

    public bool CanEnqueue(HostCommand command)
    {
        if (!command.IsCurrent)
        {
            return false;
        }

        return command.Kind switch
        {
            HostCommandKind.SetBlock => CanApplySetBlock(command),
            _ => fallback is not null
        };
    }

    private bool CanApplySetBlock(HostCommand command)
    {
        return command.D is >= ushort.MinValue and <= ushort.MaxValue &&
            blockEdits.CanApply(new BlockEdit(
                new BlockPosition(command.A, command.B, command.C),
                new BlockId((ushort)command.D)));
    }

    private bool ApplySetBlock(HostCommand command)
    {
        var result = blockEdits.Apply(new BlockEdit(
            new BlockPosition(command.A, command.B, command.C),
            new BlockId((ushort)command.D)));
        if (result.Changed)
        {
            foreach (var change in result.Changes)
            {
                blockChanges?.Enqueue(change);
            }

            changedEdits?.Invoke(result.Changes);
        }

        return result.Applied;
    }
}
