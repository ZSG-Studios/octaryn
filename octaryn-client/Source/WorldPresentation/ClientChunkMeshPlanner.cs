using Octaryn.Shared.World;

namespace Octaryn.Client.WorldPresentation;

internal sealed class ClientChunkMeshPlanner
{
    private readonly ClientBlockRenderRules _rules;

    public ClientChunkMeshPlanner(ClientBlockRenderRules rules)
    {
        _rules = rules;
    }

    public ClientChunkMeshPlan Build(ClientChunkNeighborhoodSnapshot snapshot)
    {
        var cubeFaces = new List<ClientCubeMeshFace>();
        var spriteFaces = new List<ClientSpriteMeshFace>();
        var fluidBlocks = new List<ClientFluidMeshBlock>();

        for (var x = 0; x < ClientChunkNeighborhoodSnapshot.Width; x++)
        for (var y = 0; y < ClientChunkNeighborhoodSnapshot.Height; y++)
        for (var z = 0; z < ClientChunkNeighborhoodSnapshot.Depth; z++)
        {
            var block = snapshot.LocalBlock(1, 1, x, y, z);
            var properties = _rules.Properties(block);
            switch (properties.Kind)
            {
                case ClientBlockRenderKind.Empty:
                case ClientBlockRenderKind.Hidden:
                    break;
                case ClientBlockRenderKind.Sprite:
                    AppendSpriteFaces(spriteFaces, block, x, y, z);
                    break;
                case ClientBlockRenderKind.Water:
                case ClientBlockRenderKind.Lava:
                    fluidBlocks.Add(new ClientFluidMeshBlock(block, properties.Kind, x, y, z, properties.FluidLevel));
                    break;
                case ClientBlockRenderKind.OpaqueCube:
                case ClientBlockRenderKind.TransparentCube:
                    AppendCubeFaces(cubeFaces, snapshot, block, properties.Kind, x, y, z);
                    break;
            }
        }

        return new ClientChunkMeshPlan(cubeFaces, spriteFaces, fluidBlocks);
    }

    private void AppendSpriteFaces(
        List<ClientSpriteMeshFace> spriteFaces,
        BlockId block,
        int x,
        int y,
        int z)
    {
        foreach (var direction in _rules.SpriteFaceDirections(block))
        {
            spriteFaces.Add(new ClientSpriteMeshFace(block, x, y, z, direction));
        }
    }

    private void AppendCubeFaces(
        List<ClientCubeMeshFace> cubeFaces,
        ClientChunkNeighborhoodSnapshot snapshot,
        BlockId block,
        ClientBlockRenderKind kind,
        int x,
        int y,
        int z)
    {
        foreach (var direction in _rules.VisibleCubeFaces(snapshot, x, y, z))
        {
            cubeFaces.Add(new ClientCubeMeshFace(block, kind, x, y, z, direction));
        }
    }
}
