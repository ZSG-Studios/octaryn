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
        Require(BlockSupportRules.CanStaySupported(new BlockId(1), aboveGround, BlockId.Air), "grass does not need base support");
        Require(!BlockSupportRules.CanStaySupported(new BlockId(1), bottomLayer, new BlockId(1)), "non-air blocks require y above zero");

        Require(BlockSupportRules.RequiresGrass(new BlockId(9)), "bush requires grass");
        Require(BlockSupportRules.RequiresGrass(new BlockId(13)), "lavender requires grass");
        Require(!BlockSupportRules.RequiresGrass(new BlockId(22)), "torch does not require grass");
        Require(BlockSupportRules.CanStaySupported(new BlockId(9), aboveGround, new BlockId(1)), "bush accepts grass base");
        Require(!BlockSupportRules.CanStaySupported(new BlockId(9), aboveGround, new BlockId(2)), "bush rejects dirt base");

        Require(BlockSupportRules.RequiresSolidBase(new BlockId(22)), "red torch requires solid base");
        Require(BlockSupportRules.RequiresSolidBase(new BlockId(28)), "white torch requires solid base");
        Require(!BlockSupportRules.RequiresSolidBase(new BlockId(9)), "bush does not require solid base");
        Require(BlockSupportRules.CanStaySupported(new BlockId(25), aboveGround, new BlockId(30)), "torch accepts glass base");
        Require(!BlockSupportRules.CanStaySupported(new BlockId(25), aboveGround, new BlockId(14)), "torch rejects water base");

        Require(BlockSupportRules.IsSolid(new BlockId(1)), "grass is solid");
        Require(BlockSupportRules.IsSolid(new BlockId(7)), "leaves are solid");
        Require(BlockSupportRules.IsSolid(new BlockId(29)), "planks are solid");
        Require(BlockSupportRules.IsSolid(new BlockId(30)), "glass is solid");
        Require(!BlockSupportRules.IsSolid(new BlockId(8)), "cloud is not solid");
        Require(!BlockSupportRules.IsSolid(new BlockId(14)), "water is not solid");
        Require(!BlockSupportRules.IsSolid(new BlockId(22)), "torch is not solid");

        var authorityRules = new BasegameBlockAuthorityRules();
        Require(authorityRules.CanApplyEdit(new BlockEdit(new BlockPosition(3, 4, 5), new BlockId(5)), BlockId.Air), "authority accepts unsupported-independent edit");
        Require(!authorityRules.CanApplyEdit(new BlockEdit(new BlockPosition(3, 4, 5), new BlockId(9)), new BlockId(2)), "authority rejects unsupported grass block edit");
        Require(authorityRules.CanStaySupported(new BlockId(22), aboveGround, new BlockId(30)), "authority accepts supported solid-base block");
        Require(authorityRules.IsClientPlaceable(new BlockId(31)), "authority accepts client-placeable lava source");
        Require(!authorityRules.IsClientPlaceable(new BlockId(32)), "authority rejects non-placeable lava flow");
        ValidateReplacementRules();
        ValidateSkylightOpacity();
        ValidateFluids();
        return 0;
    }

    private static void ValidateReplacementRules()
    {
        Require(BlockReplacementRules.CanBeReplacedByFluid(new BlockId(7)), "fluid replaces leaves");
        Require(BlockReplacementRules.CanBeReplacedByFluid(new BlockId(9)), "fluid replaces bush");
        Require(BlockReplacementRules.CanBeReplacedByFluid(new BlockId(13)), "fluid replaces lavender");
        Require(!BlockReplacementRules.CanBeReplacedByFluid(BlockId.Air), "fluid replacement rule does not include air");
        Require(!BlockReplacementRules.CanBeReplacedByFluid(new BlockId(1)), "fluid does not replace grass");
        Require(!BlockReplacementRules.CanBeReplacedByFluid(new BlockId(14)), "fluid replacement rule does not include fluids");
        Require(!BlockReplacementRules.CanBeReplacedByFluid(new BlockId(30)), "fluid does not replace glass");
    }

    private static void ValidateSkylightOpacity()
    {
        Require(BasegameBlockCatalog.SkylightOpacity(BlockId.Air) == 0, "air has no skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(new BlockId(7)) == 1, "leaves have low skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(new BlockId(14)) == 2, "water source has fluid skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(new BlockId(21)) == 2, "flowing water has fluid skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(new BlockId(31)) == 2, "lava source has fluid skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(new BlockId(38)) == 2, "flowing lava has fluid skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(new BlockId(30)) == 0, "glass has no skylight opacity");
        Require(BasegameBlockCatalog.SkylightOpacity(new BlockId(5)) == 15, "stone blocks skylight");
        Require(BasegameBlockCatalog.SkylightOpacity(new BlockId(65535)) == 0, "unknown block has no skylight opacity");
    }

    private static void ValidateFluids()
    {
        Require(BasegameBlockCatalog.FluidKind(new BlockId(14)) == BasegameFluidKind.Water, "water fluid kind");
        Require(BasegameBlockCatalog.FluidKind(new BlockId(15)) == BasegameFluidKind.Water, "flowing water fluid kind");
        Require(BasegameBlockCatalog.FluidKind(new BlockId(31)) == BasegameFluidKind.Lava, "lava fluid kind");
        Require(BasegameBlockCatalog.FluidKind(new BlockId(32)) == BasegameFluidKind.Lava, "flowing lava fluid kind");
        Require(BasegameBlockCatalog.FluidKind(new BlockId(1)) == BasegameFluidKind.None, "grass has no fluid kind");

        Require(BasegameBlockCatalog.IsWater(new BlockId(14)), "water source is water");
        Require(BasegameBlockCatalog.IsWater(new BlockId(21)), "water level seven is water");
        Require(!BasegameBlockCatalog.IsWater(new BlockId(31)), "lava is not water");
        Require(BasegameBlockCatalog.IsLava(new BlockId(31)), "lava source is lava");
        Require(BasegameBlockCatalog.IsLava(new BlockId(38)), "lava level seven is lava");
        Require(!BasegameBlockCatalog.IsLava(new BlockId(14)), "water is not lava");

        Require(BasegameBlockCatalog.IsFluid(new BlockId(14)), "water source is fluid");
        Require(BasegameBlockCatalog.IsFluid(new BlockId(38)), "lava level seven is fluid");
        Require(!BasegameBlockCatalog.IsFluid(new BlockId(30)), "glass is not fluid");
        Require(BasegameBlockCatalog.IsFluidSource(new BlockId(14)), "water source is fluid source");
        Require(!BasegameBlockCatalog.IsFluidSource(new BlockId(15)), "flowing water is not fluid source");
        Require(BasegameBlockCatalog.IsFluidSource(new BlockId(31)), "lava source is fluid source");
        Require(!BasegameBlockCatalog.IsFluidSource(new BlockId(32)), "flowing lava is not fluid source");

        Require(BasegameBlockCatalog.FluidLevel(new BlockId(14)) == 0, "water source level");
        Require(BasegameBlockCatalog.FluidLevel(new BlockId(15)) == 1, "water level one");
        Require(BasegameBlockCatalog.FluidLevel(new BlockId(21)) == 7, "water level seven");
        Require(BasegameBlockCatalog.FluidLevel(new BlockId(31)) == 0, "lava source level");
        Require(BasegameBlockCatalog.FluidLevel(new BlockId(32)) == 1, "lava level one");
        Require(BasegameBlockCatalog.FluidLevel(new BlockId(38)) == 7, "lava level seven");
        Require(BasegameBlockCatalog.FluidLevel(BlockId.Air) == -1, "air has no fluid level");

        Require(BasegameBlockCatalog.MakeWater(-1).Value == 14, "make water clamps low");
        Require(BasegameBlockCatalog.MakeWater(0).Value == 14, "make water source");
        Require(BasegameBlockCatalog.MakeWater(1).Value == 15, "make water level one");
        Require(BasegameBlockCatalog.MakeWater(7).Value == 21, "make water level seven");
        Require(BasegameBlockCatalog.MakeWater(99).Value == 21, "make water clamps high");
        Require(BasegameBlockCatalog.MakeLava(-1).Value == 31, "make lava clamps low");
        Require(BasegameBlockCatalog.MakeLava(0).Value == 31, "make lava source");
        Require(BasegameBlockCatalog.MakeLava(1).Value == 32, "make lava level one");
        Require(BasegameBlockCatalog.MakeLava(7).Value == 38, "make lava level seven");
        Require(BasegameBlockCatalog.MakeLava(99).Value == 38, "make lava clamps high");
        Require(BasegameBlockCatalog.MakeFluid(BasegameFluidKind.Water, 3).Value == 17, "make generic water");
        Require(BasegameBlockCatalog.MakeFluid(BasegameFluidKind.Lava, 3).Value == 34, "make generic lava");
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
