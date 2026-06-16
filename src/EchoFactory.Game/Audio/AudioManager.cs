using Microsoft.Xna.Framework.Audio;

namespace EchoFactory.Game;

internal enum Sfx
{
    Place,
    Remove,
    Click,
    Compile,
    Deliver,
    Solved,
    Paradox,
}

/// <summary>Plays procedural sound effects. Fails soft if there is no audio device.</summary>
internal sealed class AudioManager : IDisposable
{
    private readonly Dictionary<Sfx, SoundEffect> _effects = [];
    private readonly bool _enabled;

    public AudioManager()
    {
        try
        {
            _effects[Sfx.Place] = Make(Synth.Tone(523f, 0.07f, Synth.Shape.Square));
            _effects[Sfx.Remove] = Make(Synth.Tone(196f, 0.07f, Synth.Shape.Square));
            _effects[Sfx.Click] = Make(Synth.Tone(660f, 0.05f, Synth.Shape.Sine));
            _effects[Sfx.Compile] = Make(Synth.Tone(330f, 0.16f, Synth.Shape.Triangle));
            _effects[Sfx.Deliver] = Make(Synth.Tone(880f, 0.06f, Synth.Shape.Sine));
            _effects[Sfx.Solved] = Make(Synth.Sequence([660f, 880f, 1320f], 0.09f, Synth.Shape.Sine));
            _effects[Sfx.Paradox] = Make(Synth.Tone(110f, 0.32f, Synth.Shape.Square));
            _enabled = true;
        }
        catch (Exception e) when (e is NoAudioHardwareException or InvalidOperationException or NotSupportedException)
        {
            _enabled = false;
        }
    }

    public void Play(Sfx sfx, float volume)
    {
        if (!_enabled || volume <= 0f)
        {
            return;
        }

        try
        {
            if (_effects.TryGetValue(sfx, out var effect))
            {
                effect.Play(Math.Clamp(volume, 0f, 1f), 0f, 0f);
            }
        }
        catch (InvalidOperationException)
        {
            // too many concurrent instances — drop this one
        }
    }

    public void Dispose()
    {
        foreach (var effect in _effects.Values)
        {
            effect.Dispose();
        }
    }

    private static SoundEffect Make(byte[] pcm) => new(pcm, Synth.SampleRate, AudioChannels.Mono);
}
