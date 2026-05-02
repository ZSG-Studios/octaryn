namespace Octaryn.Server.World.Time;

internal readonly record struct WorldTimeConfig(
    double RealSecondsPerDay,
    int StartYear,
    int StartMonth,
    int StartDay,
    double StartSecondsOfDay)
{
    public const double WorldSecondsPerDay = 24.0 * 60.0 * 60.0;

    public static WorldTimeConfig Default { get; } = new(
        1800.0,
        1000,
        1,
        1,
        12.0 * 60.0 * 60.0);

    public static WorldTimeConfig Sanitize(WorldTimeConfig? config)
    {
        var value = config ?? Default;
        var startMonth = value.StartMonth is >= 1 and <= 12 ? value.StartMonth : Default.StartMonth;
        var maxStartDay = WorldTimeCalendar.DaysInMonth(value.StartYear, startMonth);
        return new WorldTimeConfig(
            ClampRealSecondsPerDay(value.RealSecondsPerDay),
            value.StartYear,
            startMonth,
            Math.Clamp(value.StartDay, 1, maxStartDay),
            SanitizeStartSecondsOfDay(value.StartSecondsOfDay));
    }

    public static double ClampRealSecondsPerDay(double value)
    {
        return value > 0.0 && double.IsFinite(value) ? value : Default.RealSecondsPerDay;
    }

    public static double SanitizeStartSecondsOfDay(double value)
    {
        if (!double.IsFinite(value))
        {
            return Default.StartSecondsOfDay;
        }

        while (value < 0.0)
        {
            value += WorldSecondsPerDay;
        }

        while (value >= WorldSecondsPerDay)
        {
            value -= WorldSecondsPerDay;
        }

        return value;
    }
}
