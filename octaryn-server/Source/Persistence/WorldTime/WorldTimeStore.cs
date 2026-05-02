using System.Text.Json;
using Octaryn.Server.World.Time;

namespace Octaryn.Server.Persistence.WorldTime;

internal static class WorldTimeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static bool TryLoad(string path, out WorldTimeBlob blob)
    {
        blob = default;
        if (!File.Exists(path))
        {
            return false;
        }

        var file = JsonSerializer.Deserialize<WorldTimeFile>(File.ReadAllText(path), JsonOptions);
        if (file is null || file.Version != WorldTimeBlob.CurrentVersion)
        {
            return false;
        }

        blob = new WorldTimeBlob(file.Version, file.DayIndex, file.SecondsOfDay);
        return true;
    }

    public static void Save(string path, WorldTimeBlob blob)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";
        var file = new WorldTimeFile
        {
            Version = blob.Version,
            DayIndex = blob.DayIndex,
            SecondsOfDay = blob.SecondsOfDay
        };
        File.WriteAllText(tempPath, JsonSerializer.Serialize(file, JsonOptions));
        File.Move(tempPath, path, overwrite: true);
    }
}
