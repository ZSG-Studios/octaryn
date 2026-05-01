namespace Octaryn.Basegame.Gameplay.Rules;

public readonly record struct BasegameFrameState(
    double DeltaSeconds,
    ulong FrameIndex);
