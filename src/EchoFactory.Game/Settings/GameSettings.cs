namespace EchoFactory.Game;

/// <summary>Player-tweakable settings, persisted to JSON.</summary>
public sealed class GameSettings
{
    public float MasterVolume { get; set; } = 0.8f;

    public float SfxVolume { get; set; } = 0.9f;

    public float MusicVolume { get; set; } = 0.5f;

    public bool ShowGrid { get; set; } = true;

    public float PlaybackSpeed { get; set; } = 3.5f;

    public float EffectiveSfx => Math.Clamp(MasterVolume, 0f, 1f) * Math.Clamp(SfxVolume, 0f, 1f);
}
