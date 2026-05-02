using Octaryn.Basegame.Content.Blocks;
using Octaryn.Shared.World;

namespace Octaryn.Basegame.Gameplay.Interaction;

public static class BlockSupportRules
{
    public static bool CanStaySupported(BlockId block, BlockPosition position, BlockId belowBlock)
    {
        if (block == BlockId.Air)
        {
            return true;
        }

        if (position.Y <= 0)
        {
            return false;
        }

        if (RequiresGrass(block))
        {
            return belowBlock == BasegameBlockCatalog.Grass;
        }

        if (RequiresSolidBase(block))
        {
            return IsSolid(belowBlock);
        }

        return true;
    }

    public static bool RequiresGrass(BlockId block)
    {
        return BasegameBlockCatalog.RequiresGrass(block);
    }

    public static bool RequiresSolidBase(BlockId block)
    {
        return BasegameBlockCatalog.RequiresSolidBase(block);
    }

    public static bool IsSolid(BlockId block)
    {
        return BasegameBlockCatalog.IsSolid(block);
    }
}
