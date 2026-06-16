using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoFactory.Game;

internal sealed class MainMenuScene : IScene
{
    private readonly SceneManager _scenes;
    private readonly UiButton _play;
    private readonly UiButton _settings;
    private readonly UiButton _quit;
    private Vector2 _mouse;

    public MainMenuScene(SceneManager scenes)
    {
        _scenes = scenes;
        int w = scenes.ScreenW;
        int h = scenes.ScreenH;
        const int bw = 300;
        const int bh = 56;
        int x = (w - bw) / 2;
        _play = new UiButton(new Rectangle(x, (h / 2) + 4, bw, bh), "PLAY");
        _settings = new UiButton(new Rectangle(x, (h / 2) + 72, bw, bh), "SETTINGS");
        _quit = new UiButton(new Rectangle(x, (h / 2) + 140, bw, bh), "QUIT");
    }

    public void Update(float dt, InputState input)
    {
        _mouse = input.Mouse;

        if ((input.LeftClick && _play.Hit(_mouse)) || input.KeyPressed(Keys.Enter))
        {
            _scenes.Play(Sfx.Click);
            _scenes.Switch(new LevelSelectScene(_scenes));
        }
        else if (input.LeftClick && _settings.Hit(_mouse))
        {
            _scenes.Play(Sfx.Click);
            _scenes.Switch(new SettingsScene(_scenes, this));
        }
        else if ((input.LeftClick && _quit.Hit(_mouse)) || input.KeyPressed(Keys.Escape))
        {
            _scenes.Quit();
        }
    }

    public void Draw(Renderer r)
    {
        r.TextCentered("ECHOFACTORY", new Vector2(r.Width / 2f, (r.Height / 2f) - 130), 11f, Palette.Accent);
        r.TextCentered("TIME-LOOP FACTORY PUZZLES", new Vector2(r.Width / 2f, (r.Height / 2f) - 74), 3f, Palette.TextDim);
        _play.Draw(r, _mouse);
        _settings.Draw(r, _mouse);
        _quit.Draw(r, _mouse);
    }
}
