using Octaryn.Shared.World;

namespace Octaryn.Client.WorldPresentation;

internal readonly record struct ClientSpriteMeshFace(
    BlockId Block,
    int X,
    int Y,
    int Z,
    Direction Direction);
