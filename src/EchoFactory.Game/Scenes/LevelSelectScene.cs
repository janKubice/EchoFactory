using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoFactory.Game;

internal sealed class LevelSelectScene : IScene
{
    private readonly SceneManager _scenes;
    private readonly List<(UiButton Button, string LevelId)> _levels = [];
    private readonly UiButton _back;
    private Vector2 _mouse;

    public LevelSelectScene(SceneManager scenes)
    {
        _scenes = scenes;
        scenes.Catalog.Reload(); // pick up any editor-saved levels
        int w = scenes.ScreenW;

        const int bw = 360;
        const int bh = 46;
        int x = (w - bw) / 2;
        int y = 130;
        foreach (string id in scenes.Catalog.LevelIds)
        {
            _levels.Add((new UiButton(new Rectangle(x, y, bw, bh), id.ToUpperInvariant()), id));
            y += bh + 12;
        }

        _back = new UiButton(new Rectangle(40, 40, 140, 44), "< BACK");
    }

    public void Update(float dt, InputState input)
    {
        _mouse = input.Mouse;

        if ((input.LeftClick && _back.Hit(_mouse)) || input.KeyPressed(Keys.Escape))
        {
            _scenes.Switch(new MainMenuScene(_scenes));
            return;
        }

        if (input.LeftClick)
        {
            foreach (var (button, levelId) in _levels)
            {
                if (button.Hit(_mouse))
                {
                    _scenes.Switch(new GameplayScene(_scenes, levelId));
                    return;
                }
            }
        }
    }

    public void Draw(Renderer r)
    {
        r.TextCentered("SELECT LEVEL", new Vector2(r.Width / 2f, 70f), 6f, Palette.Text);
        _back.Draw(r, _mouse);
        foreach (var (button, _) in _levels)
        {
            button.Draw(r, _mouse);
        }
    }
}
