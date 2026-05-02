using Octaryn.Basegame.Content.Blocks;
using Octaryn.Shared.World;

namespace Octaryn.Basegame.Gameplay.Player;

public static class PlayerBlockSelectionRules
{
    public static BlockId DefaultSelectedBlock => BasegameBlockCatalog.DefaultSelectedBlock;

    public static PlayerBlockSelectionState SelectTargetBlock(
        PlayerBlockSelectionState current,
        BlockId targetBlock)
    {
        return BasegameBlockCatalog.IsTargetable(targetBlock)
            ? new PlayerBlockSelectionState(targetBlock)
            : current;
    }

    public static PlayerBlockSelectionState ChangeSelectedBlock(
        PlayerBlockSelectionState current,
        int delta)
    {
        var currentIndex = BasegameBlockCatalog.PlaceableIndexOf(current.SelectedBlock);
        if (currentIndex < 0)
        {
            currentIndex = 0;
            delta--;
        }

        var nextIndex = PositiveModulo(currentIndex + delta, BasegameBlockCatalog.PlaceableCount);
        return new PlayerBlockSelectionState(BasegameBlockCatalog.PlaceableAt(nextIndex));
    }

    public static bool IsPlaceable(BlockId block)
    {
        return BasegameBlockCatalog.IsPlaceable(block);
    }

    private static int PositiveModulo(int value, int divisor)
    {
        var result = value % divisor;
        return result < 0 ? result + divisor : result;
    }
}
