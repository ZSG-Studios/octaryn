using Octaryn.Shared.World;

namespace Octaryn.Client.WorldPresentation;

internal readonly record struct ClientFluidMeshBlock(
    BlockId Block,
    ClientBlockRenderKind Kind,
    int X,
    int Y,
    int Z,
    int Level);
