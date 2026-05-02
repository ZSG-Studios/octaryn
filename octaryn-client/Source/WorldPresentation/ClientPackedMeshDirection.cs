using Octaryn.Shared.World;

namespace Octaryn.Client.WorldPresentation;

internal enum ClientPackedMeshDirection : byte
{
    North = 0,
    South = 1,
    East = 2,
    West = 3,
    Up = 4,
    Down = 5
}

internal static class ClientPackedMeshDirectionMap
{
    public static ClientPackedMeshDirection FromDirection(Direction direction)
    {
        return direction switch
        {
            Direction.PositiveZ => ClientPackedMeshDirection.North,
            Direction.NegativeZ => ClientPackedMeshDirection.South,
            Direction.PositiveX => ClientPackedMeshDirection.East,
            Direction.NegativeX => ClientPackedMeshDirection.West,
            Direction.PositiveY => ClientPackedMeshDirection.Up,
            Direction.NegativeY => ClientPackedMeshDirection.Down,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported mesh direction")
        };
    }

    public static int ToOldDirectionIndex(Direction direction)
    {
        return (int)FromDirection(direction);
    }
}
