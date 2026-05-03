using Octaryn.Basegame.Content.Blocks;
using Octaryn.Basegame.Content.Fluids;
using Octaryn.Basegame.Gameplay.Interaction;
using Octaryn.Shared.World;

return BasegameInteractionProbe.Run();

internal static class BasegameInteractionProbe
{
    public static int Run()
    {
        var aboveGround = new BlockPosition(0, 2, 0);
        var bottomLayer = new BlockPosition(0, 0, 0);

        Require(BlockSupportRules.CanStaySupported(BlockId.Air, bottomLayer, BlockId.Air), "air is always supported");
        Require(BlockSupportRules.CanStaySupported(BasegameBlockCatalog.Grass, aboveGround, BlockId.Air), "grass does not need base support");
        Require(BlockSupportRules.CanStaySupported(BasegameBlockCatalog.Grass, bottomLayer, BasegameBlockCatalog.Grass), "basegame support rules do not own world floor policy");

        Require(BlockSupportRules.RequiresGrass(BasegameBlockCatalog.Bush), "bush requires grass");
        Require(BlockSupportRules.RequiresGrass(BasegameBlockCatalog.Lavender), "lavender requires grass");
        Require(!BlockSupportRules.RequiresGrass(BasegameBlockCatalog.RedTorch), "torch does not require grass");
        Require(BlockSupportRules.CanStaySupported(BasegameBlockCatalog.Bush, aboveGround, BasegameBlockCatalog.Grass), "bush accepts grass base");
        Require(!BlockSupportRules.CanStaySupported(BasegameBlockCatalog.Bush, aboveGround, BasegameBlockCatalog.Dirt), "bush rejects dirt base");

        Require(BlockSupportRules.RequiresSolidBase(BasegameBlockCatalog.RedTorch), "red torch requires solid base");
        Require(BlockSupportRules.RequiresSolidBase(BasegameBlockCatalog.WhiteTorch), "white torch requires solid base");
        Require(!BlockSupportRules.RequiresSolidBase(BasegameBlockCatalog.Bush), "bush does not require solid base");
        Require(BlockSupportRules.CanStaySupported(BasegameBlockCatalog.YellowTorch, aboveGround, BasegameBlockCatalog.Glass), "torch accepts glass base");
        Require(!BlockSupportRules.CanStaySupported(BasegameBlockCatalog.YellowTorch, aboveGround, BasegameBlockCatalog.WaterSource), "torch rejects water base");

        Require(BlockSupportRules.IsSolid(BasegameBlockCatalog.Grass), "grass is solid");
        Require(BlockSupportRules.IsSolid(BasegameBlockCatalog.Leaves), "leaves are solid");
        Require(BlockSupportRules.IsSolid(BasegameBlockCatalog.Planks), "planks are solid");
        Require(BlockSupportRules.IsSolid(BasegameBlockCatalog.Glass), "glass is solid");
        Require(!BlockSupportRules.IsSolid(BasegameBlockCatalog.Cloud), "cloud is not solid");
        Require(!BlockSupportRules.IsSolid(BasegameBlockCatalog.WaterSource), "water is not solid");
        Require(!BlockSupportRules.IsSolid(BasegameBlockCatalog.RedTorch), "torch is not solid");

        var authorityRules = new BasegameBlockAuthorityRules();
        Require(authorityRules.CanApplyEdit(new BlockEdit(new BlockPosition(3, 4, 5), BasegameBlockCatalog.Stone), BlockId.Air), "authority accepts unsupported-independent edit");
        Require(!authorityRules.CanApplyEdit(new BlockEdit(new BlockPosition(3, 4, 5), BasegameBlockCatalog.Bush), BasegameBlockCatalog.Dirt), "authority rejects unsupported grass block edit");
        Require(authorityRules.CanStaySupported(BasegameBlockCatalog.RedTorch, aboveGround, BasegameBlockCatalog.Glass), "authority accepts supported solid-base block");
        Require(authorityRules.IsClientPlaceable(BasegameBlockCatalog.LavaSource), "authority accepts client-placeable lava source");
        Require(!authorityRules.IsClientPlaceable(BasegameBlockCatalog.LavaLevelOne), "authority rejects non-placeable lava flow");
        ValidateReplacementRules();
        ValidateSkylightOpacity();
        ValidateFluids();
        return 0;
    }

