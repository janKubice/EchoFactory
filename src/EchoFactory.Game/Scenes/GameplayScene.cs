using EchoFactory.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoFactory.Game;

internal enum Tool
{
    Belt,
    MathAdd,
    MathMul,
    Splitter,
    Portal,
}

internal enum PlayMode
{
    Build,
    Playback,
}

/// <summary>Build → Compile → Playback for a single level.</summary>
internal sealed class GameplayScene : IScene
{
    private static readonly (Tool Tool, string Label)[] Tools =
    {
        (Tool.Belt, "1 BELT"),
        (Tool.MathAdd, "2 ADD"),
        (Tool.MathMul, "3 MUL x2"),
        (Tool.Splitter, "4 SPLIT"),
        (Tool.Portal, "5 PORTAL -2"),
    };

    private readonly SceneManager _scenes;
    private readonly string _levelId;
    private readonly LevelDefinition _level;
    private readonly BuildEditor _editor;
    private readonly GridView _grid;
    private readonly Rectangle _playArea;
    private readonly Rectangle _timeline;
    private readonly Rectangle _compileBtn;
    private readonly List<(Rectangle Rect, Tool Tool)> _palette = [];

    private Tool _tool = Tool.Belt;
    private Direction _dir = Direction.Right;
    private PlayMode _mode = PlayMode.Build;
    private SimulationResult? _result;
    private float _playTime;
    private bool _playing;
    private float _speed = 3.5f;
    private Vector2 _mouse;

    public GameplayScene(SceneManager scenes, string levelId)
    {
        _scenes = scenes;
        _levelId = levelId;
        _level = scenes.Catalog.LoadLevel(levelId);
        _editor = new BuildEditor(_level);

        int w = scenes.ScreenW;
        int h = scenes.ScreenH;
        _playArea = new Rectangle(40, 72, w - 80, h - 72 - 150);
        _grid = new GridView(_level.Grid, _playArea);
        _timeline = new Rectangle(40, h - 64, w - 280, 18);
        _compileBtn = new Rectangle(w - 220, h - 132, 180, 48);

        int px = 40;
        foreach (var (tool, _) in Tools)
        {
            _palette.Add((new Rectangle(px, h - 132, 150, 48), tool));
            px += 158;
        }
    }

    public void Update(float dt, InputState input)
    {
        _mouse = input.Mouse;
        bool overGrid = _grid.TryScreenToCell(_mouse, out GridPoint cell);

        if (input.KeyPressed(Keys.Escape))
        {
            if (_mode == PlayMode.Playback)
            {
                _mode = PlayMode.Build;
            }
            else
            {
                _scenes.Switch(new LevelSelectScene(_scenes));
            }

            return;
        }

        if (_mode == PlayMode.Build)
        {
            UpdateBuild(input, overGrid, cell);
        }
        else
        {
            UpdatePlayback(dt, input);
        }
    }

    private void UpdateBuild(InputState input, bool overGrid, GridPoint cell)
    {
        if (input.KeyPressed(Keys.D1)) _tool = Tool.Belt;
        if (input.KeyPressed(Keys.D2)) _tool = Tool.MathAdd;
        if (input.KeyPressed(Keys.D3)) _tool = Tool.MathMul;
        if (input.KeyPressed(Keys.D4)) _tool = Tool.Splitter;
        if (input.KeyPressed(Keys.D5)) _tool = Tool.Portal;
        if (input.KeyPressed(Keys.R)) _dir = Cw(_dir);
        if (input.KeyPressed(Keys.X)) _editor.Clear();
        if (input.KeyPressed(Keys.L))
        {
            var reference = _scenes.Catalog.LoadReferenceSolution(_levelId);
            if (reference is not null)
            {
                _editor.LoadFrom(reference.Build);
            }
        }

        if ((input.KeyPressed(Keys.Space) || input.KeyPressed(Keys.C) || (input.LeftClick && _compileBtn.Contains(Point(_mouse)))) && _editor.Count > 0)
        {
            Compile();
            return;
        }

        // Palette selection takes priority over placing.
        if (input.LeftClick)
        {
            foreach (var (rect, tool) in _palette)
            {
                if (rect.Contains(Point(_mouse)))
                {
                    _tool = tool;
                    return;
                }
            }
        }

        if (overGrid && input.LeftDown)
        {
            _editor.Place(MakeNode(_tool, cell, _dir));
        }

        if (overGrid && input.RightClick)
        {
            _editor.Remove(cell);
        }
    }

