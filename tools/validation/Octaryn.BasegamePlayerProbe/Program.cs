using Octaryn.Basegame.Content.Blocks;
using Octaryn.Basegame.Gameplay.Player;
using Octaryn.Shared.World;

return BasegamePlayerProbe.Run();

internal static class BasegamePlayerProbe
{
    public static int Run()
    {
        Require(PlayerBlockSelectionState.Default.SelectedBlock == BasegameBlockCatalog.YellowTorch, "default selected block is yellow torch");
        Require(PlayerBlockSelectionRules.IsPlaceable(BasegameBlockCatalog.Grass), "grass is placeable");
        Require(PlayerBlockSelectionRules.IsPlaceable(BasegameBlockCatalog.WaterSource), "water source is placeable");
        Require(!PlayerBlockSelectionRules.IsPlaceable(BlockId.Air), "air is not placeable");
        Require(!PlayerBlockSelectionRules.IsPlaceable(BasegameBlockCatalog.Cloud), "cloud is not placeable");
        Require(!PlayerBlockSelectionRules.IsPlaceable(BasegameBlockCatalog.WaterLevelOne), "flowing water is not placeable");
        Require(!PlayerBlockSelectionRules.IsPlaceable(BasegameBlockCatalog.LavaLevelOne), "flowing lava is not placeable");

        var selected = new PlayerBlockSelectionState(BasegameBlockCatalog.YellowTorch);
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(selected, 1).SelectedBlock == BasegameBlockCatalog.CyanTorch, "change block forward");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(selected, -1).SelectedBlock == BasegameBlockCatalog.BlueTorch, "change block backward");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(BasegameBlockCatalog.Leaves), 1).SelectedBlock == BasegameBlockCatalog.Bush, "change skips cloud");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(BasegameBlockCatalog.Leaves), 2).SelectedBlock == BasegameBlockCatalog.Bush, "change raw block offset skips cloud");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(BasegameBlockCatalog.Cloud), 1).SelectedBlock == BasegameBlockCatalog.Bush, "change from cloud advances to bush");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(BasegameBlockCatalog.Cloud), -1).SelectedBlock == BasegameBlockCatalog.Leaves, "change from cloud retreats to leaves");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(BasegameBlockCatalog.WaterSource), 1).SelectedBlock == BasegameBlockCatalog.RedTorch, "change skips flowing water");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(BasegameBlockCatalog.WaterLevelOne), -1).SelectedBlock == BasegameBlockCatalog.WaterSource, "change from flowing water retreats to source");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(BasegameBlockCatalog.LavaSource), 1).SelectedBlock == BasegameBlockCatalog.Grass, "change wraps forward");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(BasegameBlockCatalog.LavaLevelSeven), -1).SelectedBlock == BasegameBlockCatalog.LavaSource, "change from flowing lava retreats to source");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(BasegameBlockCatalog.Grass), -1).SelectedBlock == BasegameBlockCatalog.LavaSource, "change wraps backward");
        Require(PlayerBlockSelectionRules.ChangeSelectedBlock(new PlayerBlockSelectionState(BasegameBlockCatalog.Cloud), 0).SelectedBlock == BasegameBlockCatalog.YellowTorch, "zero change from invalid block returns default");

        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, BlockId.Air) == selected, "select ignores air");
        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, BasegameBlockCatalog.Cloud) == selected, "select ignores cloud");
        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, BasegameBlockCatalog.WaterSource) == selected, "select ignores water source");
        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, BasegameBlockCatalog.WaterLevelOne) == selected, "select ignores flowing water");
        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, BasegameBlockCatalog.LavaSource) == selected, "select ignores lava source");
        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, BasegameBlockCatalog.Stone).SelectedBlock == BasegameBlockCatalog.Stone, "select target block");
        Require(PlayerBlockSelectionRules.SelectTargetBlock(selected, BasegameBlockCatalog.Bush).SelectedBlock == BasegameBlockCatalog.Bush, "select target sprite block");
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