    private static void ValidateReplacementRules()
    {
        Require(BlockReplacementRules.CanBeReplacedByFluid(BasegameBlockCatalog.Leaves), "fluid replaces leaves");
        Require(BlockReplacementRules.CanBeReplacedByFluid(BasegameBlockCatalog.Bush), "fluid replaces bush");
        Require(BlockReplacementRules.CanBeReplacedByFluid(BasegameBlockCatalog.Lavender), "fluid replaces lavender");
        Require(!BlockReplacementRules.CanBeReplacedByFluid(BlockId.Air), "fluid replacement rule does not include air");
        Require(!BlockReplacementRules.CanBeReplacedByFluid(BasegameBlockCatalog.Grass), "fluid does not replace grass");
        Require(!BlockReplacementRules.CanBeReplacedByFluid(BasegameBlockCatalog.WaterSource), "fluid replacement rule does not include fluids");
        Require(!BlockReplacementRules.CanBeReplacedByFluid(BasegameBlockCatalog.Glass), "fluid does not replace glass");
    }

    private static void ValidateSkylightOpacity()
    {
        Require(BasegameBlockCatalog.SkylightOpacity(BlockId.Air) == 0, "air has no skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(BasegameBlockCatalog.Leaves) == 1, "leaves have low skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(BasegameBlockCatalog.WaterSource) == 2, "water source has fluid skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(BasegameBlockCatalog.WaterLevelSeven) == 2, "flowing water has fluid skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(BasegameBlockCatalog.LavaSource) == 2, "lava source has fluid skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(BasegameBlockCatalog.LavaLevelSeven) == 2, "flowing lava has fluid skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(BasegameBlockCatalog.Glass) == 0, "glass has no skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(BasegameBlockCatalog.Stone) == 15, "stone blocks skylight");
        Require(BasegameBlockCatalog.SkylightOpacity(new BlockId(ushort.MaxValue)) == 0, "unknown block has no skylight opacity");
    }

    private static void ValidateFluids()
    {
        Require(BasegameBlockCatalog.FluidKind(BasegameBlockCatalog.WaterSource) == BasegameFluidKind.Water, "water fluid kind");
        Require(BasegameBlockCatalog.FluidKind(BasegameBlockCatalog.WaterLevelOne) == BasegameFluidKind.Water, "flowing water fluid kind");
        Require(BasegameBlockCatalog.FluidKind(BasegameBlockCatalog.LavaSource) == BasegameFluidKind.Lava, "lava fluid kind");
        Require(BasegameBlockCatalog.FluidKind(BasegameBlockCatalog.LavaLevelOne) == BasegameFluidKind.Lava, "flowing lava fluid kind");
        Require(BasegameBlockCatalog.FluidKind(BasegameBlockCatalog.Grass) == BasegameFluidKind.None, "grass has no fluid kind");

        Require(BasegameBlockCatalog.IsWater(BasegameBlockCatalog.WaterSource), "water source is water");
        Require(BasegameBlockCatalog.IsWater(BasegameBlockCatalog.WaterLevelSeven), "water level seven is water");
        Require(!BasegameBlockCatalog.IsWater(BasegameBlockCatalog.LavaSource), "lava is not water");
        Require(BasegameBlockCatalog.IsLava(BasegameBlockCatalog.LavaSource), "lava source is lava");
        Require(BasegameBlockCatalog.IsLava(BasegameBlockCatalog.LavaLevelSeven), "lava level seven is lava");
        Require(!BasegameBlockCatalog.IsLava(BasegameBlockCatalog.WaterSource), "water is not lava");

        Require(BasegameBlockCatalog.IsFluid(BasegameBlockCatalog.WaterSource), "water source is fluid");
        Require(BasegameBlockCatalog.IsFluid(BasegameBlockCatalog.LavaLevelSeven), "lava level seven is fluid");
        Require(!BasegameBlockCatalog.IsFluid(BasegameBlockCatalog.Glass), "glass is not fluid");
        Require(BasegameBlockCatalog.IsFluidSource(BasegameBlockCatalog.WaterSource), "water source is fluid source");
        Require(!BasegameBlockCatalog.IsFluidSource(BasegameBlockCatalog.WaterLevelOne), "flowing water is not fluid source");
        Require(BasegameBlockCatalog.IsFluidSource(BasegameBlockCatalog.LavaSource), "lava source is fluid source");
        Require(!BasegameBlockCatalog.IsFluidSource(BasegameBlockCatalog.LavaLevelOne), "flowing lava is not fluid source");

        Require(BasegameBlockCatalog.FluidLevel(BasegameBlockCatalog.WaterSource) == 0, "water source level");
        Require(BasegameBlockCatalog.FluidLevel(BasegameBlockCatalog.WaterLevelOne) == 1, "water level one");
        Require(BasegameBlockCatalog.FluidLevel(BasegameBlockCatalog.WaterLevelSeven) == 7, "water level seven");
        Require(BasegameBlockCatalog.FluidLevel(BasegameBlockCatalog.LavaSource) == 0, "lava source level");
        Require(BasegameBlockCatalog.FluidLevel(BasegameBlockCatalog.LavaLevelOne) == 1, "lava level one");
        Require(BasegameBlockCatalog.FluidLevel(BasegameBlockCatalog.LavaLevelSeven) == 7, "lava level seven");
        Require(BasegameBlockCatalog.FluidLevel(BlockId.Air) == -1, "air has no fluid level");

        Require(BasegameBlockCatalog.MakeWater(-1) == BasegameBlockCatalog.WaterSource, "make water clamps low");
        Require(BasegameBlockCatalog.MakeWater(0) == BasegameBlockCatalog.WaterSource, "make water source");
        Require(BasegameBlockCatalog.MakeWater(1) == BasegameBlockCatalog.WaterLevelOne, "make water level one");
        Require(BasegameBlockCatalog.MakeWater(7) == BasegameBlockCatalog.WaterLevelSeven, "make water level seven");
        Require(BasegameBlockCatalog.MakeWater(99) == BasegameBlockCatalog.WaterLevelSeven, "make water clamps high");
        Require(BasegameBlockCatalog.MakeLava(-1) == BasegameBlockCatalog.LavaSource, "make lava clamps low");
        Require(BasegameBlockCatalog.MakeLava(0) == BasegameBlockCatalog.LavaSource, "make lava source");
        Require(BasegameBlockCatalog.MakeLava(1) == BasegameBlockCatalog.LavaLevelOne, "make lava level one");
        Require(BasegameBlockCatalog.MakeLava(7) == BasegameBlockCatalog.LavaLevelSeven, "make lava level seven");
        Require(BasegameBlockCatalog.MakeLava(99) == BasegameBlockCatalog.LavaLevelSeven, "make lava clamps high");
        var generatedWater = BasegameBlockCatalog.MakeFluid(BasegameFluidKind.Water, 3);
        Require(BasegameBlockCatalog.FluidKind(generatedWater) == BasegameFluidKind.Water, "make generic water kind");
        Require(BasegameBlockCatalog.FluidLevel(generatedWater) == 3, "make generic water level");

        var generatedLava = BasegameBlockCatalog.MakeFluid(BasegameFluidKind.Lava, 3);
        Require(BasegameBlockCatalog.FluidKind(generatedLava) == BasegameFluidKind.Lava, "make generic lava kind");
        Require(BasegameBlockCatalog.FluidLevel(generatedLava) == 3, "make generic lava level");
        Require(BasegameBlockCatalog.MakeFluid(BasegameFluidKind.None, 3) == BlockId.Air, "make no fluid");
    }

    private static void Require(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException(name);
        }
    }
}