    private void UpdatePlayback(float dt, InputState input)
    {
        int last = (_result?.States.Count ?? 1) - 1;

        if (input.KeyPressed(Keys.Space))
        {
            _playing = !_playing;
        }

        if (input.KeyPressed(Keys.B))
        {
            _mode = PlayMode.Build;
        }

        if (input.KeyPressed(Keys.Left))
        {
            _playing = false;
            _playTime = MathF.Max(0, MathF.Floor(_playTime) - 1);
        }

        if (input.KeyPressed(Keys.Right))
        {
            _playing = false;
            _playTime = MathF.Min(last, MathF.Floor(_playTime) + 1);
        }

        if (input.LeftDown && _timeline.Contains(Point(_mouse)) && last > 0)
        {
            float frac = Math.Clamp((_mouse.X - _timeline.X) / _timeline.Width, 0f, 1f);
            _playTime = frac * last;
            _playing = false;
        }

        if (_playing)
        {
            _playTime += dt * _speed;
            if (_playTime >= last)
            {
                _playTime = last;
                _playing = false;
            }
        }
    }

    private void Compile()
    {
        _result = SimulationCompiler.Compile(_level, _editor.ToBuild());
        _mode = PlayMode.Playback;
        _playTime = 0;
        _playing = true;
    }

    // ---- drawing ----

    public void Draw(Renderer r)
    {
        DrawGrid(r);
        DrawFixedNodes(r);
        DrawPlayerNodes(r);

        if (_mode == PlayMode.Build)
        {
            DrawBuildHud(r);
        }
        else
        {
            DrawItems(r);
            DrawPlaybackHud(r);
        }

        DrawTopBar(r);
    }

    private void DrawGrid(Renderer r)
    {
        for (int y = 0; y < _level.Grid.Height; y++)
        {
            for (int x = 0; x < _level.Grid.Width; x++)
            {
                Vector2 tl = _grid.CellTopLeft(new GridPoint(x, y));
                r.FillRect(tl.X + 1, tl.Y + 1, _grid.CellSize - 2, _grid.CellSize - 2, Palette.Grid);
            }
        }

        if (_mode == PlayMode.Build && _grid.TryScreenToCell(_mouse, out GridPoint hover))
        {
            Vector2 tl = _grid.CellTopLeft(hover);
            Color c = _editor.CanPlace(hover) ? Palette.Accent : Palette.Paradox;
            r.RectOutline(tl.X + 1, tl.Y + 1, _grid.CellSize - 2, _grid.CellSize - 2, 2, c);
        }
    }

    private void DrawFixedNodes(Renderer r)
    {
        foreach (var g in _level.Generators)
        {
            DrawBox(r, g.Position, Palette.Generator, "G");
        }

        foreach (var s in _level.Sinks)
        {
            DrawBox(r, s.Position, Palette.Sink, "S");
        }
    }

    private void DrawPlayerNodes(Renderer r)
    {
        foreach (var node in _editor.Nodes)
        {
            switch (node.Kind)
            {
                case NodeKind.Belt:
                    DrawBelt(r, node.Position, node.Direction);
                    break;
                case NodeKind.Math:
                    DrawBox(r, node.Position, Palette.Math, MathIcon(node.Math!.Operation));
                    break;
                case NodeKind.Splitter:
                    DrawBox(r, node.Position, Palette.Splitter, "Y");
                    break;
                case NodeKind.Portal:
                    DrawPortal(r, node.Position);
                    break;
                default:
                    break;
            }
        }
    }

