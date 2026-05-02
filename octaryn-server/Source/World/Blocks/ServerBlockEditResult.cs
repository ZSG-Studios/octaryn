using Octaryn.Shared.World;

namespace Octaryn.Server.World.Blocks;

internal readonly record struct ServerBlockEditResult(bool Applied, bool Changed, IReadOnlyList<BlockEdit> Changes)
{
    public static ServerBlockEditResult Unchanged => new(Applied: true, Changed: false, Changes: []);

    public static ServerBlockEditResult ChangedEdit(BlockEdit edit)
    {
        return new ServerBlockEditResult(Applied: true, Changed: true, Changes: [edit]);
    }
}
