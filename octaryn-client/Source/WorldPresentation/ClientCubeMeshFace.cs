using Octaryn.Shared.World;

namespace Octaryn.Client.WorldPresentation;

internal readonly record struct ClientCubeMeshFace(
    BlockId Block,
    ClientBlockRenderKind Kind,
    int X,
    int Y,
    int Z,
    Direction Direction);
