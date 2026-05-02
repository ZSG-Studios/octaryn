namespace Octaryn.Shared.World;

public interface IWorldGenerationRules
{
    int WaterHeight { get; }

    TerrainColumnPlan PlanTerrainColumn(TerrainColumnSample sample);

    void AddFeatureBlocks(TerrainColumnPlan column, float plantNoise, ICollection<BlockEdit> blocks);
}
