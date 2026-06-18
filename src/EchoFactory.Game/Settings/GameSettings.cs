namespace EchoFactory.Game;

/// <summary>Player-tweakable settings, persisted to JSON.</summary>
public sealed class GameSettings
{
    public float MasterVolume { get; set; } = 0.8f;

    public float SfxVolume { get; set; } = 0.9f;

    public float MusicVolume { get; set; } = 0.5f;

    public bool ShowGrid { get; set; } = true;

    public float PlaybackSpeed { get; set; } = 3.5f;

    /// <summary>Borderless fullscreen (the virtual 1280x720 view is letterboxed to the screen).</summary>
    public bool Fullscreen { get; set; }

    /// <summary>Show the company/studio intro splash on launch.</summary>
    public bool ShowIntro { get; set; } = true;

    /// <summary>Path to the intro logo image (PNG/JPG/BMP). Relative paths resolve next to the executable.</summary>
    public string LogoPath { get; set; } = "branding/logo.png";

    /// <summary>Studio/company name shown under the logo on the intro (blank = hidden).</summary>
    public string CompanyName { get; set; } = "YOUR STUDIO";

    public float EffectiveSfx => Math.Clamp(MasterVolume, 0f, 1f) * Math.Clamp(SfxVolume, 0f, 1f);
}
