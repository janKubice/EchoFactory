using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoFactory.Game;

/// <summary>
/// Studio/company intro shown once on launch. Loads a configurable logo image
/// (<see cref="GameSettings.LogoPath"/>); when none is present it draws a labelled
/// placeholder telling the user exactly where to drop their file. Skippable by any key/click.
/// </summary>
internal sealed class SplashScene : IScene
{
    private const float FadeIn = 0.5f;
    private const float Hold = 1.7f;
    private const float FadeOut = 0.5f;
    private const float Total = FadeIn + Hold + FadeOut;

    private readonly SceneManager _scenes;
    private float _t;
    private bool _done;
    private bool _logoTried;
    private Texture2D? _logo;

    public SplashScene(SceneManager scenes) => _scenes = scenes;

    public void Update(float dt, InputState input)
    {
        _t += dt;
        if (input.LeftClick || input.AnyKeyPressed || _t >= Total)
        {
            Finish();
        }
    }

    public void Draw(Renderer r)
    {
        if (!_logoTried)
        {
            _logoTried = true;
            _logo = r.TryLoadTexture(BrandingPaths.Resolve(_scenes.Settings.LogoPath));
        }

        byte a = (byte)(Math.Clamp(Alpha(), 0f, 1f) * 255f);
        var box = new Rectangle((r.Width / 2) - 230, (r.Height / 2) - 150, 460, 230);

        if (_logo is not null)
        {
            r.DrawTextureFit(_logo, box, new Color(a, a, a, a));
        }
        else
        {
            // No logo yet — show the user where to put one.
            r.RectOutline(box.X, box.Y, box.Width, box.Height, 3, Fade(Palette.Accent, a));
            r.TextCentered("[ YOUR LOGO HERE ]", new Vector2(box.Center.X, box.Center.Y - 18), 3.4f, Fade(Palette.Accent, a));
            r.TextCenteredFit(
                "DROP  " + _scenes.Settings.LogoPath.ToUpperInvariant() + "  NEXT TO THE GAME",
                new Vector2(box.Center.X, box.Center.Y + 30),
                box.Width - 30,
                18,
                Fade(Palette.TextDim, a));
        }

        string company = _scenes.Settings.CompanyName?.Trim().ToUpperInvariant() ?? string.Empty;
        if (company.Length > 0)
        {
            r.TextCentered(company, new Vector2(r.Width / 2f, box.Bottom + 44), 3.6f, Fade(Palette.Text, a));
        }

        r.TextCentered("PRESS ANY KEY", new Vector2(r.Width / 2f, r.Height - 56f), 2f, Fade(Palette.TextDim, (byte)(a / 2)));
    }

    private float Alpha()
    {
        if (_t < FadeIn)
        {
            return _t / FadeIn;
        }

        if (_t > FadeIn + Hold)
        {
            return MathF.Max(0f, 1f - ((_t - FadeIn - Hold) / FadeOut));
        }

        return 1f;
    }

    private void Finish()
    {
        if (_done)
        {
            return;
        }

        _done = true;
        _logo?.Dispose();
        _scenes.Switch(new MainMenuScene(_scenes));
    }

    private static Color Fade(Color c, byte a) => new(c.R, c.G, c.B, a);
}
