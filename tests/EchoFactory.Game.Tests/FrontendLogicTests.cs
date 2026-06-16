using EchoFactory.Game;
using Xunit;

namespace EchoFactory.Game.Tests;

public class FrontendLogicTests
{
    [Fact]
    public void Settings_RoundTripThroughJson()
    {
        var settings = new GameSettings { MasterVolume = 0.3f, SfxVolume = 0.5f, ShowGrid = false, PlaybackSpeed = 6f };

        string json = SettingsStore.Serialize(settings);
        GameSettings back = SettingsStore.Deserialize(json);

        Assert.Equal(0.3f, back.MasterVolume);
        Assert.Equal(0.5f, back.SfxVolume);
        Assert.False(back.ShowGrid);
        Assert.Equal(6f, back.PlaybackSpeed);
    }

    [Fact]
    public void EffectiveSfx_IsMasterTimesSfx()
    {
        var settings = new GameSettings { MasterVolume = 0.5f, SfxVolume = 0.4f };
        Assert.Equal(0.2f, settings.EffectiveSfx, 3);
    }

    [Fact]
    public void Synth_Tone_ProducesExpectedPcmLength()
    {
        byte[] pcm = Synth.Tone(440f, 0.1f, Synth.Shape.Sine);
        // 16-bit mono => 2 bytes per sample.
        Assert.Equal((int)(0.1f * Synth.SampleRate) * 2, pcm.Length);
    }
}
