namespace Octaryn.Shared.Time;

public readonly record struct WorldTime(
    ulong TickId,
    ulong DayIndex,
    double DeltaSeconds,
    double TotalSeconds);
