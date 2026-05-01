namespace Octaryn.Shared.GameModules;

public readonly record struct ModuleCommandRequest(
    string RequestKind,
    ulong RequestId)
{
    public static ModuleCommandRequest Create(string requestKind, ulong requestId = 0)
    {
        return new ModuleCommandRequest(requestKind, requestId);
    }
}
