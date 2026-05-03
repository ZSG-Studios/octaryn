using Octaryn.Basegame.Content.Worldgen;
using Octaryn.Basegame.Module;
using Octaryn.Server;
using Octaryn.Server.World.Generation;
using Octaryn.Shared.ApiExposure;
using Octaryn.Shared.GameModules;
using Octaryn.Shared.World;

return ServerWorldGenerationProbe.Run();

internal static class ServerWorldGenerationProbe
{
    public static int Run()
    {
        ValidateBasegameRules();
        ValidateServerGeneration();
        ValidateActivatorGenerationPath();
        ValidateManifestCapabilities();
        return 0;
    }

    private static void ValidateBasegameRules()
    {
        var rules = new BasegameWorldGenerationRules();
        Require(rules.WaterHeight == 30, "water height matches old worldgen");

        var sand = rules.PlanTerrainColumn(Sample(0, 0, 0.0f, -1.0f, -1.0f));
        Require(sand.TerrainHeight == 18, "lowland noise adjusts old low terrain");
        Require(sand.SurfaceBlock.Value == 3, "low terrain uses sand surface");
        Require(sand.FillBlock.Value == 3, "low terrain uses sand fill");

        var grass = rules.PlanTerrainColumn(Sample(1, 0, 0.3f, 0.0f, -1.0f));
        Require(grass.SurfaceBlock.Value == 1, "mid lowland terrain uses grass surface");
        Require(grass.FillBlock.Value == 2, "mid lowland terrain uses dirt fill");
        Require(grass.HasGrassSurface, "grass terrain accepts flora");

        var stone = rules.PlanTerrainColumn(Sample(2, 0, 0.7f, 0.0f, 0.0f));
        Require(stone.SurfaceBlock.Value == 5, "high terrain uses stone surface");
        Require(stone.FillBlock.Value == 5, "high terrain uses stone fill");

        var snow = rules.PlanTerrainColumn(Sample(3, 0, 3.0f, 0.0f, 0.0f));
        Require(snow.SurfaceBlock.Value == 4, "peak terrain uses snow surface");
        Require(snow.FillBlock.Value == 5, "peak terrain uses stone fill");

        var featureColumn = rules.PlanTerrainColumn(Sample(4, 0, 0.0f, 0.0f, 2.0f));
        var featureBlocks = new List<BlockEdit>();
        rules.AddFeatureBlocks(featureColumn, 0.05f, featureBlocks);
        Require(featureBlocks.Count == 1 && featureBlocks[0].Block.Value == 11, "flower threshold uses old flower selection order");

        featureBlocks.Clear();
        rules.AddFeatureBlocks(featureColumn, 0.2f, featureBlocks);
        Require(featureBlocks.Count == 1 && featureBlocks[0].Block.Value == 9, "bush threshold emits bush");

        featureBlocks.Clear();
        var treeColumn = featureColumn with { LocalX = 3, LocalZ = 3, DecorationY = 40 };
        rules.AddFeatureBlocks(treeColumn, 0.8f, featureBlocks);
        Require(featureBlocks.Count == 21, "tree threshold emits trunk and leaves");
        Require(featureBlocks.Count(block => block.Block.Value == 6) == 4, "tree trunk height follows old rule");
        Require(featureBlocks.Count(block => block.Block.Value == 7) == 17, "tree leaves follow old canopy rule");
    }

    private static void ValidateServerGeneration()
    {
        var generator = new ServerTerrainGenerator(new BasegameWorldGenerationRules());
        var blocks = generator.GenerateChunkColumn(0, 0);

        Require(blocks.Count > ChunkConstants.Width * ChunkConstants.Depth, "generation emits terrain blocks");
        Require(blocks.All(block => block.Position.Y >= ChunkConstants.WorldMinY), "generated blocks stay above min y");
        Require(blocks.All(block => block.Position.Y < ChunkConstants.WorldMaxYExclusive), "generated blocks stay below max y");
        Require(blocks.Any(block => block.Position.Y == ChunkConstants.WorldMinY), "generation fills centered world floor");
        Require(blocks.Any(block => block.Position.Y < 0), "generation fills below origin in centered world");
        Require(blocks.Any(block => block.Block.Value is 1 or 3 or 4 or 5), "generation emits terrain surface blocks");
        var waterBlocks = new ServerTerrainGenerator(new FixedLowlandRules()).GenerateChunkColumn(0, 0);
        Require(waterBlocks.Any(block => block.Block.Value == 14), "generation emits water where terrain is below water height");

        var repeated = generator.GenerateChunkColumn(0, 0);
        Require(blocks.SequenceEqual(repeated), "generation is deterministic for the same chunk column");

        var neighbor = generator.GenerateChunkColumn(ChunkConstants.Width, 0);
        Require(neighbor.Any(block => block.Position.X >= ChunkConstants.Width), "neighbor origin maps to world x");
        Require(!blocks.SequenceEqual(neighbor), "neighbor chunk has distinct terrain");
    }

    private static void ValidateActivatorGenerationPath()
    {
        using var activator = new ServerModuleActivator(new BasegameModuleRegistration());
        var blocks = activator.GenerateTerrainChunkColumn(0, 0);
        Require(blocks.Count > 0, "server activator exposes generation for modules with worldgen rules");
    }

    private static void ValidateManifestCapabilities()
    {
        var manifest = new BasegameModuleRegistration().Manifest;
        Require(manifest.RequiredCapabilities.Contains(ModuleCapabilityIds.WorldgenBiomes, StringComparer.Ordinal), "manifest declares biome capability");
        Require(manifest.RequiredCapabilities.Contains(ModuleCapabilityIds.WorldgenFeatures, StringComparer.Ordinal), "manifest declares feature capability");
        Require(manifest.RequiredCapabilities.Contains(ModuleCapabilityIds.WorldgenNoise, StringComparer.Ordinal), "manifest declares noise capability");
        Require(GameModuleValidator.Validate(manifest).IsValid, "basegame manifest validates with worldgen capabilities");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static TerrainColumnSample Sample(
        int worldX,
        int worldZ,
        float heightNoise,
        float lowlandNoise,
        float biomeNoise)
    {
        return new TerrainColumnSample(
            worldX,
            worldZ,
            worldX,
            worldZ,
            ChunkConstants.Width,
            ChunkConstants.Depth,
            ChunkConstants.WorldMaxYExclusive - 1,
            heightNoise,
            lowlandNoise,
            biomeNoise);
    }

    private sealed class FixedLowlandRules : IWorldGenerationRules
    {
        public int WaterHeight => 30;

        public TerrainColumnPlan PlanTerrainColumn(TerrainColumnSample sample)
        {
            return new TerrainColumnPlan(
                sample.WorldX,
                sample.WorldZ,
                sample.LocalX,
                sample.LocalZ,
                sample.LocalWidth,
                sample.LocalDepth,
                TerrainHeight: 18,
                DecorationY: 30,
                SurfaceBlock: new BlockId(3),
                FillBlock: new BlockId(3),
                IsLowland: true,
                HasGrassSurface: false);
        }

        public void AddFeatureBlocks(TerrainColumnPlan column, float plantNoise, ICollection<BlockEdit> blocks)
        {
            _ = column;
            _ = plantNoise;
            _ = blocks;
        }
    }
}
