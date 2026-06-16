using System.Text.Json;

namespace EchoFactory.Game;

/// <summary>Loads/saves <see cref="GameSettings"/> as JSON next to the executable (fails soft).</summary>
internal static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "echofactory-settings.json");

    public static GameSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return Deserialize(File.ReadAllText(FilePath));
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // ignore — fall back to defaults
        }

        return new GameSettings();
    }

    public static void Save(GameSettings settings)
    {
        try
        {
            File.WriteAllText(FilePath, Serialize(settings));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // ignore — settings are best-effort
        }
    }

    public static string Serialize(GameSettings settings) => JsonSerializer.Serialize(settings, Options);

    public static GameSettings Deserialize(string json) =>
        JsonSerializer.Deserialize<GameSettings>(json, Options) ?? new GameSettings();
}
