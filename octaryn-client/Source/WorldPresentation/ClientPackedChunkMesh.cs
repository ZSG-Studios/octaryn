namespace Octaryn.Client.WorldPresentation;

internal sealed class ClientPackedChunkMesh
{
    public ClientPackedChunkMesh(
        IReadOnlyList<ulong> opaqueCubeFaces,
        IReadOnlyList<ulong> transparentCubeFaces,
        IReadOnlyList<uint> spriteVertices,
        IReadOnlyList<ClientFluidMeshBlock> fluidBlocks)
    {
        OpaqueCubeFaces = opaqueCubeFaces;
        TransparentCubeFaces = transparentCubeFaces;
        SpriteVertices = spriteVertices;
        FluidBlocks = fluidBlocks;
    }

    public IReadOnlyList<ulong> OpaqueCubeFaces { get; }

    public IReadOnlyList<ulong> TransparentCubeFaces { get; }

    public IReadOnlyList<uint> SpriteVertices { get; }

    public IReadOnlyList<ClientFluidMeshBlock> FluidBlocks { get; }
}
