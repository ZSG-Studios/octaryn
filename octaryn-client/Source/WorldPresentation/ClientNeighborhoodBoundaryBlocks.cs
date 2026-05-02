using Octaryn.Shared.World;

namespace Octaryn.Client.WorldPresentation;

internal readonly record struct ClientNeighborhoodBoundaryBlocks(BlockId BelowWorldBlock)
{
    public static ClientNeighborhoodBoundaryBlocks Air => new(BlockId.Air);
}
