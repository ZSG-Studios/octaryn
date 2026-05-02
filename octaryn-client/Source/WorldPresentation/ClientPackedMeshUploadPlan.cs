namespace Octaryn.Client.WorldPresentation;

internal sealed class ClientPackedMeshUploadPlan
{
    private const int PackedCubeFaceBytes = sizeof(ulong);
    private const int PackedSpriteVertexBytes = sizeof(uint);

    public ClientPackedMeshUploadPlan(
        int opaqueFaceCount,
        int transparentFaceCount,
        int spriteVertexCount,
        int spriteIndexCount)
    {
        OpaqueFaceCount = opaqueFaceCount;
        TransparentFaceCount = transparentFaceCount;
        SpriteVertexCount = spriteVertexCount;
        SpriteIndexCount = spriteIndexCount;
    }

    public int OpaqueFaceCount { get; }

    public int TransparentFaceCount { get; }

    public int SpriteVertexCount { get; }

    public int SpriteIndexCount { get; }

    public bool ClearsOpaqueFaces => OpaqueFaceCount == 0;

    public bool ClearsTransparentFaces => TransparentFaceCount == 0;

    public bool ClearsSpriteVertices => SpriteVertexCount == 0;

    public bool RequiresUploadSubmit => OpaqueFaceCount > 0 || TransparentFaceCount > 0 || SpriteVertexCount > 0;

    public ulong OpaqueByteCount => (ulong)OpaqueFaceCount * PackedCubeFaceBytes;

    public ulong TransparentByteCount => (ulong)TransparentFaceCount * PackedCubeFaceBytes;

    public ulong SpriteByteCount => (ulong)SpriteVertexCount * PackedSpriteVertexBytes;

    public ClientPackedMeshUploadDescriptor ToDescriptor()
    {
        var flags = 0u;
        if (ClearsOpaqueFaces)
        {
            flags |= ClientPackedMeshUploadDescriptor.ClearOpaqueFacesFlag;
        }

        if (ClearsTransparentFaces)
        {
            flags |= ClientPackedMeshUploadDescriptor.ClearTransparentFacesFlag;
        }

        if (ClearsSpriteVertices)
        {
            flags |= ClientPackedMeshUploadDescriptor.ClearSpriteVerticesFlag;
        }

        return new ClientPackedMeshUploadDescriptor(
            checked((uint)OpaqueFaceCount),
            checked((uint)TransparentFaceCount),
            checked((uint)SpriteVertexCount),
            checked((uint)SpriteIndexCount),
            OpaqueByteCount,
            TransparentByteCount,
            SpriteByteCount,
            flags);
    }
}
