using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoFactory.Game;

internal sealed class SettingsScene : IScene
{
    private readonly SceneManager _scenes;
    private readonly IScene _back;
    private readonly Rectangle _masterTrack;
    private readonly Rectangle _sfxTrack;
    private readonly Rectangle _musicTrack;
    private readonly Rectangle _gridToggle;
    private readonly Rectangle _introToggle;
    private readonly UiButton _backBtn;
    private Vector2 _mouse;

    public SettingsScene(SceneManager scenes, IScene back)
    {
        _scenes = scenes;
        _back = back;

        int w = scenes.ScreenW;
        const int sw = 440;
        int sx = (w - sw) / 2;
        int y = 220;
        _masterTrack = new Rectangle(sx, y, sw, 16);
        _sfxTrack = new Rectangle(sx, y + 80, sw, 16);
        _musicTrack = new Rectangle(sx, y + 160, sw, 16);
        _gridToggle = new Rectangle(sx, y + 230, sw, 46);
        _introToggle = new Rectangle(sx, y + 286, sw, 46);
        _backBtn = new UiButton(new Rectangle(40, 40, 150, 46), "< BACK");
    }

    public void Update(float dt, InputState input)
    {
        _mouse = input.Mouse;
        _scenes.Background.Update(dt);
        GameSettings s = _scenes.Settings;

        if (input.LeftDown)
        {
            if (Near(_masterTrack))
            {
                s.MasterVolume = ValueAt(_masterTrack);
            }

            if (Near(_sfxTrack))
            {
                s.SfxVolume = ValueAt(_sfxTrack);
            }

            if (Near(_musicTrack))
            {
                s.MusicVolume = ValueAt(_musicTrack);
            }
        }

        if (input.LeftClick)
        {
            if (_gridToggle.Contains((int)_mouse.X, (int)_mouse.Y))
            {
                s.ShowGrid = !s.ShowGrid;
                _scenes.Play(Sfx.Click);
            }

            if (_introToggle.Contains((int)_mouse.X, (int)_mouse.Y))
            {
                s.ShowIntro = !s.ShowIntro;
                _scenes.Play(Sfx.Click);
            }

            if (_backBtn.Hit(_mouse))
            {
                _scenes.Switch(_back);
            }
        }

        if (input.KeyPressed(Keys.Escape))
        {
            _scenes.Switch(_back);
        }
    }

    public void Draw(Renderer r)
    {
        _scenes.Background.Draw(r);
        GameSettings s = _scenes.Settings;
        r.TextCentered("SETTINGS", new Vector2(r.Width / 2f, 110f), 6f, Palette.Text);
        _backBtn.Draw(r, _mouse);

        Slider(r, _masterTrack, "MASTER VOLUME", s.MasterVolume);
        Slider(r, _sfxTrack, "SOUND EFFECTS", s.SfxVolume);
        Slider(r, _musicTrack, "MUSIC", s.MusicVolume);

        bool hover = _gridToggle.Contains((int)_mouse.X, (int)_mouse.Y);
        r.FillRect(_gridToggle.X, _gridToggle.Y, _gridToggle.Width, _gridToggle.Height, hover ? Palette.PanelHi : Palette.Panel);
        r.RectOutline(_gridToggle.X, _gridToggle.Y, _gridToggle.Width, _gridToggle.Height, 2, hover ? Palette.Accent : Palette.GridLine);
        r.TextCentered("SHOW GRID:  " + (s.ShowGrid ? "ON" : "OFF"), new Vector2(_gridToggle.Center.X, _gridToggle.Center.Y), 2.6f, s.ShowGrid ? Palette.Solved : Palette.TextDim);

        bool introHover = _introToggle.Contains((int)_mouse.X, (int)_mouse.Y);
        r.FillRect(_introToggle.X, _introToggle.Y, _introToggle.Width, _introToggle.Height, introHover ? Palette.PanelHi : Palette.Panel);
        r.RectOutline(_introToggle.X, _introToggle.Y, _introToggle.Width, _introToggle.Height, 2, introHover ? Palette.Accent : Palette.GridLine);
        r.TextCentered("INTRO LOGO:  " + (s.ShowIntro ? "ON" : "OFF"), new Vector2(_introToggle.Center.X, _introToggle.Center.Y), 2.6f, s.ShowIntro ? Palette.Solved : Palette.TextDim);

        r.TextCentered("DRAG SLIDERS - CLICK TOGGLE - ESC TO SAVE & BACK", new Vector2(r.Width / 2f, r.Height - 50f), 2.4f, Palette.TextDim);
    }

    private static void Slider(Renderer r, Rectangle track, string label, float value)
    {
        r.Text(label, new Vector2(track.X, track.Y - 26), 2.6f, Palette.Text);
        r.FillRect(track.X, track.Y, track.Width, track.Height, Palette.Panel);
        r.FillRect(track.X, track.Y, track.Width * Math.Clamp(value, 0f, 1f), track.Height, Palette.Accent);
        float hx = track.X + (track.Width * Math.Clamp(value, 0f, 1f));
        r.Disc(new Vector2(hx, track.Y + (track.Height / 2f)), 10f, Palette.Text);
        r.Text((int)(value * 100) + "%", new Vector2(track.Right + 16, track.Y - 4), 2.6f, Palette.TextDim);
    }

    private bool Near(Rectangle track) =>
        _mouse.X >= track.X - 12 && _mouse.X <= track.Right + 12 && _mouse.Y >= track.Y - 18 && _mouse.Y <= track.Bottom + 18;

    private float ValueAt(Rectangle track) => Math.Clamp((_mouse.X - track.X) / track.Width, 0f, 1f);
}
