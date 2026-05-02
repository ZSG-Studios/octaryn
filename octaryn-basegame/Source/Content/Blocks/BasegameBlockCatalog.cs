using Octaryn.Basegame.Content.Fluids;
using Octaryn.Shared.World;

namespace Octaryn.Basegame.Content.Blocks;

public static class BasegameBlockCatalog
{
    private static readonly ushort[] LegacyIds =
    [
        0,
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        8,
        9,
        10,
        11,
        12,
        13,
        14,
        15,
        16,
        17,
        18,
        19,
        20,
        21,
        22,
        23,
        24,
        25,
        26,
        27,
        28,
        29,
        30,
        31,
        32,
        33,
        34,
        35,
        36,
        37,
        38
    ];

    private static readonly ushort[] GrassSupportedLegacyIds =
    [
        9,
        10,
        11,
        12,
        13
    ];

    private static readonly ushort[] PlaceableLegacyIds =
    [
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        9,
        10,
        11,
        12,
        13,
        14,
        22,
        23,
        24,
        25,
        26,
        27,
        28,
        29,
        30,
        31
    ];

    private static readonly ushort[] SolidBaseSupportedLegacyIds =
    [
        22,
        23,
        24,
        25,
        26,
        27,
        28
    ];

    private static readonly ushort[] SolidLegacyIds =
    [
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        29,
        30
    ];

    private static readonly ushort[] TargetableLegacyIds =
    [
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        9,
        10,
        11,
        12,
        13,
        22,
        23,
        24,
        25,
        26,
        27,
        28,
        29,
        30
    ];

    private static readonly ushort[] FluidKindValues =
    [
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        2,
        2,
        2,
        2,
        2,
        2,
        2,
        2
    ];

    private static readonly int[] FluidLevels =
    [
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        0,
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        0,
        1,
        2,
        3,
        4,
        5,
        6,
        7
    ];

    private static readonly ushort[] FluidSourceLegacyIds =
    [
        14,
        31
    ];

    private static readonly int[] SkylightOpacities =
    [
        0,
        15,
        15,
        15,
        15,
        15,
        15,
        1,
        0,
        0,
        0,
        0,
        0,
        0,
        2,
        2,
        2,
        2,
        2,
        2,
        2,
        2,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        15,
        0,
        2,
        2,
        2,
        2,
        2,
        2,
        2,
        2
    ];

    public static BlockId Grass => new(1);

    public static BlockId Leaves => new(7);

    public static BlockId DefaultSelectedBlock => new(25);

    public static BlockId WaterSource => new(14);

    public static BlockId LavaSource => new(31);

    public static int PlaceableCount => PlaceableLegacyIds.Length;

    public static bool IsKnown(BlockId block)
    {
        return block.Value < LegacyIds.Length && LegacyIds[block.Value] == block.Value;
    }

    public static bool IsPlaceable(BlockId block)
    {
        return IndexOf(PlaceableLegacyIds, block.Value) >= 0;
    }

    public static bool IsSolid(BlockId block)
    {
        return IndexOf(SolidLegacyIds, block.Value) >= 0;
    }

    public static bool IsTargetable(BlockId block)
    {
        return IndexOf(TargetableLegacyIds, block.Value) >= 0;
    }

    public static bool IsFluid(BlockId block)
    {
        return FluidKind(block) != BasegameFluidKind.None;
    }

    public static bool IsFluidSource(BlockId block)
    {
        return IndexOf(FluidSourceLegacyIds, block.Value) >= 0;
    }

    public static bool IsWater(BlockId block)
    {
        return FluidKind(block) == BasegameFluidKind.Water;
    }

    public static bool IsLava(BlockId block)
    {
        return FluidKind(block) == BasegameFluidKind.Lava;
    }

    public static BasegameFluidKind FluidKind(BlockId block)
    {
        return block.Value < FluidKindValues.Length
            ? (BasegameFluidKind)FluidKindValues[block.Value]
            : BasegameFluidKind.None;
    }

    public static int FluidLevel(BlockId block)
    {
        return block.Value < FluidLevels.Length ? FluidLevels[block.Value] : -1;
    }

    public static int SkylightOpacity(BlockId block)
    {
        return block.Value < SkylightOpacities.Length ? SkylightOpacities[block.Value] : 0;
    }

    public static BlockId MakeWater(int level)
    {
        return new BlockId(ClampedFluidLegacyId(14, 21, level));
    }

    public static BlockId MakeLava(int level)
    {
        return new BlockId(ClampedFluidLegacyId(31, 38, level));
    }

    public static BlockId MakeFluid(BasegameFluidKind kind, int level)
    {
        return kind switch
        {
            BasegameFluidKind.Water => MakeWater(level),
            BasegameFluidKind.Lava => MakeLava(level),
            _ => BlockId.Air
        };
    }

    public static bool RequiresGrass(BlockId block)
    {
        return IndexOf(GrassSupportedLegacyIds, block.Value) >= 0;
    }

    public static bool RequiresSolidBase(BlockId block)
    {
        return IndexOf(SolidBaseSupportedLegacyIds, block.Value) >= 0;
    }

    public static BlockId PlaceableAt(int index)
    {
        return new BlockId(PlaceableLegacyIds[index]);
    }

    public static int PlaceableIndexOf(BlockId block)
    {
        return IndexOf(PlaceableLegacyIds, block.Value);
    }

    private static ushort ClampedFluidLegacyId(ushort sourceId, ushort maxLevelId, int level)
    {
        if (level <= 0)
        {
            return sourceId;
        }

        if (level >= 7)
        {
            return maxLevelId;
        }

        return (ushort)(sourceId + level);
    }

    private static int IndexOf(ushort[] values, ushort value)
    {
        for (var index = 0; index < values.Length; index++)
        {
            if (values[index] == value)
            {
                return index;
            }
        }

        return -1;
    }
}
