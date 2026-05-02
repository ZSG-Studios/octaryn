namespace Octaryn.Client.WorldPresentation;

internal sealed class ClientChunkMeshPlan
{
    public ClientChunkMeshPlan(
        IReadOnlyList<ClientCubeMeshFace> cubeFaces,
        IReadOnlyList<ClientSpriteMeshFace> spriteFaces,
        IReadOnlyList<ClientFluidMeshBlock> fluidBlocks)
    {
        CubeFaces = cubeFaces;
        SpriteFaces = spriteFaces;
        FluidBlocks = fluidBlocks;
    }

    public IReadOnlyList<ClientCubeMeshFace> CubeFaces { get; }

    public IReadOnlyList<ClientSpriteMeshFace> SpriteFaces { get; }

    public IReadOnlyList<ClientFluidMeshBlock> FluidBlocks { get; }
}
