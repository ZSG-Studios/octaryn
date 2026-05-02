using System.Collections.Generic;
using Octaryn.Shared.World;

namespace Octaryn.Basegame.Content.Worldgen;

public sealed class BasegameWorldGenerationRules : IWorldGenerationRules
{
    private static readonly BlockId Dirt = new(2);
    private static readonly BlockId Sand = new(3);
    private static readonly BlockId Snow = new(4);
    private static readonly BlockId Stone = new(5);
    private static readonly BlockId Log = new(6);
    private static readonly BlockId Bush = new(9);
    private static readonly BlockId Bluebell = new(10);
    private static readonly BlockId Gardenia = new(11);
    private static readonly BlockId Rose = new(12);
    private static readonly BlockId Lavender = new(13);
    private static readonly BlockId[] Flowers = [Bluebell, Gardenia, Lavender, Rose];

    public int WaterHeight => 30;

    public TerrainColumnPlan PlanTerrainColumn(TerrainColumnSample sample)
    {
        var height = (float)global::System.Math.Pow(global::System.Math.Max(sample.HeightNoise * 50.0f, 0.0f), 1.3f) + 30.0f;
        height = (float)global::System.Math.Clamp(height, 0.0f, sample.MaxTerrainY);

        var isLowland = false;
        if (height < 40.0f)
        {
            height += sample.LowlandNoise * 12.0f;
            isLowland = true;
        }

        var terrainHeight = (int)global::System.Math.Ceiling(height);
        var materials = ClassifyMaterials(height, sample.BiomeNoise);
        return new TerrainColumnPlan(
            sample.WorldX,
            sample.WorldZ,
            sample.LocalX,
            sample.LocalZ,
            sample.LocalWidth,
            sample.LocalDepth,
            terrainHeight,
            global::System.Math.Max(terrainHeight, WaterHeight),
            materials.SurfaceBlock,
            materials.FillBlock,
            isLowland,
            materials.HasGrassSurface);
    }

    public void AddFeatureBlocks(TerrainColumnPlan column, float plantNoise, ICollection<BlockEdit> blocks)
    {
        if (!column.IsLowland || !column.HasGrassSurface)
        {
            return;
        }

        var plant = plantNoise * 0.5f + 0.5f;
        if (plant > 0.8f &&
            column.LocalX > 2 &&
            column.LocalX < column.LocalWidth - 2 &&
            column.LocalZ > 2 &&
            column.LocalZ < column.LocalDepth - 2)
        {
            AddTreeBlocks(column, plant, blocks);
            return;
        }

        if (plant > 0.55f)
        {
            blocks.Add(new BlockEdit(new BlockPosition(column.WorldX, column.DecorationY + 1, column.WorldZ), Bush));
            return;
        }

        if (plant > 0.52f)
        {
            var flowerIndex = global::System.Math.Max((int)(plant * 1000.0f) % Flowers.Length, 0);
            blocks.Add(new BlockEdit(
                new BlockPosition(column.WorldX, column.DecorationY + 1, column.WorldZ),
                Flowers[flowerIndex]));
        }
    }

    private static TerrainMaterials ClassifyMaterials(float height, float biome)
    {
        if (height + biome < 31.0f)
        {
            return new TerrainMaterials(Sand, Sand, HasGrassSurface: false);
        }

        biome = global::System.Math.Clamp(biome * 8.0f, -5.0f, 5.0f);
        if (height + biome < 61.0f)
        {
            return new TerrainMaterials(new BlockId(1), Dirt, HasGrassSurface: true);
        }

        if (height + biome < 132.0f)
        {
            return new TerrainMaterials(Stone, Stone, HasGrassSurface: false);
        }

        return new TerrainMaterials(Snow, Stone, HasGrassSurface: false);
    }

    private static void AddTreeBlocks(TerrainColumnPlan column, float plant, ICollection<BlockEdit> blocks)
    {
        var logHeight = (int)(3.0f + plant * 2.0f);
        for (var dy = 0; dy < logHeight; dy++)
        {
            blocks.Add(new BlockEdit(
                new BlockPosition(column.WorldX, column.DecorationY + dy + 1, column.WorldZ),
                Log));
        }

        for (var dx = -1; dx <= 1; dx++)
        for (var dz = -1; dz <= 1; dz++)
        for (var dy = 0; dy < 2; dy++)
        {
            if (dx == 0 && dz == 0 && dy == 0)
            {
                continue;
            }

            blocks.Add(new BlockEdit(
                new BlockPosition(column.WorldX + dx, column.DecorationY + logHeight + dy, column.WorldZ + dz),
                new BlockId(7)));
        }
    }

    private readonly record struct TerrainMaterials(BlockId SurfaceBlock, BlockId FillBlock, bool HasGrassSurface);
}
