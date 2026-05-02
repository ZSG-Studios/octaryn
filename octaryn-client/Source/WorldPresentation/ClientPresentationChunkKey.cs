using Octaryn.Shared.World;

namespace Octaryn.Client.WorldPresentation;

internal readonly record struct ClientPresentationChunkKey(int X, int Y, int Z)
{
    public const int Width = ChunkConstants.Width;
    public const int Height = ChunkConstants.SectionHeight;
    public const int Depth = ChunkConstants.Depth;
    public const int MinSectionY = ChunkConstants.WorldMinY / Height;
    public const int MaxSectionYExclusive = ChunkConstants.WorldMaxYExclusive / Height;

    public static ClientPresentationChunkKey FromBlock(BlockPosition position)
    {
        return new ClientPresentationChunkKey(
            FloorDivide(position.X, Width),
            FloorDivide(position.Y, Height),
            FloorDivide(position.Z, Depth));
    }

    public static int LocalBlockCoordinate(int value, int size)
    {
        var local = value % size;
        return local < 0 ? local + size : local;
    }

    public static bool ContainsSectionY(int y)
    {
        return y >= MinSectionY && y < MaxSectionYExclusive;
    }

    private static int FloorDivide(int value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }
}
