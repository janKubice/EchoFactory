using EchoFactory.Content;
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

        var items = new List<(string Id, string Name, int Order)>();
        foreach (string id in scenes.Catalog.LevelIds)
        {
            try
            {
                var level = scenes.Catalog.LoadLevel(id);
                items.Add((id, string.IsNullOrEmpty(level.Name) ? id : level.Name, level.Order));
            }
            catch (ContentException)
            {
                items.Add((id, id, 1000));
            }
        }

        items.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.CompareOrdinal(a.Id, b.Id));

        int w = scenes.ScreenW;
        const int colW = 460;
        const int bh = 44;
        const int gapY = 10;
        const int gapX = 48;
        int totalW = (2 * colW) + gapX;
        int startX = (w - totalW) / 2;
        int perCol = (items.Count + 1) / 2;

        for (int i = 0; i < items.Count; i++)
        {
            int col = i / perCol;
            int row = i % perCol;
            int bx = startX + (col * (colW + gapX));
            int by = 130 + (row * (bh + gapY));
            _levels.Add((new UiButton(new Rectangle(bx, by, colW, bh), items[i].Name.ToUpperInvariant()), items[i].Id));
        }

        _back = new UiButton(new Rectangle(40, 40, 150, 46), "< BACK");
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
                    _scenes.Play(Sfx.Click);
                    _scenes.Switch(new GameplayScene(_scenes, levelId));
                    return;
                }
            }
        }
    }

    public void Draw(Renderer r)
    {
        r.TextCentered("SELECT LEVEL", new Vector2(r.Width / 2f, 80f), 6f, Palette.Text);
        _back.Draw(r, _mouse);

        foreach (var (button, levelId) in _levels)
        {
            button.Draw(r, _mouse);
            int stars = _scenes.Leaderboard.Get(levelId)?.BestStars ?? 0;
            if (stars > 0)
            {
                r.Text(new string('*', stars), new Vector2(button.Rect.Right - 70, button.Rect.Y + 12), 3f, Palette.Solved);
            }
        }
    }
}
