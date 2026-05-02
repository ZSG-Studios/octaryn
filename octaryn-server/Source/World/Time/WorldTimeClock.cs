using Octaryn.Shared.Time;

namespace Octaryn.Server.World.Time;

internal sealed class WorldTimeClock
{
    private WorldTimeConfig _config;
    private ulong _tickId;

    public WorldTimeClock()
        : this(WorldTimeConfig.Default)
    {
    }

    public WorldTimeClock(WorldTimeConfig config)
    {
        Reset(config);
    }

    public ulong DayIndex { get; private set; }

    public double SecondsOfDay { get; private set; }

    public void Reset(WorldTimeConfig? config = null)
    {
        _config = WorldTimeConfig.Sanitize(config);
        _tickId = 0;
        DayIndex = 0;
        SecondsOfDay = _config.StartSecondsOfDay;
    }

    public WorldTime AdvanceFrame(double deltaSeconds)
    {
        var safeDeltaSeconds = double.IsFinite(deltaSeconds) && deltaSeconds > 0.0 ? deltaSeconds : 0.0;
        AdvanceRealSeconds(safeDeltaSeconds);
        var snapshot = Snapshot();
        return new WorldTime(_tickId++, DayIndex, safeDeltaSeconds, snapshot.TotalWorldSeconds);
    }

    public void AdvanceRealSeconds(double realSeconds)
    {
        if (!double.IsFinite(realSeconds) || realSeconds <= 0.0)
        {
            return;
        }

        var dayScale = WorldTimeConfig.WorldSecondsPerDay /
            WorldTimeConfig.ClampRealSecondsPerDay(_config.RealSecondsPerDay);
        var nextSeconds = SecondsOfDay + realSeconds * dayScale;
        while (nextSeconds >= WorldTimeConfig.WorldSecondsPerDay)
        {
            nextSeconds -= WorldTimeConfig.WorldSecondsPerDay;
            DayIndex++;
        }

        SecondsOfDay = nextSeconds;
    }

    public WorldTimeSnapshot Snapshot()
    {
        var secondOfDay = (ulong)Math.Floor(SecondsOfDay);
        var dayFraction = (float)(SecondsOfDay / WorldTimeConfig.WorldSecondsPerDay);
        var startDay = WorldTimeCalendar.DaysFromCivil(
            _config.StartYear,
            (uint)_config.StartMonth,
            (uint)_config.StartDay);
        var date = WorldTimeCalendar.CivilFromDays(startDay + (long)DayIndex);
        return new WorldTimeSnapshot(
            date,
            DayIndex,
            (uint)secondOfDay,
            (uint)((secondOfDay / 3600u) % 24u),
            (uint)((secondOfDay / 60u) % 60u),
            (uint)(secondOfDay % 60u),
            DayIndex * WorldTimeConfig.WorldSecondsPerDay + SecondsOfDay,
            dayFraction);
    }

    public WorldTimeBlob WriteBlob()
    {
        return new WorldTimeBlob(
            WorldTimeBlob.CurrentVersion,
            DayIndex,
            Math.Clamp(SecondsOfDay, 0.0, WorldTimeConfig.WorldSecondsPerDay - 0.001));
    }

    public bool TryReadBlob(WorldTimeConfig config, WorldTimeBlob blob)
    {
        if (blob.Version != WorldTimeBlob.CurrentVersion)
        {
            return false;
        }

        Reset(config);
        DayIndex = blob.DayIndex;
        SecondsOfDay = SanitizeSecondsOfDay(blob.SecondsOfDay, out var dayCarry);
        DayIndex += dayCarry;
        return true;
    }

    private static double SanitizeSecondsOfDay(double secondsOfDay, out ulong dayCarry)
    {
        var sanitized = double.IsFinite(secondsOfDay) ? secondsOfDay : 0.0;
        dayCarry = 0;
        while (sanitized >= WorldTimeConfig.WorldSecondsPerDay)
        {
            sanitized -= WorldTimeConfig.WorldSecondsPerDay;
            dayCarry++;
        }

        while (sanitized < 0.0)
        {
            if (dayCarry == 0)
            {
                sanitized = 0.0;
                break;
            }

            sanitized += WorldTimeConfig.WorldSecondsPerDay;
            dayCarry--;
        }

        return sanitized;
    }
}
