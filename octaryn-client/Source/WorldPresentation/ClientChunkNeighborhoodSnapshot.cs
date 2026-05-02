using Octaryn.Shared.World;

namespace Octaryn.Client.WorldPresentation;

internal sealed class ClientChunkNeighborhoodSnapshot
{
    public const int Width = ClientPresentationChunkKey.Width;
    public const int Height = 256;
    private const int ChunkCount = 3;
    private const int BlocksPerChunk = Width * Height * Width;

    private readonly ClientNeighborhoodBoundaryBlocks _boundaries;
    private readonly BlockId[] _blocks;

    private ClientChunkNeighborhoodSnapshot(
        ClientPresentationChunkKey center,
        ClientNeighborhoodBoundaryBlocks boundaries,
        BlockId[] blocks)
    {
        Center = center;
        _boundaries = boundaries;
        _blocks = blocks;
    }

    public ClientPresentationChunkKey Center { get; }

    public static ClientChunkNeighborhoodSnapshot Capture(
        ClientPresentationChunkKey center,
        ClientNeighborhoodBoundaryBlocks boundaries,
        IReadOnlyDictionary<BlockPosition, BlockId> source)
    {
        var blocks = new BlockId[BlocksPerChunk * ChunkCount * ChunkCount];
        foreach (var (position, block) in source)
        {
            var chunk = ClientPresentationChunkKey.FromBlock(position.X, position.Z);
            var chunkX = chunk.X - center.X + 1;
            var chunkZ = chunk.Z - center.Z + 1;
            if (chunkX is < 0 or > 2 ||
                chunkZ is < 0 or > 2 ||
                position.Y is < 0 or >= Height)
            {
                continue;
            }

            var localX = ClientPresentationChunkKey.LocalBlockCoordinate(position.X);
            var localZ = ClientPresentationChunkKey.LocalBlockCoordinate(position.Z);
            blocks[SnapshotIndex(chunkX, chunkZ, localX, position.Y, localZ)] = block;
        }

        return new ClientChunkNeighborhoodSnapshot(center, boundaries, blocks);
    }

    public BlockId LocalBlock(int chunkX, int chunkZ, int blockX, int blockY, int blockZ)
    {
        if (chunkX < 0 || chunkX > 2 || chunkZ < 0 || chunkZ > 2)
        {
            return BlockId.Air;
        }

        return WorldBlock(
            new ClientPresentationChunkKey(Center.X + chunkX - 1, Center.Z + chunkZ - 1),
            blockX,
            blockY,
            blockZ);
    }

    public BlockId NeighborhoodBlock(int blockX, int blockY, int blockZ, int dx, int dy, int dz)
    {
        var x = blockX + dx;
        var y = blockY + dy;
        var z = blockZ + dz;
        var chunkX = 1;
        var chunkZ = 1;

        while (x < 0)
        {
            x += ClientPresentationChunkKey.Width;
            chunkX--;
        }

        while (x >= ClientPresentationChunkKey.Width)
        {
            x -= ClientPresentationChunkKey.Width;
            chunkX++;
        }

        while (z < 0)
        {
            z += ClientPresentationChunkKey.Width;
            chunkZ--;
        }

        while (z >= ClientPresentationChunkKey.Width)
        {
            z -= ClientPresentationChunkKey.Width;
            chunkZ++;
        }

        return LocalBlock(chunkX, chunkZ, x, y, z);
    }

    private BlockId WorldBlock(ClientPresentationChunkKey chunk, int localX, int y, int localZ)
    {
        if (y < 0)
        {
            return _boundaries.BelowWorldBlock;
        }

        if (y >= Height)
        {
            return BlockId.Air;
        }

        var chunkX = chunk.X - Center.X + 1;
        var chunkZ = chunk.Z - Center.Z + 1;
        if (chunkX is < 0 or > 2 || chunkZ is < 0 or > 2)
        {
            return BlockId.Air;
        }

        return _blocks[SnapshotIndex(chunkX, chunkZ, localX, y, localZ)];
    }

    private static int SnapshotIndex(int chunkX, int chunkZ, int localX, int y, int localZ)
    {
        return (chunkX * ChunkCount + chunkZ) * BlocksPerChunk +
            (localX * Height + y) * Width +
            localZ;
    }
}
