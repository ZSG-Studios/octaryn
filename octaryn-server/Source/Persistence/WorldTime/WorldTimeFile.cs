using System.Text.Json.Serialization;

namespace Octaryn.Server.Persistence.WorldTime;

internal sealed class WorldTimeFile
{
    [JsonPropertyName("version")]
    public uint Version { get; set; } = 1;

    [JsonPropertyName("day_index")]
    public ulong DayIndex { get; set; }

    [JsonPropertyName("seconds_of_day")]
    public double SecondsOfDay { get; set; }
}
