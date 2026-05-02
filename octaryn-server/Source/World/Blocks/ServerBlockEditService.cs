using Octaryn.Shared.World;

namespace Octaryn.Server.World.Blocks;

internal sealed class ServerBlockEditService(ServerBlockStore blocks, IBlockAuthorityRules authorityRules)
{
    public BlockId GetBlock(BlockPosition position)
    {
        return blocks.GetBlock(position);
    }

    public ServerBlockEditResult Apply(BlockEdit edit)
    {
        if (!CanApply(edit))
        {
            return default;
        }

        var result = blocks.SetBlock(edit);
        if (!result.Changed)
        {
            return result;
        }

        if (TryClearUnsupportedBlockAbove(edit, out var cascadeEdit))
        {
            return new ServerBlockEditResult(
                Applied: true,
                Changed: true,
                Changes: [edit, cascadeEdit]);
        }

        return result;
    }

    internal bool CanApply(BlockEdit edit)
    {
        if (!ServerBlockStore.IsValidPosition(edit.Position) || !authorityRules.IsKnownBlock(edit.Block))
        {
            return false;
        }

        if (edit.Block == BlockId.Air)
        {
            return true;
        }

        var belowPosition = new BlockPosition(edit.Position.X, edit.Position.Y - 1, edit.Position.Z);
        return authorityRules.CanApplyEdit(edit, blocks.GetBlock(belowPosition));
    }

    private bool TryClearUnsupportedBlockAbove(BlockEdit edit, out BlockEdit cascadeEdit)
    {
        cascadeEdit = default;
        if (edit.Position.Y + 1 >= ServerBlockLimits.WorldMaxYExclusive)
        {
            return false;
        }

        var abovePosition = new BlockPosition(edit.Position.X, edit.Position.Y + 1, edit.Position.Z);
        var aboveBlock = blocks.GetBlock(abovePosition);
        if (aboveBlock == BlockId.Air ||
            authorityRules.CanStaySupported(aboveBlock, abovePosition, edit.Block))
        {
            return false;
        }

        cascadeEdit = new BlockEdit(abovePosition, BlockId.Air);
        return blocks.SetBlock(cascadeEdit).Changed;
    }
}
