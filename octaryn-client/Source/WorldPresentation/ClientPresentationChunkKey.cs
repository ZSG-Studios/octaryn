namespace Octaryn.Client.WorldPresentation;

internal readonly record struct ClientPresentationChunkKey(int X, int Z)
{
    public const int Width = 32;

    public static ClientPresentationChunkKey FromBlock(int x, int z)
    {
        return new ClientPresentationChunkKey(FloorDivide(x, Width), FloorDivide(z, Width));
    }

    public static int LocalBlockCoordinate(int value)
    {
        var local = value % Width;
        return local < 0 ? local + Width : local;
    }

    private static int FloorDivide(int value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }
}
