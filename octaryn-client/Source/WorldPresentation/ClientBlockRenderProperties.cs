namespace Octaryn.Client.WorldPresentation;

internal readonly record struct ClientBlockRenderProperties(
    ClientBlockRenderKind Kind,
    bool IsOpaque,
    bool HasOcclusion,
    bool IsSprite,
    bool IsFluid,
    int FluidLevel,
    bool RequiresSolidBase)
{
    public static ClientBlockRenderProperties Air { get; } = new(
        ClientBlockRenderKind.Empty,
        IsOpaque: false,
        HasOcclusion: false,
        IsSprite: false,
        IsFluid: false,
        FluidLevel: -1,
        RequiresSolidBase: false);
}
