using Octaryn.Basegame.Gameplay.Player;
using Octaryn.Shared.World;

return BasegamePlayerProbe.Run();

internal static class BasegamePlayerProbe
{
    public static int Run()
    {
        Require(PlayerBlockSelectionState.Default.SelectedBlock.Value == 25, "default selected block is yellow torch");
        Require(PlayerBlockSelectionRules.IsPlaceable(new BlockId(1)), "grass is placeable");
        Require(PlayerBlockSelectionRules.IsPlaceable(new BlockId(14)), "water source is placeable");
        Require(!PlayerBlockSelectionRules.IsPlaceable(BlockId.Air), "air is not placeable");
        Require(!PlayerBlockSelectionRules.IsPlaceable(new BlockId(8)), "cloud is not placeable");
        Require(!PlayerBlockSelectionRules.IsPlaceable(new BlockId(15)), "flowing water is not placeable");
        Require(!PlayerBlockSelectionRules.IsPlaceable(new BlockId(32)), "flowing lava is not placeable");

        var selected = new PlayerBlockSelectionState(new BlockId(25));
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(selected, 1).SelectedBlock.Value == 26, "change block forward");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(selected, -1).SelectedBlock.Value == 24, "change block backward");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(new BlockId(7)), 1).SelectedBlock.Value == 9, "change skips cloud");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(new BlockId(7)), 2).SelectedBlock.Value == 9, "change raw block offset skips cloud");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(new BlockId(8)), 1).SelectedBlock.Value == 9, "change from cloud advances to bush");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(new BlockId(8)), -1).SelectedBlock.Value == 7, "change from cloud retreats to leaves");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(new BlockId(14)), 1).SelectedBlock.Value == 22, "change skips flowing water");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(new BlockId(15)), -1).SelectedBlock.Value == 14, "change from flowing water retreats to source");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(new BlockId(31)), 1).SelectedBlock.Value == 1, "change wraps forward");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(new BlockId(38)), -1).SelectedBlock.Value == 31, "change from flowing lava retreats to source");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(new BlockId(1)), -1).SelectedBlock.Value == 31, "change wraps backward");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(new BlockId(8)), 0).SelectedBlock.Value == 25, "zero change from invalid block returns default");

        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, BlockId.Air) == selected, "select ignores air");
        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, new BlockId(8)) == selected, "select ignores cloud");
        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, new BlockId(14)) == selected, "select ignores water source");
        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, new BlockId(15)) == selected, "select ignores flowing water");
        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, new BlockId(31)) == selected, "select ignores lava source");
        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, new BlockId(5)).SelectedBlock.Value == 5, "select target block");
        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, new BlockId(9)).SelectedBlock.Value == 9, "select target sprite block");
        return 0;
    }

    private static void Require(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException(name);
        }
    }
}
