using Microsoft.Xna.Framework;

namespace EchoFactory.Game;

/// <summary>Minimal retained button: a rectangle + label with hover styling.</summary>
internal readonly struct UiButton
{
    public UiButton(Rectangle rect, string label)
    {
        Rect = rect;
        Label = label;
    }

    public Rectangle Rect { get; }

    public string Label { get; }

    public bool Hit(Vector2 mouse) => Rect.Contains((int)mouse.X, (int)mouse.Y);

    public void Draw(Renderer r, Vector2 mouse)
    {
        bool hover = Hit(mouse);
        r.FillRect(Rect.X, Rect.Y, Rect.Width, Rect.Height, hover ? Palette.PanelHi : Palette.Panel);
        r.RectOutline(Rect.X, Rect.Y, Rect.Width, Rect.Height, 2, hover ? Palette.Accent : Palette.GridLine);
        float pixel = MathF.Max(2f, Rect.Height / 14f);
        r.TextCentered(Label, new Vector2(Rect.Center.X, Rect.Center.Y), pixel, hover ? Palette.Text : Palette.TextDim);
    }
}
