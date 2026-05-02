using Octaryn.Server.Persistence.WorldTime;
using Octaryn.Server.World.Time;

return WorldTimeProbe.Run();

internal static class WorldTimeProbe
{
    public static int Run()
    {
        ValidateDefaultSnapshot();
        ValidateAdvanceAndDateCarry();
        ValidateCalendar();
        ValidateBlobRead();
        ValidateStoreRoundTrip();
        return 0;
    }

    private static void ValidateDefaultSnapshot()
    {
        var clock = new WorldTimeClock();
        var snapshot = clock.Snapshot();
        Require(snapshot.Date.Year == 1000, "default year");
        Require(snapshot.Date.Month == 1, "default month");
        Require(snapshot.Date.Day == 1, "default day");
        Require(snapshot.DayIndex == 0, "default day index");
        Require(snapshot.SecondOfDay == 43200, "default second of day");
        Require(snapshot.Hour == 12, "default hour");
        Require(Math.Abs(snapshot.DayFraction - 0.5f) < 0.0001f, "default day fraction");
    }

    private static void ValidateAdvanceAndDateCarry()
    {
        var clock = new WorldTimeClock();
        var worldTime = clock.AdvanceFrame(900.0);
        var snapshot = clock.Snapshot();
        Require(worldTime.TickId == 0, "first world-time tick id");
        Require(worldTime.DayIndex == 1, "world-time day carry");
        Require(Math.Abs(worldTime.DeltaSeconds - 900.0) < 0.0001, "world-time delta");
        Require(snapshot.Date.Year == 1000, "advanced year");
        Require(snapshot.Date.Month == 1, "advanced month");
        Require(snapshot.Date.Day == 2, "advanced day");
        Require(snapshot.SecondOfDay == 0, "advanced second of day");
        Require(snapshot.Hour == 0, "advanced hour");
        Require(Math.Abs(snapshot.TotalWorldSeconds - 86400.0) < 0.0001, "advanced total world seconds");
    }

    private static void ValidateCalendar()
    {
        Require(WorldTimeCalendar.IsLeapYear(2000), "leap year 2000");
        Require(!WorldTimeCalendar.IsLeapYear(1900), "leap year 1900");
        Require(WorldTimeCalendar.DaysInMonth(2000, 2) == 29, "leap February");
        Require(WorldTimeCalendar.DaysInMonth(1900, 2) == 28, "non-leap February");
        Require(WorldTimeCalendar.DaysInMonth(1000, 13) == 31, "invalid month fallback");
    }

    private static void ValidateBlobRead()
    {
        var clock = new WorldTimeClock();
        var loaded = clock.TryReadBlob(
            WorldTimeConfig.Default,
            new WorldTimeBlob(WorldTimeBlob.CurrentVersion, 2, WorldTimeConfig.WorldSecondsPerDay * 2.0 + 12.5));
        Require(loaded, "blob load");
        Require(clock.DayIndex == 4, "blob day carry");
        Require(Math.Abs(clock.SecondsOfDay - 12.5) < 0.0001, "blob seconds");

        loaded = clock.TryReadBlob(WorldTimeConfig.Default, new WorldTimeBlob(99, 0, 0.0));
        Require(!loaded, "reject unknown blob version");
    }

    private static void ValidateStoreRoundTrip()
    {
        var root = Environment.GetEnvironmentVariable("OCTARYN_WORLD_TIME_PROBE_DIR");
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(Path.GetTempPath(), "octaryn-world-time-probe");
        }

        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "world_time.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var expected = new WorldTimeBlob(WorldTimeBlob.CurrentVersion, 7, 123.25);
        WorldTimeStore.Save(path, expected);
        Require(WorldTimeStore.TryLoad(path, out var actual), "world-time store load");
        Require(actual == expected, "world-time store round trip");
        Require(File.ReadAllText(path).Contains("\"seconds_of_day\"", StringComparison.Ordinal), "world-time JSON shape");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"World-time probe failed: {label}.");
        }
    }
}
