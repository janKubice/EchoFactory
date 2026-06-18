using EchoFactory.Content;
using EchoFactory.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoFactory.Game;

/// <summary>Play hub: lists campaigns (grouped levels) with progress, then drills into one.</summary>
internal sealed class CampaignSelectScene : IScene
{
    private readonly record struct Item(string Name, Rectangle Rect, int Solved, int Total, int Stars);

    private readonly SceneManager _scenes;
    private readonly List<Item> _items = [];
    private readonly UiButton _back;
    private Vector2 _mouse;

    public CampaignSelectScene(SceneManager scenes)
    {
        _scenes = scenes;
        scenes.Catalog.Reload();

        var groups = new Dictionary<string, (int MinOrder, int Total, int Solved, int Stars)>(StringComparer.Ordinal);
        var seen = new List<string>();
        foreach (string id in scenes.Catalog.LevelIds)
        {
            string camp;
            int order;
            try
            {
                LevelDefinition level = scenes.Catalog.LoadLevel(id);
                camp = LevelSelectScene.CampaignOf(level);
                order = level.Order;
            }
            catch (ContentException)
            {
                camp = "Other";
                order = 1000;
            }

            int stars = scenes.Leaderboard.Get(id)?.BestStars ?? 0;
            if (!groups.TryGetValue(camp, out var g))
            {
                g = (order, 0, 0, 0);
                seen.Add(camp);
            }

            g.MinOrder = Math.Min(g.MinOrder, order);
            g.Total++;
            g.Solved += stars > 0 ? 1 : 0;
            g.Stars += stars;
            groups[camp] = g;
        }

        seen.Sort((a, b) => groups[a].MinOrder != groups[b].MinOrder
            ? groups[a].MinOrder.CompareTo(groups[b].MinOrder)
            : string.CompareOrdinal(a, b));

        int w = scenes.ScreenW;
        const int bw = 560;
        const int bh = 72;
        const int gap = 18;
        int x = (w - bw) / 2;
        int y = 190;
        foreach (string camp in seen)
        {
            var g = groups[camp];
            _items.Add(new Item(camp, new Rectangle(x, y, bw, bh), g.Solved, g.Total, g.Stars));
            y += bh + gap;
        }

        _back = new UiButton(new Rectangle(40, 40, 150, 46), "< BACK");
    }

    public void Update(float dt, InputState input)
    {
        _mouse = input.Mouse;
        _scenes.Background.Update(dt);

        if ((input.LeftClick && _back.Hit(_mouse)) || input.KeyPressed(Keys.Escape))
        {
            _scenes.Switch(new MainMenuScene(_scenes));
            return;
        }

        if (input.LeftClick)
        {
            foreach (Item item in _items)
            {
                if (item.Rect.Contains((int)_mouse.X, (int)_mouse.Y))
                {
                    _scenes.Play(Sfx.Click);
                    _scenes.Switch(new LevelSelectScene(_scenes, item.Name));
                    return;
                }
            }
        }
    }

    public void Draw(Renderer r)
    {
        _scenes.Background.Draw(r);
        r.TextCentered("CAMPAIGNS", new Vector2(r.Width / 2f, 90f), 6f, Palette.Text);
        _back.Draw(r, _mouse);

        foreach (Item item in _items)
        {
            bool hover = item.Rect.Contains((int)_mouse.X, (int)_mouse.Y);
            bool complete = item.Total > 0 && item.Solved >= item.Total;
            r.FillRect(item.Rect.X, item.Rect.Y, item.Rect.Width, item.Rect.Height, hover ? Palette.PanelHi : Palette.Panel);
            r.RectOutline(item.Rect.X, item.Rect.Y, item.Rect.Width, item.Rect.Height, hover ? 3f : 2f, complete ? Palette.Solved : (hover ? Palette.Accent : Palette.GridLine));

            r.Text(item.Name.ToUpperInvariant(), new Vector2(item.Rect.X + 22, item.Rect.Y + 14), 3.2f, hover ? Palette.Text : Palette.TextDim);
            string progress = item.Solved + " / " + item.Total + " SOLVED   " + item.Stars + " STARS";
            r.Text(progress, new Vector2(item.Rect.X + 22, item.Rect.Y + 46), 2.1f, complete ? Palette.Solved : Palette.TextDim);

            int maxStars = item.Total * 3;
            r.Text(maxStars > 0 ? (int)(100f * item.Stars / maxStars) + "%" : "0%", new Vector2(item.Rect.Right - 78, item.Rect.Y + 26), 3f, Palette.Item);
        }
    }
}
