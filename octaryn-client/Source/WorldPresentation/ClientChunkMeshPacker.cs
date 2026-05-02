namespace Octaryn.Client.WorldPresentation;

internal sealed class ClientChunkMeshPacker
{
    private readonly ClientBlockRenderRules _rules;

    public ClientChunkMeshPacker(ClientBlockRenderRules rules)
    {
        _rules = rules;
    }

    public ClientPackedChunkMesh Pack(ClientChunkMeshPlan plan)
    {
        var opaqueFaces = new List<ulong>();
        var transparentFaces = new List<ulong>();
        var spriteVertices = new List<uint>();

        foreach (var face in plan.CubeFaces)
        {
            var packed = ClientPackedCubeFace.Pack(face, _rules);
            if (face.Kind == ClientBlockRenderKind.TransparentCube)
            {
                transparentFaces.Add(packed);
            }
            else
            {
                opaqueFaces.Add(packed);
            }
        }

        foreach (var face in plan.SpriteFaces)
        {
            for (var vertex = 0; vertex < 4; vertex++)
            {
                spriteVertices.Add(ClientPackedSpriteVertex.Pack(face, _rules, vertex));
            }
        }

        return new ClientPackedChunkMesh(opaqueFaces, transparentFaces, spriteVertices, plan.FluidBlocks);
    }
}
