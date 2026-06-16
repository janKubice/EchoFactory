namespace EchoFactory.Game;

/// <summary>Generates short tones as 16-bit mono PCM — procedural SFX, no audio assets.</summary>
internal static class Synth
{
    public const int SampleRate = 22050;

    public enum Shape
    {
        Sine,
        Square,
        Triangle,
    }

    public static byte[] Tone(float frequency, float seconds, Shape shape)
    {
        int sampleCount = (int)(seconds * SampleRate);
        var buffer = new byte[sampleCount * 2];
        WriteTone(buffer, 0, sampleCount, frequency, shape);
        return buffer;
    }

    /// <summary>A small arpeggio (one tone per frequency, back to back).</summary>
    public static byte[] Sequence(float[] frequencies, float secondsEach, Shape shape)
    {
        int per = (int)(secondsEach * SampleRate);
        var buffer = new byte[per * frequencies.Length * 2];
        for (int n = 0; n < frequencies.Length; n++)
        {
            WriteTone(buffer, n * per, per, frequencies[n], shape);
        }

        return buffer;
    }

    private static void WriteTone(byte[] buffer, int startSample, int count, float frequency, Shape shape)
    {
        for (int i = 0; i < count; i++)
        {
            float phase = i / (float)SampleRate * frequency;
            float wave = shape switch
            {
                Shape.Square => MathF.Sin(phase * MathF.Tau) >= 0 ? 1f : -1f,
                Shape.Triangle => (2f * MathF.Abs((2f * (phase - MathF.Floor(phase + 0.5f)))) ) - 1f,
                _ => MathF.Sin(phase * MathF.Tau),
            };

            float env = Envelope(i, count);
            short sample = (short)(wave * env * 0.35f * short.MaxValue);
            int o = (startSample + i) * 2;
            buffer[o] = (byte)(sample & 0xFF);
            buffer[o + 1] = (byte)((sample >> 8) & 0xFF);
        }
    }

    private static float Envelope(int i, int total)
    {
        float t = i / (float)total;
        const float attack = 0.03f;
        return t < attack ? t / attack : MathF.Pow(1f - ((t - attack) / (1f - attack)), 1.5f);
    }
}
