using Microsoft.Xna.Framework;

namespace EchoFactory.Game;

/// <summary>
/// A subtle animated "factory" backdrop for menu screens: faint horizontal belts with
/// flowing chevrons and drifting item dots. Asset-free, cheap, and drawn behind the UI
/// with low alpha so foreground text stays readable. One shared instance keeps the
/// animation continuous as the player moves between menu scenes.
/// </summary>
internal sealed class MenuBackground
{
    private const int Rows = 8;
    private static readonly Color[] RowColors =
    [
        Palette.Belt, Palette.Math, Palette.Splitter, Palette.Filter,
        Palette.Router, Palette.Portal, Palette.Accumulator, Palette.Accent,
    ];

    private float _t;

    public void Update(float dt) => _t += dt;

    public void Draw(Renderer r)
    {
        float w = r.Width;
        float h = r.Height;
        float rowH = h / (Rows + 1f);
        const float spacing = 92f;
        const float chev = 9f;

        for (int row = 0; row < Rows; row++)
        {
            float y = rowH * (row + 1);
            int dir = (row % 2 == 0) ? 1 : -1;
            float speed = 26f + (row * 5f);
            Color col = RowColors[row % RowColors.Length];

            // belt track
            r.Line(new Vector2(0, y), new Vector2(w, y), 2f, Fade(Palette.GridLine, 26));

            // flowing chevrons
            float phase = Mod(_t * speed * dir, spacing);
            Color chevCol = Fade(col, 34);
            for (float x = -spacing + phase; x < w + spacing; x += spacing)
            {
                var tip = new Vector2(x + (dir * chev), y);
                r.Line(tip, new Vector2(x - (dir * chev), y - chev), 2f, chevCol);
                r.Line(tip, new Vector2(x - (dir * chev), y + chev), 2f, chevCol);
            }

            // a couple of drifting items riding the belt
            for (int k = 0; k < 2; k++)
            {
                float start = (k * 0.5f * w) + (dir > 0 ? 0 : w);
                float ix = Mod(start + (_t * speed * dir), w + 160f) - 80f;
                r.Disc(new Vector2(ix, y), 5f, Fade(Palette.Item, 64));
            }
        }
    }

    private static float Mod(float a, float m)
    {
        float v = a % m;
        return v < 0 ? v + m : v;
    }

    private static Color Fade(Color c, byte a) => new(c.R, c.G, c.B, a);
}
