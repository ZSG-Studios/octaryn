namespace Octaryn.Shared.GameModules;

public readonly record struct ModuleFrameContext(
    double DeltaSeconds,
    ulong FrameIndex);
