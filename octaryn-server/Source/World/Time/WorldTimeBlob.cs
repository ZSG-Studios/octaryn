namespace Octaryn.Server.World.Time;

internal readonly record struct WorldTimeBlob(
    uint Version,
    ulong DayIndex,
    double SecondsOfDay)
{
    public const uint CurrentVersion = 1;
}
