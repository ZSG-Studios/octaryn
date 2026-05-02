using Octaryn.Shared.World;

namespace Octaryn.Client.WorldPresentation;

internal readonly record struct ClientBlockPresentationUpdate(
    BlockPosition Position,
    BlockId Block,
    ClientPresentationChunkKey Chunk);