    private void DrawItems(Renderer r)
    {
        if (_result is null || _result.States.Count == 0)
        {
            return;
        }

        int t = Math.Clamp((int)MathF.Floor(_playTime), 0, _result.States.Count - 1);
        float frac = _playTime - t;
        GridState cur = _result.States[t];
        GridState nxt = _result.States[Math.Min(t + 1, _result.States.Count - 1)];

        float radius = _grid.CellSize * 0.32f;
        foreach (var kv in cur.Items)
        {
            Vector2 from = _grid.CellCenter(kv.Key);
            Vector2 to = from;
            foreach (var n in nxt.Items)
            {
                if (n.Value.Id.Equals(kv.Value.Id))
                {
                    to = _grid.CellCenter(n.Key);
                    break;
                }
            }

            Vector2 pos = Vector2.Lerp(from, to, frac);
            r.Disc(pos, radius, Palette.Item);
            r.TextCentered(kv.Value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), pos, MathF.Max(2f, _grid.CellSize * 0.16f), Palette.ItemText);
        }
    }

    private void DrawBox(Renderer r, GridPoint cell, Color color, string icon)
    {
        Vector2 tl = _grid.CellTopLeft(cell);
        float pad = _grid.CellSize * 0.16f;
        r.FillRect(tl.X + pad, tl.Y + pad, _grid.CellSize - (2 * pad), _grid.CellSize - (2 * pad), color);
        r.TextCentered(icon, _grid.CellCenter(cell), MathF.Max(2f, _grid.CellSize * 0.2f), Palette.ItemText);
    }

    private void DrawBelt(Renderer r, GridPoint cell, Direction dir)
    {
        Vector2 c = _grid.CellCenter(cell);
        Vector2 d = Offset(dir);
        float half = _grid.CellSize * 0.42f;
        Vector2 tail = c - (d * half);
        Vector2 head = c + (d * half);
        r.Line(tail, head, MathF.Max(2f, _grid.CellSize * 0.10f), Palette.Belt);

        // arrowhead
        Vector2 perp = new(-d.Y, d.X);
        float a = _grid.CellSize * 0.16f;
        r.Line(head, head - (d * a) + (perp * a), 3f, Palette.Belt);
        r.Line(head, head - (d * a) - (perp * a), 3f, Palette.Belt);
    }

    private void DrawPortal(Renderer r, GridPoint cell)
    {
        Vector2 c = _grid.CellCenter(cell);
        float radius = _grid.CellSize * 0.34f;
        r.Disc(c, radius, Palette.Portal);
        r.TextCentered("@", c, MathF.Max(2f, _grid.CellSize * 0.2f), Palette.ItemText);
        if (_mode == PlayMode.Playback)
        {
            float pulse = radius + 3f + (MathF.Sin(_playTime * 4f) * 3f);
            r.Ring(c, pulse, 2f, Palette.Portal);
        }
    }

    private void DrawTopBar(Renderer r)
    {
        r.FillRect(0, 0, r.Width, 60, Palette.Panel);
        r.Text(_levelId.ToUpperInvariant(), new Vector2(40, 18), 4f, Palette.Text);
        string mode = _mode == PlayMode.Build ? "BUILD" : "PLAYBACK";
        r.Text("MODE " + mode, new Vector2(r.Width / 2f - 80, 22), 3f, Palette.Accent);
        r.Text("NODES " + _editor.Count, new Vector2(r.Width - 220, 22), 3f, Palette.TextDim);
    }

    private void DrawBuildHud(Renderer r)
    {
        foreach (var (rect, tool) in _palette)
        {
            bool selected = tool == _tool;
            r.FillRect(rect.X, rect.Y, rect.Width, rect.Height, selected ? Palette.PanelHi : Palette.Panel);
            r.RectOutline(rect.X, rect.Y, rect.Width, rect.Height, 2, selected ? Palette.Accent : Palette.GridLine);
            string label = Tools.First(t => t.Tool == tool).Label;
            r.TextCentered(label, new Vector2(rect.Center.X, rect.Center.Y), 2.4f, selected ? Palette.Text : Palette.TextDim);
        }

        var btn = new UiButton(_compileBtn, "COMPILE");
        btn.Draw(r, _mouse);

        r.Text("LEFT-DRAG PLACE   RIGHT-CLICK REMOVE   R ROTATE (" + DirName(_dir) + ")   L LOAD SOLUTION   X CLEAR   SPACE COMPILE   ESC BACK",
            new Vector2(40, r.Height - 30), 2.2f, Palette.TextDim);
    }

    private void DrawPlaybackHud(Renderer r)
    {
        int last = (_result?.States.Count ?? 1) - 1;
        r.FillRect(_timeline.X, _timeline.Y, _timeline.Width, _timeline.Height, Palette.Panel);
        if (last > 0)
        {
            float frac = _playTime / last;
            r.FillRect(_timeline.X, _timeline.Y, _timeline.Width * frac, _timeline.Height, Palette.Accent);
        }

        int curTick = Math.Clamp((int)MathF.Round(_playTime), 0, last);
        r.Text("TICK " + curTick + " / " + last, new Vector2(_timeline.Right + 16, _timeline.Y - 2), 2.6f, Palette.Text);

        if (_result is { } res)
        {
            (string text, Color color) = res.Outcome switch
            {
                LevelOutcome.Solved => ("SOLVED", Palette.Solved),
                LevelOutcome.Paradox => ("PARADOX: " + (res.Error?.Message ?? string.Empty), Palette.Paradox),
                _ => ("NOT SOLVED YET", Palette.Failed),
            };
            r.Text(text, new Vector2(40, r.Height - 132), 3f, color);

            if (res.Outcome == LevelOutcome.Solved)
            {
                int stars = StarRating.Compute(res, _level.Par);
                r.Text("STARS " + new string('*', stars) + new string('.', 3 - stars) + "   TICKS " + res.Stats.FinalTick + "   NODES " + res.Stats.Footprint,
                    new Vector2(40, r.Height - 104), 2.6f, Palette.Text);
            }
        }

        r.Text("SPACE PLAY/PAUSE   LEFT/RIGHT STEP   DRAG TIMELINE   B EDIT   ESC BACK",
            new Vector2(40, r.Height - 30), 2.2f, Palette.TextDim);
    }

    // ---- helpers ----

    private static PlacedNode MakeNode(Tool tool, GridPoint cell, Direction dir) => tool switch
    {
        Tool.Belt => PlacedNode.Belt(cell, dir),
        Tool.MathAdd => PlacedNode.MathOp(cell, new MathConfig { Operation = MathOperation.Add, Output = dir }),
        Tool.MathMul => PlacedNode.MathOp(cell, new MathConfig { Operation = MathOperation.Mul, Output = dir, Constant = 2 }),
        Tool.Splitter => PlacedNode.Split(cell, new SplitterConfig { OutputA = Cw(dir), OutputB = Ccw(dir) }),
        Tool.Portal => PlacedNode.TimePortal(cell, new PortalConfig { TimeOffset = 2, Output = dir }),
        _ => PlacedNode.Belt(cell, dir),
    };

    private static string MathIcon(MathOperation op) => op switch
    {
        MathOperation.Add => "+",
        MathOperation.Sub => "-",
        MathOperation.Mul => "X",
        MathOperation.Div => "/",
        MathOperation.Mod => "%",
        MathOperation.Min => "V",
        MathOperation.Max => "^",
        _ => "?",
    };

    private static Direction Cw(Direction d) => (Direction)(((int)d + 1) % 4);

    private static Direction Ccw(Direction d) => (Direction)(((int)d + 3) % 4);

    private static string DirName(Direction d) => d.ToString().ToUpperInvariant();

    private static Vector2 Offset(Direction d) => d switch
    {
        Direction.Up => new Vector2(0, -1),
        Direction.Right => new Vector2(1, 0),
        Direction.Down => new Vector2(0, 1),
        Direction.Left => new Vector2(-1, 0),
        _ => Vector2.Zero,
    };

    private static Point Point(Vector2 v) => new((int)v.X, (int)v.Y);
}
