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
    Filter,
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
        (Tool.MathMul, "3 MUL X2"),
        (Tool.Splitter, "4 SPLIT"),
        (Tool.Portal, "5 PORTAL"),
        (Tool.Filter, "6 FILTER"),
    };

    private readonly SceneManager _scenes;
    private readonly string _levelId;
    private readonly LevelDefinition _level;
    private readonly BuildEditor _editor;
    private readonly GridView _grid;
    private readonly Rectangle _timeline;
    private readonly Rectangle _compileBtn;
    private readonly List<(Rectangle Rect, Tool Tool)> _palette = [];
    private readonly string _startInfo;
    private readonly string _goalInfo;

    private Tool _tool = Tool.Belt;
    private Direction _dir = Direction.Right;
    private PlayMode _mode = PlayMode.Build;
    private SimulationResult? _result;
    private float _playTime;
    private bool _playing;
    private readonly float _speed = 3.5f;
    private Vector2 _mouse;

    public GameplayScene(SceneManager scenes, string levelId)
    {
        _scenes = scenes;
        _levelId = levelId;
        _level = scenes.Catalog.LoadLevel(levelId);
        _editor = new BuildEditor(_level);

        int w = scenes.ScreenW;
        int h = scenes.ScreenH;
        var playArea = new Rectangle(40, 108, w - 80, h - 108 - 150);
        _grid = new GridView(_level.Grid, playArea);
        _timeline = new Rectangle(40, h - 64, w - 280, 18);
        _compileBtn = new Rectangle(w - 220, h - 132, 180, 48);

        int px = 40;
        foreach (var (tool, _) in Tools)
        {
            _palette.Add((new Rectangle(px, h - 132, 150, 48), tool));
            px += 158;
        }

        _startInfo = _level.Generators.Count == 0
            ? "-"
            : string.Join("    ", _level.Generators.Select(GeneratorValues));
        _goalInfo = _level.Sinks.Count == 0
            ? "-"
            : string.Join("    ", _level.Sinks.Select(SinkValues));
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
        if (input.KeyPressed(Keys.D6)) _tool = Tool.Filter;
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

        if (overGrid && input.LeftDown && CanPlaceTool(_tool, cell))
        {
            _editor.Place(MakeNode(_tool, cell, _dir));
        }

        if (overGrid && input.RightClick)
        {
            _editor.Remove(cell);
        }
    }

    private bool CanPlaceTool(Tool tool, GridPoint cell)
    {
        string id = DefId(tool);
        LevelInventory? inv = _level.Inventory;
        if (inv is not null && !inv.Allows(id))
        {
            return false;
        }

        int limit = inv?.LimitFor(id) ?? int.MaxValue;
        // Placing onto a cell already holding this def replaces it (count unchanged).
        int used = CountDef(id);
        if (_editor.At(cell) is { } existing && DefOf(existing) == id)
        {
            used--;
        }

        return used < limit;
    }

    private void UpdatePlayback(float dt, InputState input)
    {
        int last = (_result?.States.Count ?? 1) - 1;

        if (input.KeyPressed(Keys.Space)) _playing = !_playing;
        if (input.KeyPressed(Keys.B)) _mode = PlayMode.Build;

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
            DrawNodeCell(r, g.Position, Palette.Generator, "G", GeneratorValues(g));
            DrawOutArrow(r, g.Position, g.Output, Palette.Generator);
        }

        foreach (var s in _level.Sinks)
        {
            DrawNodeCell(r, s.Position, Palette.Sink, "S", SinkValues(s));
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
                    DrawNodeCell(r, node.Position, Palette.Math, MathIcon(node.Math!.Operation), node.Math!.Constant?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
                    DrawOutArrow(r, node.Position, node.Math!.Output, Palette.Math);
                    break;
                case NodeKind.Splitter:
                    DrawNodeCell(r, node.Position, Palette.Splitter, "Y", string.Empty);
                    break;
                case NodeKind.Portal:
                    DrawPortal(r, node.Position, node.Portal!);
                    break;
                case NodeKind.Filter:
                    DrawNodeCell(r, node.Position, Palette.Filter, "F", CompSym(node.Filter!.Comparison) + node.Filter!.Constant.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    DrawOutArrow(r, node.Position, node.Filter!.Output, Palette.Filter);
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

        float radius = _grid.CellSize * 0.3f;
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
            r.TextCenteredFit(kv.Value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), pos, radius * 1.3f, radius * 1.1f, Palette.ItemText);
        }
    }

    private void DrawNodeCell(Renderer r, GridPoint cell, Color color, string icon, string sub)
    {
        Vector2 tl = _grid.CellTopLeft(cell);
        float s = _grid.CellSize;
        float pad = s * 0.14f;
        r.FillRect(tl.X + pad, tl.Y + pad, s - (2 * pad), s - (2 * pad), Palette.Panel);
        r.RectOutline(tl.X + pad, tl.Y + pad, s - (2 * pad), s - (2 * pad), MathF.Max(2f, s * 0.03f), color);

        Vector2 c = _grid.CellCenter(cell);
        if (string.IsNullOrEmpty(sub))
        {
            r.TextCenteredFit(icon, c, s * 0.4f, s * 0.4f, color);
        }
        else
        {
            r.TextCenteredFit(icon, c - new Vector2(0, s * 0.16f), s * 0.4f, s * 0.26f, color);
            r.TextCenteredFit(sub, c + new Vector2(0, s * 0.2f), s * 0.78f, s * 0.24f, Palette.Item);
        }
    }

    private void DrawBelt(Renderer r, GridPoint cell, Direction dir)
    {
        Vector2 c = _grid.CellCenter(cell);
        Vector2 d = Offset(dir);
        float half = _grid.CellSize * 0.4f;
        r.Line(c - (d * half), c + (d * half), MathF.Max(2f, _grid.CellSize * 0.08f), Palette.Belt);

        Vector2 head = c + (d * half);
        Vector2 perp = new(-d.Y, d.X);
        float a = _grid.CellSize * 0.15f;
        r.Line(head, head - (d * a) + (perp * a), 3f, Palette.Belt);
        r.Line(head, head - (d * a) - (perp * a), 3f, Palette.Belt);
    }

    private void DrawOutArrow(Renderer r, GridPoint cell, Direction dir, Color color)
    {
        Vector2 c = _grid.CellCenter(cell);
        Vector2 d = Offset(dir);
        float s = _grid.CellSize;
        Vector2 tip = c + (d * s * 0.46f);
        Vector2 baseP = c + (d * s * 0.34f);
        Vector2 perp = new(-d.Y, d.X);
        float a = s * 0.07f;
        r.Line(baseP, tip, 3f, color);
        r.Line(tip, tip - (d * a) + (perp * a), 3f, color);
        r.Line(tip, tip - (d * a) - (perp * a), 3f, color);
    }

    private void DrawPortal(Renderer r, GridPoint cell, PortalConfig config)
    {
        Vector2 c = _grid.CellCenter(cell);
        float radius = _grid.CellSize * 0.34f;
        r.Disc(c, radius, Palette.Panel);
        r.Ring(c, radius, MathF.Max(2f, _grid.CellSize * 0.03f), Palette.Portal);
        r.TextCenteredFit("@", c, radius, radius, Palette.Portal);
        DrawOutArrow(r, cell, config.Output, Palette.Portal);
        if (_mode == PlayMode.Playback)
        {
            float pulse = radius + 4f + (MathF.Sin(_playTime * 4f) * 3f);
            r.Ring(c, pulse, 2f, Palette.Portal);
        }
    }

    private void DrawTopBar(Renderer r)
    {
        r.FillRect(0, 0, r.Width, 56, Palette.Panel);
        r.Text(_levelId.ToUpperInvariant(), new Vector2(40, 18), 3.5f, Palette.Text);
        string mode = _mode == PlayMode.Build ? "BUILD" : "PLAYBACK";
        r.TextCentered("MODE " + mode, new Vector2(r.Width / 2f, 28), 3f, Palette.Accent);
        r.Text("NODES " + _editor.Count, new Vector2(r.Width - 200, 22), 3f, Palette.TextDim);

        // Objective strip
        r.FillRect(0, 56, r.Width, 48, Palette.Background);
        r.Text("START", new Vector2(40, 70), 2.4f, Palette.Generator);
        r.Text(_startInfo, new Vector2(120, 68), 3.2f, Palette.Item);
        float gx = r.Width / 2f + 40;
        r.Text("GOAL", new Vector2(gx, 70), 2.4f, Palette.Sink);
        r.Text(_goalInfo, new Vector2(gx + 70, 68), 3.2f, Palette.Item);
    }

    private void DrawBuildHud(Renderer r)
    {
        foreach (var (rect, tool) in _palette)
        {
            string id = DefId(tool);
            bool allowed = _level.Inventory?.Allows(id) ?? true;
            int limit = _level.Inventory?.LimitFor(id) ?? int.MaxValue;
            int used = CountTool(tool);
            bool selected = tool == _tool;

            r.FillRect(rect.X, rect.Y, rect.Width, rect.Height, selected ? Palette.PanelHi : Palette.Panel);
            r.RectOutline(rect.X, rect.Y, rect.Width, rect.Height, 2, selected ? Palette.Accent : Palette.GridLine);

            string label = Tools.First(t => t.Tool == tool).Label;
            r.TextCentered(label, new Vector2(rect.Center.X, rect.Center.Y - 6), 2.2f, allowed ? (selected ? Palette.Text : Palette.TextDim) : Palette.Paradox);

            string countText = !allowed ? "BLOCKED" : (limit == int.MaxValue ? used + " USED" : used + " / " + limit);
            Color countColor = !allowed ? Palette.Paradox : (limit != int.MaxValue && used >= limit ? Palette.Failed : Palette.TextDim);
            r.TextCentered(countText, new Vector2(rect.Center.X, rect.Center.Y + 12), 1.8f, countColor);
        }

        var btn = new UiButton(_compileBtn, "COMPILE");
        btn.Draw(r, _mouse);

        r.Text("LEFT-DRAG PLACE   RIGHT REMOVE   R ROTATE (" + DirName(_dir) + ")   L LOAD SOLUTION   X CLEAR   SPACE COMPILE   ESC BACK",
            new Vector2(40, r.Height - 28), 2f, Palette.TextDim);
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
                LevelOutcome.Solved => ("SOLVED!", Palette.Solved),
                LevelOutcome.Paradox => ("PARADOX: " + (res.Error?.Message ?? string.Empty), Palette.Paradox),
                _ => ("NOT SOLVED - WRONG OR MISSING OUTPUT", Palette.Failed),
            };
            r.Text(text, new Vector2(40, r.Height - 132), 3f, color);

            if (res.Outcome == LevelOutcome.Solved)
            {
                int stars = StarRating.Compute(res, _level.Par);
                r.Text("STARS " + new string('*', stars) + new string('.', 3 - stars) + "    TICKS " + res.Stats.FinalTick + "    NODES " + res.Stats.Footprint,
                    new Vector2(40, r.Height - 104), 2.6f, Palette.Text);
            }
        }

        r.Text("SPACE PLAY/PAUSE   LEFT/RIGHT STEP   DRAG TIMELINE   B EDIT   ESC BACK",
            new Vector2(40, r.Height - 28), 2f, Palette.TextDim);
    }

    // ---- helpers ----

    private int CountTool(Tool tool) => _editor.Nodes.Count(n => ToolOf(n) == tool);

    private int CountDef(string id) => _editor.Nodes.Count(n => DefOf(n) == id);

    private static string DefId(Tool tool) => tool switch
    {
        Tool.Belt => "node_belt",
        Tool.MathAdd => "node_math_add",
        Tool.MathMul => "node_math_mul",
        Tool.Splitter => "node_splitter",
        Tool.Portal => "node_portal",
        Tool.Filter => "node_filter",
        _ => "node_belt",
    };

    private static string DefOf(PlacedNode n) => DefId(ToolOf(n));

    private static Tool ToolOf(PlacedNode n) => n.Kind switch
    {
        NodeKind.Belt => Tool.Belt,
        NodeKind.Splitter => Tool.Splitter,
        NodeKind.Portal => Tool.Portal,
        NodeKind.Filter => Tool.Filter,
        NodeKind.Math => n.Math!.Constant.HasValue ? Tool.MathMul : Tool.MathAdd,
        _ => Tool.Belt,
    };

    private static string GeneratorValues(GeneratorSpec g) =>
        string.Join(" ", g.Schedule.OrderBy(static s => s.Tick).Select(static s => s.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    private static string SinkValues(SinkSpec s) =>
        s.Expected.Count == 0 ? "-" : string.Join(" ", s.Expected.Select(static v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    private static PlacedNode MakeNode(Tool tool, GridPoint cell, Direction dir) => tool switch
    {
        Tool.Belt => PlacedNode.Belt(cell, dir),
        Tool.MathAdd => PlacedNode.MathOp(cell, new MathConfig { Operation = MathOperation.Add, Output = dir }),
        Tool.MathMul => PlacedNode.MathOp(cell, new MathConfig { Operation = MathOperation.Mul, Output = dir, Constant = 2 }),
        Tool.Splitter => PlacedNode.Split(cell, new SplitterConfig { OutputA = Cw(dir), OutputB = Ccw(dir) }),
        Tool.Portal => PlacedNode.TimePortal(cell, new PortalConfig { TimeOffset = 2, Output = dir }),
        Tool.Filter => PlacedNode.Gate(cell, new FilterConfig { Comparison = Comparison.Ge, Constant = 1, Output = dir }),
        _ => PlacedNode.Belt(cell, dir),
    };

    private static string CompSym(Comparison c) => c switch
    {
        Comparison.Eq => "=",
        Comparison.Ne => "!=",
        Comparison.Lt => "<",
        Comparison.Le => "<=",
        Comparison.Gt => ">",
        Comparison.Ge => ">=",
        _ => "?",
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
