using Octaryn.Shared.World;

namespace Octaryn.Client.WorldPresentation;

internal sealed class ClientBlockPresentationStore
{
    private readonly Dictionary<BlockPosition, BlockId> _blocks = new();
    private readonly Queue<ClientBlockPresentationUpdate> _updates = new();
    private readonly HashSet<ClientPresentationChunkKey> _dirtyChunks = new();

    public bool Apply(BlockPosition position, BlockId block)
    {
        var current = GetBlock(position);
        if (current == block)
        {
            return false;
        }

        if (block == BlockId.Air)
        {
            _blocks.Remove(position);
        }
        else
        {
            _blocks[position] = block;
        }

        var ownerChunk = ClientPresentationChunkKey.FromBlock(position.X, position.Z);
        MarkDirtyChunks(position, ownerChunk);
        _updates.Enqueue(new ClientBlockPresentationUpdate(position, block, ownerChunk));
        return true;
    }

    public BlockId GetBlock(BlockPosition position)
    {
        return _blocks.GetValueOrDefault(position, BlockId.Air);
    }

    public int PendingUpdateCount => _updates.Count;

    public int DirtyChunkCount => _dirtyChunks.Count;

    public bool TryDequeueUpdate(out ClientBlockPresentationUpdate update)
    {
        return _updates.TryDequeue(out update);
    }

    public IReadOnlyList<ClientPresentationChunkKey> DrainDirtyChunks()
    {
        var chunks = _dirtyChunks.ToArray();
        _dirtyChunks.Clear();
        return chunks;
    }

    public ClientChunkNeighborhoodSnapshot CaptureNeighborhood(
        ClientPresentationChunkKey center,
        ClientNeighborhoodBoundaryBlocks boundaries)
    {
        return ClientChunkNeighborhoodSnapshot.Capture(center, boundaries, _blocks);
    }

    private void MarkDirtyChunks(BlockPosition position, ClientPresentationChunkKey ownerChunk)
    {
        _dirtyChunks.Add(ownerChunk);

        var localX = ClientPresentationChunkKey.LocalBlockCoordinate(position.X);
        if (localX == 0)
        {
            _dirtyChunks.Add(new ClientPresentationChunkKey(ownerChunk.X - 1, ownerChunk.Z));
        }
        else if (localX == ClientPresentationChunkKey.Width - 1)
        {
            _dirtyChunks.Add(new ClientPresentationChunkKey(ownerChunk.X + 1, ownerChunk.Z));
        }

        var localZ = ClientPresentationChunkKey.LocalBlockCoordinate(position.Z);
        if (localZ == 0)
        {
            _dirtyChunks.Add(new ClientPresentationChunkKey(ownerChunk.X, ownerChunk.Z - 1));
        }
        else if (localZ == ClientPresentationChunkKey.Width - 1)
        {
            _dirtyChunks.Add(new ClientPresentationChunkKey(ownerChunk.X, ownerChunk.Z + 1));
        }
    }
}
