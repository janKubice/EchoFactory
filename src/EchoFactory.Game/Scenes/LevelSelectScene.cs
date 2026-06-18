using EchoFactory.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoFactory.Game;

internal sealed class LevelSelectScene : IScene
{
    private readonly SceneManager _scenes;
    private readonly List<(UiButton Button, string LevelId)> _levels = [];
    private readonly UiButton _back;
    private readonly int _viewTop;
    private readonly int _viewBottom;
    private readonly float _maxScroll;
    private float _scroll;
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

        const int contentTop = 130;
        for (int i = 0; i < items.Count; i++)
        {
            int col = i / perCol;
            int row = i % perCol;
            int bx = startX + (col * (colW + gapX));
            int by = contentTop + (row * (bh + gapY));
            _levels.Add((new UiButton(new Rectangle(bx, by, colW, bh), items[i].Name.ToUpperInvariant()), items[i].Id));
        }

        _viewTop = contentTop - 6;
        _viewBottom = scenes.ScreenH - 24;
        int contentBottom = contentTop + (perCol * (bh + gapY));
        _maxScroll = Math.Max(0, contentBottom - _viewBottom + 8);

        _back = new UiButton(new Rectangle(40, 40, 150, 46), "< BACK");
    }

    public void Update(float dt, InputState input)
    {
        _mouse = input.Mouse;

        if (_maxScroll > 0 && input.ScrollDelta != 0)
        {
            _scroll = Math.Clamp(_scroll - (input.ScrollDelta * 0.4f), 0f, _maxScroll);
        }

        if ((input.LeftClick && _back.Hit(_mouse)) || input.KeyPressed(Keys.Escape))
        {
            _scenes.Switch(new MainMenuScene(_scenes));
            return;
        }

        if (input.LeftClick)
        {
            foreach (var (button, levelId) in _levels)
            {
                Rectangle hit = Shift(button.Rect);
                if (hit.Y + hit.Height > _viewTop && hit.Y < _viewBottom && hit.Contains(Point(_mouse)))
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
            Rectangle rect = Shift(button.Rect);
            if (rect.Y + rect.Height <= _viewTop || rect.Y >= _viewBottom)
            {
                continue; // outside the scroll viewport
            }

            new UiButton(rect, button.Label).Draw(r, _mouse);
            int stars = _scenes.Leaderboard.Get(levelId)?.BestStars ?? 0;
            if (stars > 0)
            {
                r.Text(new string('*', stars), new Vector2(rect.Right - 70, rect.Y + 12), 3f, Palette.Solved);
            }
        }

        if (_maxScroll > 0)
        {
            r.TextCentered("SCROLL FOR MORE", new Vector2(r.Width / 2f, r.Height - 14f), 1.9f, Palette.TextDim);
        }
    }

    private Rectangle Shift(Rectangle rect) => new(rect.X, rect.Y - (int)_scroll, rect.Width, rect.Height);

    private static Point Point(Vector2 v) => new((int)v.X, (int)v.Y);
}
