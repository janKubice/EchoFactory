using System.Text.Json;
using System.Text.Json.Serialization;

namespace EchoFactory.Content;

/// <summary>A player's best result on a level (two leaderboard axes + best star rating).</summary>
public sealed class LevelRecord
{
    public int? BestTicks { get; set; }

    public int? BestFootprint { get; set; }

    public int BestStars { get; set; }
}

/// <summary>What changed when a score was submitted.</summary>
public readonly record struct SubmitOutcome(bool NewTickRecord, bool NewFootprintRecord, LevelRecord Record);

/// <summary>
/// Local leaderboard: best ticks and best footprint per level (the two axes from
/// meta-services.md). Scores come from the deterministic engine, so they are trustworthy.
/// </summary>
public sealed class Leaderboard
{
    public Dictionary<string, LevelRecord> Records { get; set; } = new(StringComparer.Ordinal);

    public LevelRecord? Get(string levelId) => Records.GetValueOrDefault(levelId);

    public SubmitOutcome Submit(string levelId, int ticks, int footprint, int stars)
    {
        if (!Records.TryGetValue(levelId, out var record))
        {
            Records[levelId] = record = new LevelRecord();
        }

        bool newTicks = record.BestTicks is null || ticks < record.BestTicks;
        bool newFootprint = record.BestFootprint is null || footprint < record.BestFootprint;

        if (newTicks)
        {
            record.BestTicks = ticks;
        }

        if (newFootprint)
        {
            record.BestFootprint = footprint;
        }

        record.BestStars = Math.Max(record.BestStars, stars);
        return new SubmitOutcome(newTicks, newFootprint, record);
    }
}

/// <summary>JSON persistence for a <see cref="Leaderboard"/> (fails soft).</summary>
public static class LeaderboardStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(Leaderboard leaderboard) => JsonSerializer.Serialize(leaderboard, Options);

    public static Leaderboard Deserialize(string json) =>
        JsonSerializer.Deserialize<Leaderboard>(json, Options) ?? new Leaderboard();

    public static Leaderboard Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return Deserialize(File.ReadAllText(path));
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // ignore — start fresh
        }

        return new Leaderboard();
    }

    public static void Save(string path, Leaderboard leaderboard)
    {
        try
        {
            File.WriteAllText(path, Serialize(leaderboard));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // best-effort
        }
    }
}
