using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoFactory.Game;

/// <summary>Asset-free 2D drawing: rectangles, lines, discs/rings and bitmap text.</summary>
internal sealed class Renderer : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly SpriteBatch _batch;
    private readonly Texture2D _pixel;
    private readonly Texture2D _disc;
    private const int DiscSize = 128;

    public Renderer(GraphicsDevice device)
    {
        _device = device;
        _batch = new SpriteBatch(device);

        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _disc = new Texture2D(device, DiscSize, DiscSize);
        var pixels = new Color[DiscSize * DiscSize];
        float r = DiscSize / 2f;
        for (int y = 0; y < DiscSize; y++)
        {
            for (int x = 0; x < DiscSize; x++)
            {
                float dx = x + 0.5f - r;
                float dy = y + 0.5f - r;
                float d = MathF.Sqrt((dx * dx) + (dy * dy));
                float a = Math.Clamp(r - d, 0f, 1f); // 1px soft edge
                pixels[(y * DiscSize) + x] = new Color(1f, 1f, 1f, a);
            }
        }

        _disc.SetData(pixels);
    }

    public int Width => _device.Viewport.Width;

    public int Height => _device.Viewport.Height;

    public void Begin() => _batch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp);

    public void End() => _batch.End();

    public void FillRect(float x, float y, float w, float h, Color color) =>
        _batch.Draw(_pixel, new Rectangle((int)x, (int)y, (int)w, (int)h), color);

    public void RectOutline(float x, float y, float w, float h, float thickness, Color color)
    {
        FillRect(x, y, w, thickness, color);
        FillRect(x, y + h - thickness, w, thickness, color);
        FillRect(x, y, thickness, h, color);
        FillRect(x + w - thickness, y, thickness, h, color);
    }

    public void Line(Vector2 a, Vector2 b, float thickness, Color color)
    {
        Vector2 d = b - a;
        float len = d.Length();
        if (len < 0.001f)
        {
            return;
        }

        float angle = MathF.Atan2(d.Y, d.X);
        _batch.Draw(
            _pixel,
            a,
            null,
            color,
            angle,
            new Vector2(0, 0.5f),
            new Vector2(len, thickness),
            SpriteEffects.None,
            0f);
    }

    public void Disc(Vector2 center, float radius, Color color)
    {
        float scale = radius * 2f / DiscSize;
        _batch.Draw(
            _disc,
            center,
            null,
            color,
            0f,
            new Vector2(DiscSize / 2f, DiscSize / 2f),
            scale,
            SpriteEffects.None,
            0f);
    }

    public void Ring(Vector2 center, float radius, float thickness, Color color, int segments = 40)
    {
        Vector2 prev = center + new Vector2(radius, 0);
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments * MathHelper.TwoPi;
            Vector2 next = center + new Vector2(MathF.Cos(t) * radius, MathF.Sin(t) * radius);
            Line(prev, next, thickness, color);
            prev = next;
        }
    }

    public void Text(string text, Vector2 pos, float pixel, Color color)
    {
        float cx = pos.X;
        foreach (char c in text)
        {
            string[] glyph = VectorFont.Glyph(c);
            for (int row = 0; row < VectorFont.GlyphH; row++)
            {
                string line = glyph[row];
                for (int col = 0; col < line.Length; col++)
                {
                    if (line[col] == '#')
                    {
                        FillRect(cx + (col * pixel), pos.Y + (row * pixel), pixel, pixel, color);
                    }
                }
            }

            cx += (VectorFont.GlyphW + 1) * pixel;
        }
    }

    public void TextCentered(string text, Vector2 center, float pixel, Color color)
    {
        Vector2 size = Measure(text, pixel);
        Text(text, new Vector2(center.X - (size.X / 2f), center.Y - (size.Y / 2f)), pixel, color);
    }

    /// <summary>Draws text centered on <paramref name="center"/>, scaled to fit within maxW x maxH.</summary>
    public void TextCenteredFit(string text, Vector2 center, float maxW, float maxH, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        float widthAtOne = (text.Length * (VectorFont.GlyphW + 1)) - 1f;
        float pixel = MathF.Min(maxW / widthAtOne, maxH / VectorFont.GlyphH);
        pixel = MathF.Max(pixel, 0.75f);
        TextCentered(text, center, pixel, color);
    }

    public static Vector2 Measure(string text, float pixel) =>
        new(text.Length <= 0 ? 0 : (text.Length * (VectorFont.GlyphW + 1) * pixel) - pixel, VectorFont.GlyphH * pixel);

    public void Dispose()
    {
        _batch.Dispose();
        _pixel.Dispose();
        _disc.Dispose();
    }
}
