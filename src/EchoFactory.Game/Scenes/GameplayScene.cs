using EchoFactory.Content;
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
    Router,
    Accumulator,
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
        (Tool.MathMul, "3 MUL"),
        (Tool.Splitter, "4 SPLIT"),
        (Tool.Portal, "5 PORTAL"),
        (Tool.Filter, "6 FILTER"),
        (Tool.Router, "7 ROUTER"),
        (Tool.Accumulator, "8 SUM"),
    };

    private readonly SceneManager _scenes;
    private readonly string _levelId;
    private readonly LevelDefinition _level;
    private readonly IScene? _returnTo;
    private readonly BuildEditor _editor;
    private readonly GridView _grid;
    private readonly Rectangle _timeline;
    private readonly Rectangle _compileBtn;
    private readonly Rectangle _panelRect;
    private readonly Rectangle _helpBtn;
    private readonly Rectangle _cardRect;
    private readonly Rectangle _retryBtn;
    private readonly Rectangle _nextBtn;
    private readonly Rectangle _levelsBtn;
    private readonly List<(Rectangle Rect, Tool Tool)> _palette = [];
    private readonly string _startInfo;
    private readonly string _goalInfo;

    private Tool _tool = Tool.Belt;
    private Direction _dir = Direction.Right;
    private int _mulConstant = 2;
    private int _portalOffset = 2;
    private int _filterConstant = 1;
    private Comparison _filterComparison = Comparison.Ge;
    private int _routerConstant = 5;
    private Comparison _routerComparison = Comparison.Ge;
    private int _accConstant = 10;
    private Comparison _accComparison = Comparison.Ge;
    private PlayMode _mode = PlayMode.Build;
    private GridPoint? _selected;
    private bool _showHelp;
    private SimulationResult? _result;
    private SubmitOutcome? _submit;
    private float _playTime;
    private bool _playing;
    private int _lastSoundTick = -1;
    private bool _resultSoundPlayed;
    private Vector2 _mouse;

    public GameplayScene(SceneManager scenes, string levelId)
        : this(scenes, levelId, scenes.Catalog.LoadLevel(levelId))
    {
    }

    public GameplayScene(SceneManager scenes, string levelId, LevelDefinition level, IScene? returnTo = null)
    {
        _scenes = scenes;
        _levelId = levelId;
        _level = level;
        _returnTo = returnTo;
        _editor = new BuildEditor(_level);

        int w = scenes.ScreenW;
        int h = scenes.ScreenH;
        // Reserve a right-hand strip for the config panel so it never overlaps the grid.
        _panelRect = new Rectangle(w - 296, 120, 256, 332);
        var playArea = new Rectangle(40, 108, _panelRect.X - 40 - 16, h - 108 - 150);
        _grid = new GridView(_level.Grid, playArea);
        _timeline = new Rectangle(40, h - 64, w - 280, 18);
        _compileBtn = new Rectangle(w - 220, h - 132, 180, 48);
        _helpBtn = new Rectangle(w - 150, 12, 110, 32);

        const int cardW = 580;
        const int cardH = 300;
        _cardRect = new Rectangle((w - cardW) / 2, (h - cardH) / 2, cardW, cardH);
        int by = _cardRect.Bottom - 72;
        _retryBtn = new Rectangle(_cardRect.X + 30, by, 160, 48);
        _nextBtn = new Rectangle(_cardRect.X + 210, by, 160, 48);
        _levelsBtn = new Rectangle(_cardRect.X + 390, by, 160, 48);

        int px = 40;
        foreach (var (tool, _) in Tools)
        {
            _palette.Add((new Rectangle(px, h - 132, 118, 48), tool));
            px += 124;
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

        // Help overlay is modal: while open it swallows input.
        if (input.KeyPressed(Keys.H))
        {
            _showHelp = !_showHelp;
            _scenes.Play(Sfx.Click);
            return;
        }

        if (_showHelp)
        {
            if (input.KeyPressed(Keys.Escape) || input.LeftClick)
            {
                _showHelp = false;
            }

            return;
        }

        if (input.LeftClick && _helpBtn.Contains(Point(_mouse)))
        {
            _showHelp = true;
            _scenes.Play(Sfx.Click);
            return;
        }

        if (input.KeyPressed(Keys.Escape))
        {
            if (_mode == PlayMode.Playback)
            {
                _mode = PlayMode.Build;
            }
            else if (_selected is not null)
            {
                _selected = null;
            }
            else
            {
                _scenes.Switch(_returnTo ?? new CampaignSelectScene(_scenes));
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
        bool ctrl = input.KeyDown(Keys.LeftControl) || input.KeyDown(Keys.RightControl);
        if (ctrl && input.KeyPressed(Keys.Z))
        {
            if (_editor.Undo()) _scenes.Play(Sfx.Click);
            return;
        }

        if (ctrl && input.KeyPressed(Keys.Y))
        {
            if (_editor.Redo()) _scenes.Play(Sfx.Click);
            return;
        }

        if (input.KeyPressed(Keys.D1)) _tool = Tool.Belt;
        if (input.KeyPressed(Keys.D2)) _tool = Tool.MathAdd;
        if (input.KeyPressed(Keys.D3)) _tool = Tool.MathMul;
        if (input.KeyPressed(Keys.D4)) _tool = Tool.Splitter;
        if (input.KeyPressed(Keys.D5)) _tool = Tool.Portal;
        if (input.KeyPressed(Keys.D6)) _tool = Tool.Filter;
        if (input.KeyPressed(Keys.D7)) _tool = Tool.Router;
        if (input.KeyPressed(Keys.D8)) _tool = Tool.Accumulator;
        if (input.KeyPressed(Keys.R)) _dir = Cw(_dir);

        if (input.KeyPressed(Keys.OemPlus) || input.KeyPressed(Keys.Add)) AdjustConfig(+1);
        if (input.KeyPressed(Keys.OemMinus) || input.KeyPressed(Keys.Subtract)) AdjustConfig(-1);
        if (input.KeyPressed(Keys.Tab))
        {
            if (_tool == Tool.Filter) _filterComparison = (Comparison)(((int)_filterComparison + 1) % 6);
            if (_tool == Tool.Router) _routerComparison = (Comparison)(((int)_routerComparison + 1) % 6);
            if (_tool == Tool.Accumulator) _accComparison = (Comparison)(((int)_accComparison + 1) % 6);
        }

        if (input.KeyPressed(Keys.X) && _editor.Clear()) _scenes.Play(Sfx.Remove);
        if (input.KeyPressed(Keys.L))
        {
            var reference = _scenes.Catalog.LoadReferenceSolution(_levelId);
            if (reference is not null)
            {
                _editor.LoadFrom(reference.Build);
                _scenes.Play(Sfx.Place);
            }
        }

        if ((input.KeyPressed(Keys.Space) || (input.LeftClick && _compileBtn.Contains(Point(_mouse)))) && _editor.Count > 0)
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
                    _scenes.Play(Sfx.Click);
                    return;
                }
            }
        }

        // Per-node config panel: while a node is selected, its panel intercepts clicks.
        if (_selected is { } sel && _editor.At(sel) is { } selNode)
        {
            if (input.LeftClick)
            {
                foreach (var pc in PanelControls(selNode, sel))
                {
                    if (pc.Rect.Contains(Point(_mouse)))
                    {
                        ApplyPanel(pc, sel);
                        return;
                    }
                }

                if (_panelRect.Contains(Point(_mouse)))
                {
                    return; // swallow clicks on the panel background
                }
            }
        }
        else
        {
            _selected = null; // selected node was removed/undone
        }

        // Click a placed node to edit it; drag on empty cells to place.
        if (overGrid && input.LeftClick && _editor.At(cell) is not null)
        {
            _selected = cell;
            _scenes.Play(Sfx.Click);
            return;
        }

        if (overGrid && input.LeftDown && _editor.At(cell) is null && CanPlaceTool(_tool, cell) && _editor.Place(MakeNode(cell)))
        {
            _selected = cell;
            _scenes.Play(Sfx.Place);
        }

        if (overGrid && input.RightClick && _editor.Remove(cell))
        {
            if (_selected == cell)
            {
                _selected = null;
            }

            _scenes.Play(Sfx.Remove);
        }
    }

    private void ApplyPanel(PanelControl ctrl, GridPoint pos)
    {
        if (ctrl.Delete)
        {
            if (_editor.Remove(pos))
            {
                _selected = null;
                _scenes.Play(Sfx.Remove);
            }

            return;
        }

        if (ctrl.Result is { } updated && _editor.Place(updated))
        {
            _scenes.Play(Sfx.Click);
        }
    }

    private void AdjustConfig(int delta)
    {
        switch (_tool)
        {
            case Tool.MathMul:
                _mulConstant = Math.Clamp(_mulConstant + delta, -9, 9);
                break;
            case Tool.Portal:
                _portalOffset = Math.Clamp(_portalOffset + delta, -9, 9);
                if (_portalOffset == 0)
                {
                    _portalOffset = delta > 0 ? 1 : -1;
                }

                break;
            case Tool.Filter:
                _filterConstant += delta;
                break;
            case Tool.Router:
                _routerConstant += delta;
                break;
            case Tool.Accumulator:
                _accConstant += delta;
                break;
            default:
                break;
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

        // Results card buttons (shown once the playback has finished).
        if (_playTime >= last && input.LeftClick && _result is { } res)
        {
            if (_retryBtn.Contains(Point(_mouse)))
            {
                _mode = PlayMode.Build;
                return;
            }

            if (_levelsBtn.Contains(Point(_mouse)))
            {
                _scenes.Switch(_returnTo ?? new CampaignSelectScene(_scenes));
                return;
            }

            if (res.Outcome == LevelOutcome.Solved && NextLevelId() is { } next && _nextBtn.Contains(Point(_mouse)))
            {
                _scenes.Switch(new GameplayScene(_scenes, next));
                return;
            }
        }

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
            _playTime += dt * _scenes.Settings.PlaybackSpeed;
            if (_playTime >= last)
            {
                _playTime = last;
                _playing = false;
            }
        }

        if (_result is { } playback)
        {
            int curTick = Math.Clamp((int)MathF.Floor(_playTime), 0, last);
            if (curTick != _lastSoundTick)
            {
                _lastSoundTick = curTick;
                if (curTick < playback.States.Count && playback.States[curTick].Events.Any(static e => e.Kind == VisualEventKind.Consume))
                {
                    _scenes.Play(Sfx.Deliver);
                }
            }

            if (!_resultSoundPlayed && _playTime >= last)
            {
                _resultSoundPlayed = true;
                _scenes.Play(playback.Outcome switch
                {
                    LevelOutcome.Solved => Sfx.Solved,
                    LevelOutcome.Paradox => Sfx.Paradox,
                    _ => Sfx.Click,
                });
            }
        }
    }

    private void Compile()
    {
        _result = SimulationCompiler.Compile(_level, _editor.ToBuild());
        _mode = PlayMode.Playback;
        _playTime = 0;
        _playing = true;
        _lastSoundTick = -1;
        _resultSoundPlayed = false;

        _submit = null;
        if (_result.Outcome == LevelOutcome.Solved)
        {
            int stars = StarRating.Compute(_result, _level.Par);
            _submit = _scenes.Leaderboard.Submit(_levelId, _result.Stats.FinalTick, _result.Stats.Footprint, stars);
        }

        _scenes.Play(Sfx.Compile);
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
            DrawConfigPanel(r);
        }
        else
        {
            DrawItems(r);
            DrawPlaybackHud(r);
        }

        DrawTopBar(r);

        if (_showHelp)
        {
            DrawHelpOverlay(r);
        }
    }

    private void DrawGrid(Renderer r)
    {
        if (_scenes.Settings.ShowGrid)
        {
            for (int y = 0; y < _level.Grid.Height; y++)
            {
                for (int x = 0; x < _level.Grid.Width; x++)
                {
                    Vector2 tl = _grid.CellTopLeft(new GridPoint(x, y));
                    r.FillRect(tl.X + 1, tl.Y + 1, _grid.CellSize - 2, _grid.CellSize - 2, Palette.Grid);
                }
            }
        }

        if (_mode == PlayMode.Build && _grid.TryScreenToCell(_mouse, out GridPoint hover))
        {
            Vector2 tl = _grid.CellTopLeft(hover);
            bool ok = _editor.CanPlace(hover) && CanPlaceTool(_tool, hover);
            r.RectOutline(tl.X + 1, tl.Y + 1, _grid.CellSize - 2, _grid.CellSize - 2, 2, ok ? Palette.Accent : Palette.Paradox);
            if (ok)
            {
                DrawGhost(r, hover);
            }
        }
    }

    private void DrawGhost(Renderer r, GridPoint cell)
    {
        float s = _grid.CellSize;
        Color col = ToolColor(_tool);
        if (_tool == Tool.Belt)
        {
            DrawBeltChevrons(r, _grid.CellCenter(cell), _dir, new Color(col.R, col.G, col.B, (byte)120));
            return;
        }

        Vector2 tl = _grid.CellTopLeft(cell);
        float pad = s * 0.14f;
        r.RectOutline(tl.X + pad, tl.Y + pad, s - (2 * pad), s - (2 * pad), 2, col);
        r.TextCenteredFit(ToolIcon(_tool), _grid.CellCenter(cell), s * 0.4f, s * 0.4f, col);
        DrawOutArrow(r, cell, _dir, col);
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
                    DrawInPorts(r, node.Position, Palette.Math, node.Math!.Output);
                    DrawOutArrow(r, node.Position, node.Math!.Output, Palette.Math);
                    break;
                case NodeKind.Splitter:
                    DrawNodeCell(r, node.Position, Palette.Splitter, "Y", string.Empty);
                    DrawInPorts(r, node.Position, Palette.Splitter, node.Splitter!.OutputA, node.Splitter!.OutputB);
                    DrawOutArrow(r, node.Position, node.Splitter!.OutputA, Palette.Splitter);
                    DrawOutArrow(r, node.Position, node.Splitter!.OutputB, Palette.Splitter);
                    break;
                case NodeKind.Portal:
                    DrawPortal(r, node.Position, node.Portal!);
                    break;
                case NodeKind.Filter:
                    DrawNodeCell(r, node.Position, Palette.Filter, "F", CompSym(node.Filter!.Comparison) + node.Filter!.Constant.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    DrawInPorts(r, node.Position, Palette.Filter, node.Filter!.Output);
                    DrawOutArrow(r, node.Position, node.Filter!.Output, Palette.Filter);
                    break;
                case NodeKind.Router:
                    DrawNodeCell(r, node.Position, Palette.Router, "R", CompSym(node.Router!.Comparison) + node.Router!.Constant.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    DrawInPorts(r, node.Position, Palette.Router, node.Router!.OutMatch, node.Router!.OutElse);
                    DrawOutArrow(r, node.Position, node.Router!.OutMatch, Palette.Router);
                    DrawOutArrow(r, node.Position, node.Router!.OutElse, Palette.TextDim);
                    break;
                case NodeKind.Accumulator:
                    DrawNodeCell(r, node.Position, Palette.Accumulator, "A", CompSym(node.Accumulator!.ReleaseWhen) + node.Accumulator!.Constant.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    DrawInPorts(r, node.Position, Palette.Accumulator, node.Accumulator!.Output);
                    DrawOutArrow(r, node.Position, node.Accumulator!.Output, Palette.Accumulator);
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

    private void DrawBelt(Renderer r, GridPoint cell, Direction dir) => DrawBeltChevrons(r, _grid.CellCenter(cell), dir, Palette.Belt);

    private void DrawBeltChevrons(Renderer r, Vector2 c, Direction dir, Color color)
    {
        Vector2 d = Offset(dir);
        Vector2 perp = new(-d.Y, d.X);
        float s = _grid.CellSize;

        // faint track + three flowing chevrons pointing along the belt
        r.Line(c - (d * (s * 0.46f)), c + (d * (s * 0.46f)), MathF.Max(2f, s * 0.045f), Palette.GridLine);

        float chev = s * 0.13f;
        float thick = MathF.Max(3f, s * 0.055f);
        for (int i = -1; i <= 1; i++)
        {
            Vector2 mid = c + (d * (i * s * 0.24f));
            Vector2 tip = mid + (d * chev);
            r.Line(tip, mid - (perp * chev), thick, color);
            r.Line(tip, mid + (perp * chev), thick, color);
        }
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

    /// <summary>Faint inward chevrons on every side that is not an output — "items flow in here".</summary>
    private void DrawInPorts(Renderer r, GridPoint cell, Color color, params Direction[] outputs)
    {
        Vector2 c = _grid.CellCenter(cell);
        float s = _grid.CellSize;
        var tint = new Color(color.R, color.G, color.B, (byte)80);
        float thick = MathF.Max(2f, s * 0.028f);
        foreach (Direction dir in AllDirs)
        {
            if (Array.IndexOf(outputs, dir) >= 0)
            {
                continue;
            }

            Vector2 d = Offset(dir);
            Vector2 perp = new(-d.Y, d.X);
            Vector2 edge = c + (d * s * 0.46f);
            Vector2 tip = edge - (d * (s * 0.12f)); // points inward, toward the cell centre
            float a = s * 0.05f;
            r.Line(edge + (perp * a), tip, thick, tint);
            r.Line(edge - (perp * a), tip, thick, tint);
        }
    }

    private void DrawPortal(Renderer r, GridPoint cell, PortalConfig config)
    {
        Vector2 c = _grid.CellCenter(cell);
        float s = _grid.CellSize;
        float radius = s * 0.34f;
        r.Disc(c, radius, Palette.Panel);
        r.Ring(c, radius, MathF.Max(2f, s * 0.03f), Palette.Portal);
        r.TextCenteredFit("@", c - new Vector2(0, s * 0.12f), radius, radius * 0.7f, Palette.Portal);

        // Time offset is the whole point of the portal — show it as t+N / t-N.
        int off = config.TimeOffset;
        string label = "t" + (off >= 0 ? "+" : "-") + Math.Abs(off).ToString(System.Globalization.CultureInfo.InvariantCulture);
        r.TextCenteredFit(label, c + new Vector2(0, s * 0.22f), s * 0.6f, s * 0.2f, Palette.Item);

        DrawInPorts(r, cell, Palette.Portal, config.Output);
        DrawOutArrow(r, cell, config.Output, Palette.Portal);

        // Echo rings hint at the value duplicated across time; a comet rides them during playback.
        r.Ring(c, radius + 4f, 1.5f, new Color(Palette.Portal.R, Palette.Portal.G, Palette.Portal.B, (byte)90));
        if (_mode == PlayMode.Playback)
        {
            float pulse = radius + 6f + (MathF.Sin(_playTime * 4f) * 3f);
            r.Ring(c, pulse, 2f, Palette.Portal);
            float ang = _playTime * 5f;
            Vector2 comet = c + (new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (radius + 6f));
            r.Disc(comet, MathF.Max(2f, s * 0.05f), Palette.Item);
        }
    }

    private void DrawTopBar(Renderer r)
    {
        string name = _returnTo is not null ? "EDITOR TEST" : (string.IsNullOrEmpty(_level.Name) ? _levelId : _level.Name);
        r.FillRect(0, 0, r.Width, 56, Palette.Panel);
        r.Text(name.ToUpperInvariant(), new Vector2(40, 18), 3.5f, Palette.Text);
        string mode = _mode == PlayMode.Build ? "BUILD" : "PLAYBACK";
        r.TextCentered("MODE " + mode, new Vector2(r.Width / 2f, 28), 3f, Palette.Accent);
        if (_returnTo is not null)
        {
            r.Text("ESC = BACK TO EDITOR", new Vector2(r.Width / 2f + 130, 22), 2.1f, Palette.Sink);
        }

        r.Text("NODES " + _editor.Count, new Vector2(r.Width - 330, 22), 3f, Palette.TextDim);
        new UiButton(_helpBtn, "? HELP").Draw(r, _mouse);

        // Objective strip: hint + start/goal
        r.FillRect(0, 56, r.Width, 48, Palette.Background);
        if (!string.IsNullOrEmpty(_level.Description))
        {
            r.Text(_level.Description.ToUpperInvariant(), new Vector2(40, 60), 2.2f, Palette.Accent);
        }

        r.Text("START", new Vector2(40, 84), 2.2f, Palette.Generator);
        r.Text(_startInfo, new Vector2(112, 82), 2.8f, Palette.Item);
        float gx = (r.Width / 2f) + 40;
        r.Text("GOAL", new Vector2(gx, 84), 2.2f, Palette.Sink);
        r.Text(_goalInfo, new Vector2(gx + 64, 82), 2.8f, Palette.Item);
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
            r.TextCenteredFit(label, new Vector2(rect.Center.X, rect.Center.Y - 6), rect.Width - 12, 18, allowed ? (selected ? Palette.Text : Palette.TextDim) : Palette.Paradox);

            string countText = !allowed ? "BLOCKED" : (limit == int.MaxValue ? used + " USED" : used + " / " + limit);
            Color countColor = !allowed ? Palette.Paradox : (limit != int.MaxValue && used >= limit ? Palette.Failed : Palette.TextDim);
            r.TextCentered(countText, new Vector2(rect.Center.X, rect.Center.Y + 12), 1.8f, countColor);
        }

        var btn = new UiButton(_compileBtn, "COMPILE");
        btn.Draw(r, _mouse);

        r.Text("TOOL: " + ConfigLabel() + "      +/- ADJUST   TAB CYCLE   R ROTATE (" + DirName(_dir) + ")",
            new Vector2(40, r.Height - 50), 2f, Palette.Accent);
        r.Text("DRAG EMPTY CELLS = PLACE   CLICK A NODE = CONFIG PANEL   RIGHT REMOVE   CTRL+Z/Y UNDO   L LOAD   X CLEAR   SPACE COMPILE",
            new Vector2(40, r.Height - 28), 2f, Palette.TextDim);
    }

    private string ConfigLabel() => _tool switch
    {
        Tool.Belt => "BELT " + DirName(_dir),
        Tool.MathAdd => "ADD (two inputs)",
        Tool.MathMul => "MUL x" + _mulConstant,
        Tool.Splitter => "SPLITTER",
        Tool.Portal => "PORTAL offset " + _portalOffset,
        Tool.Filter => "FILTER " + CompSym(_filterComparison) + _filterConstant,
        Tool.Router => "ROUTER " + CompSym(_routerComparison) + _routerConstant + " -> " + DirName(_dir) + " ELSE " + DirName(Opp(_dir)),
        Tool.Accumulator => "SUM release " + CompSym(_accComparison) + _accConstant + " -> " + DirName(_dir),
        _ => string.Empty,
    };

    // ---- per-node config panel ----

    private readonly record struct PanelControl(Rectangle Rect, string Label, PlacedNode? Result, bool Delete);

    private List<PanelControl> PanelControls(PlacedNode n, GridPoint p)
    {
        var raw = new List<(string Label, PlacedNode? Node, bool Del)>();

        switch (n.Kind)
        {
            case NodeKind.Belt:
                raw.Add(("DIR <", PlacedNode.Belt(p, Ccw(n.Direction)), false));
                raw.Add(("DIR >", PlacedNode.Belt(p, Cw(n.Direction)), false));
                break;
            case NodeKind.Math:
                var m = n.Math!;
                if (m.Constant.HasValue)
                {
                    raw.Add(("VAL -", WithMath(p, m.Operation, m.Output, NudgeNonZero(m.Constant.Value, -1)), false));
                    raw.Add(("VAL +", WithMath(p, m.Operation, m.Output, NudgeNonZero(m.Constant.Value, +1)), false));
                }

                raw.Add(("OUT <", WithMath(p, m.Operation, Ccw(m.Output), m.Constant), false));
                raw.Add(("OUT >", WithMath(p, m.Operation, Cw(m.Output), m.Constant), false));
                break;
            case NodeKind.Splitter:
                var s = n.Splitter!;
                raw.Add(("OUT A >", PlacedNode.Split(p, new SplitterConfig { OutputA = Cw(s.OutputA), OutputB = s.OutputB, StartWithA = s.StartWithA }), false));
                raw.Add(("OUT B >", PlacedNode.Split(p, new SplitterConfig { OutputA = s.OutputA, OutputB = Cw(s.OutputB), StartWithA = s.StartWithA }), false));
                raw.Add(("1ST " + (s.StartWithA ? "A" : "B"), PlacedNode.Split(p, new SplitterConfig { OutputA = s.OutputA, OutputB = s.OutputB, StartWithA = !s.StartWithA }), false));
                break;
            case NodeKind.Portal:
                var pt = n.Portal!;
                raw.Add(("OFF -", PlacedNode.TimePortal(p, new PortalConfig { TimeOffset = NudgeNonZero(pt.TimeOffset, -1), Output = pt.Output }), false));
                raw.Add(("OFF +", PlacedNode.TimePortal(p, new PortalConfig { TimeOffset = NudgeNonZero(pt.TimeOffset, +1), Output = pt.Output }), false));
                raw.Add(("OUT <", PlacedNode.TimePortal(p, new PortalConfig { TimeOffset = pt.TimeOffset, Output = Ccw(pt.Output) }), false));
                raw.Add(("OUT >", PlacedNode.TimePortal(p, new PortalConfig { TimeOffset = pt.TimeOffset, Output = Cw(pt.Output) }), false));
                break;
            case NodeKind.Filter:
                var f = n.Filter!;
                raw.Add(("COND " + CompSym(NextComp(f.Comparison)), PlacedNode.Gate(p, new FilterConfig { Comparison = NextComp(f.Comparison), Constant = f.Constant, Output = f.Output }), false));
                raw.Add(("K -", PlacedNode.Gate(p, new FilterConfig { Comparison = f.Comparison, Constant = f.Constant - 1, Output = f.Output }), false));
                raw.Add(("K +", PlacedNode.Gate(p, new FilterConfig { Comparison = f.Comparison, Constant = f.Constant + 1, Output = f.Output }), false));
                raw.Add(("OUT >", PlacedNode.Gate(p, new FilterConfig { Comparison = f.Comparison, Constant = f.Constant, Output = Cw(f.Output) }), false));
                break;
            case NodeKind.Router:
                var rt = n.Router!;
                raw.Add(("COND " + CompSym(NextComp(rt.Comparison)), WithRouter(p, NextComp(rt.Comparison), rt.Constant, rt.OutMatch, rt.OutElse), false));
                raw.Add(("K -", WithRouter(p, rt.Comparison, rt.Constant - 1, rt.OutMatch, rt.OutElse), false));
                raw.Add(("K +", WithRouter(p, rt.Comparison, rt.Constant + 1, rt.OutMatch, rt.OutElse), false));
                raw.Add(("MATCH >", WithRouter(p, rt.Comparison, rt.Constant, Cw(rt.OutMatch), rt.OutElse), false));
                raw.Add(("ELSE >", WithRouter(p, rt.Comparison, rt.Constant, rt.OutMatch, Cw(rt.OutElse)), false));
                break;
            case NodeKind.Accumulator:
                var ac = n.Accumulator!;
                raw.Add(("REL " + CompSym(NextComp(ac.ReleaseWhen)), WithAcc(p, NextComp(ac.ReleaseWhen), ac.Constant, ac.Output, ac.Initial), false));
                raw.Add(("K -", WithAcc(p, ac.ReleaseWhen, ac.Constant - 1, ac.Output, ac.Initial), false));
                raw.Add(("K +", WithAcc(p, ac.ReleaseWhen, ac.Constant + 1, ac.Output, ac.Initial), false));
                raw.Add(("INIT -", WithAcc(p, ac.ReleaseWhen, ac.Constant, ac.Output, ac.Initial - 1), false));
                raw.Add(("INIT +", WithAcc(p, ac.ReleaseWhen, ac.Constant, ac.Output, ac.Initial + 1), false));
                raw.Add(("OUT >", WithAcc(p, ac.ReleaseWhen, ac.Constant, Cw(ac.Output), ac.Initial), false));
                break;
            default:
                break;
        }

        raw.Add(("DELETE", null, true));

        var list = new List<PanelControl>(raw.Count);
        int bw = (_panelRect.Width - 30) / 2;
        for (int i = 0; i < raw.Count; i++)
        {
            int col = i % 2;
            int row = i / 2;
            var rect = new Rectangle(_panelRect.X + 10 + (col * (bw + 10)), _panelRect.Y + 78 + (row * 48), bw, 40);
            list.Add(new PanelControl(rect, raw[i].Label, raw[i].Node, raw[i].Del));
        }

        return list;
    }

    private void DrawConfigPanel(Renderer r)
    {
        if (_selected is not { } sel || _editor.At(sel) is not { } node)
        {
            r.Text("CLICK A PLACED NODE TO CONFIGURE IT", new Vector2(_panelRect.X, _panelRect.Y), 1.8f, Palette.TextDim);
            return;
        }

        // Highlight the selected cell on the grid.
        Vector2 tl = _grid.CellTopLeft(sel);
        r.RectOutline(tl.X + 1, tl.Y + 1, _grid.CellSize - 2, _grid.CellSize - 2, 3, Palette.Accent);

        r.FillRect(_panelRect.X, _panelRect.Y, _panelRect.Width, _panelRect.Height, Palette.Panel);
        r.RectOutline(_panelRect.X, _panelRect.Y, _panelRect.Width, _panelRect.Height, 2, Palette.Accent);

        Color col = ToolColor(ToolOf(node));
        r.Text(NodeTitle(node), new Vector2(_panelRect.X + 12, _panelRect.Y + 12), 2.6f, col);
        r.TextCenteredFit(NodeSummary(node), new Vector2(_panelRect.Center.X, _panelRect.Y + 48), _panelRect.Width - 20, 18, Palette.Text);
        r.TextCenteredFit(KindHelp(node), new Vector2(_panelRect.Center.X, _panelRect.Y + 66), _panelRect.Width - 18, 12, Palette.TextDim);

        foreach (var ctrl in PanelControls(node, sel))
        {
            new UiButton(ctrl.Rect, ctrl.Label).Draw(r, _mouse);
        }
    }

    private static string KindHelp(PlacedNode n) => n.Kind switch
    {
        NodeKind.Belt => "Moves items one cell per tick.",
        NodeKind.Math => n.Math!.Constant.HasValue ? "Transforms each value by a constant." : "Adds its two incoming values.",
        NodeKind.Splitter => "Alternates items between A and B.",
        NodeKind.Portal => "Time machine: an item in at tick T leaves at T+offset (negative = past).",
        NodeKind.Filter => "Drops items that fail the test.",
        NodeKind.Router => "Match goes one way, the rest the other (never drops).",
        NodeKind.Accumulator => "Sums incoming values, releases on the condition.",
        _ => string.Empty,
    };

    private static string NodeTitle(PlacedNode n) => n.Kind switch
    {
        NodeKind.Belt => "BELT",
        NodeKind.Math => n.Math!.Constant.HasValue ? "MATH (UNARY)" : "MATH (ADD)",
        NodeKind.Splitter => "SPLITTER",
        NodeKind.Portal => "PORTAL",
        NodeKind.Filter => "FILTER",
        NodeKind.Router => "ROUTER",
        NodeKind.Accumulator => "ACCUMULATOR",
        _ => "NODE",
    };

    private static string NodeSummary(PlacedNode n) => n.Kind switch
    {
        NodeKind.Belt => "FLOW " + DirName(n.Direction),
        NodeKind.Math => MathIcon(n.Math!.Operation) + (n.Math!.Constant is { } c ? Num(c) : string.Empty) + " -> " + DirName(n.Math!.Output),
        NodeKind.Splitter => "A " + DirName(n.Splitter!.OutputA) + "  B " + DirName(n.Splitter!.OutputB),
        NodeKind.Portal => "t" + (n.Portal!.TimeOffset >= 0 ? "+" : string.Empty) + Num(n.Portal!.TimeOffset) + " -> " + DirName(n.Portal!.Output),
        NodeKind.Filter => "pass " + CompSym(n.Filter!.Comparison) + Num(n.Filter!.Constant) + " -> " + DirName(n.Filter!.Output),
        NodeKind.Router => CompSym(n.Router!.Comparison) + Num(n.Router!.Constant) + " ? " + DirName(n.Router!.OutMatch) + " : " + DirName(n.Router!.OutElse),
        NodeKind.Accumulator => "rel " + CompSym(n.Accumulator!.ReleaseWhen) + Num(n.Accumulator!.Constant) + " init " + Num(n.Accumulator!.Initial) + " -> " + DirName(n.Accumulator!.Output),
        _ => string.Empty,
    };

    private static PlacedNode WithMath(GridPoint p, MathOperation op, Direction outDir, int? constant) =>
        PlacedNode.MathOp(p, new MathConfig { Operation = op, Output = outDir, Constant = constant });

    private static PlacedNode WithRouter(GridPoint p, Comparison c, int k, Direction match, Direction els) =>
        PlacedNode.Route(p, new RouterConfig { Comparison = c, Constant = k, OutMatch = match, OutElse = els });

    private static PlacedNode WithAcc(GridPoint p, Comparison c, int k, Direction outDir, int init) =>
        PlacedNode.Accumulate(p, new AccumulatorConfig { ReleaseWhen = c, Constant = k, Output = outDir, Initial = init });

    private static Comparison NextComp(Comparison c) => (Comparison)(((int)c + 1) % 6);

    private static int NudgeNonZero(int v, int delta)
    {
        int r = v + delta;
        return r == 0 ? v + (2 * delta) : r;
    }

    private static string Num(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

    // ---- help overlay (objective + node codex) ----

    private void DrawHelpOverlay(Renderer r)
    {
        r.FillRect(0, 0, r.Width, r.Height, new Color((byte)0, (byte)0, (byte)0, (byte)212));

        var panel = new Rectangle(80, 64, r.Width - 160, r.Height - 128);
        r.FillRect(panel.X, panel.Y, panel.Width, panel.Height, Palette.Panel);
        r.RectOutline(panel.X, panel.Y, panel.Width, panel.Height, 3, Palette.Accent);

        r.Text("HOW THIS LEVEL WORKS", new Vector2(panel.X + 24, panel.Y + 18), 3.2f, Palette.Accent);
        r.Text("PRESS H OR CLICK TO CLOSE", new Vector2(panel.Right - 300, panel.Y + 26), 1.9f, Palette.TextDim);

        r.Text("OBJECTIVE", new Vector2(panel.X + 24, panel.Y + 58), 2.3f, Palette.Sink);
        string objective = string.IsNullOrEmpty(_level.Description) ? "Deliver the required values to the goal." : _level.Description;
        r.TextCenteredFit(objective, new Vector2(panel.Center.X, panel.Y + 88), panel.Width - 60, 24, Palette.Text);

        r.Text("NODES IN THIS LEVEL", new Vector2(panel.X + 24, panel.Y + 120), 2.3f, Palette.Math);

        var defs = CodexDefs();
        int top = panel.Y + 150;
        int avail = (panel.Bottom - 18) - top;
        int rowH = Math.Clamp(defs.Count == 0 ? avail : avail / defs.Count, 54, 80);
        float descLeft = panel.X + 70;
        float descRight = panel.Right - 28;
        for (int i = 0; i < defs.Count; i++)
        {
            NodeDefinition d = defs[i];
            int y = top + (i * rowH);
            Color col = NodeDefColor(d.Kind);

            var chip = new Rectangle(panel.X + 24, y + 6, 30, 30);
            r.FillRect(chip.X, chip.Y, chip.Width, chip.Height, Palette.PanelHi);
            r.RectOutline(chip.X, chip.Y, chip.Width, chip.Height, 2, col);
            r.TextCenteredFit(d.Visual.Icon, new Vector2(chip.Center.X, chip.Center.Y), 20, 20, col);

            // Title and description on clearly separated lines (bounded heights → no overlap).
            r.Text(d.Name.ToUpperInvariant(), new Vector2(descLeft, y + 8), 2.0f, col);
            if (!string.IsNullOrEmpty(d.Description))
            {
                r.TextCenteredFit(d.Description, new Vector2((descLeft + descRight) / 2f, y + 36), descRight - descLeft, 13, Palette.TextDim);
            }
        }
    }

    private List<NodeDefinition> CodexDefs()
    {
        var list = new List<NodeDefinition>();
        foreach (NodeDefinition d in _scenes.Catalog.Registry.Definitions)
        {
            bool fixedIo = d.Kind is NodeKind.Generator or NodeKind.Sink;
            bool allowed = _level.Inventory?.Allows(d.Id) ?? true;
            if (fixedIo || allowed)
            {
                list.Add(d);
            }
        }

        return list
            .OrderBy(static d => d.Kind is NodeKind.Generator ? 0 : d.Kind is NodeKind.Sink ? 1 : 2)
            .ThenBy(static d => d.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static Color NodeDefColor(NodeKind kind) => kind switch
    {
        NodeKind.Generator => Palette.Generator,
        NodeKind.Sink => Palette.Sink,
        NodeKind.Math => Palette.Math,
        NodeKind.Splitter => Palette.Splitter,
        NodeKind.Portal => Palette.Portal,
        NodeKind.Filter => Palette.Filter,
        NodeKind.Router => Palette.Router,
        NodeKind.Accumulator => Palette.Accumulator,
        _ => Palette.Belt,
    };

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

        if (_playTime >= last)
        {
            DrawResultsCard(r);
        }
    }

    private void DrawResultsCard(Renderer r)
    {
        if (_result is not { } res)
        {
            return;
        }

        r.FillRect(_cardRect.X, _cardRect.Y, _cardRect.Width, _cardRect.Height, Palette.Panel);
        Color accent = res.Outcome switch
        {
            LevelOutcome.Solved => Palette.Solved,
            LevelOutcome.Paradox => Palette.Paradox,
            _ => Palette.Failed,
        };
        r.RectOutline(_cardRect.X, _cardRect.Y, _cardRect.Width, _cardRect.Height, 3, accent);

        string title = res.Outcome switch
        {
            LevelOutcome.Solved => "LEVEL SOLVED",
            LevelOutcome.Paradox => "PARADOX",
            _ => "NOT SOLVED",
        };
        r.TextCentered(title, new Vector2(_cardRect.Center.X, _cardRect.Y + 52), 6f, accent);

        if (res.Outcome == LevelOutcome.Solved)
        {
            int stars = StarRating.Compute(res, _level.Par);
            r.TextCentered("STARS " + new string('*', stars) + new string('.', 3 - stars), new Vector2(_cardRect.Center.X, _cardRect.Y + 104), 4f, Palette.Item);
            r.TextCentered("TICKS " + res.Stats.FinalTick + "       NODES " + res.Stats.Footprint, new Vector2(_cardRect.Center.X, _cardRect.Y + 142), 3f, Palette.Text);

            if (_submit is { } sub)
            {
                r.TextCentered("BEST  " + (sub.Record.BestTicks?.ToString() ?? "-") + " TICKS / " + (sub.Record.BestFootprint?.ToString() ?? "-") + " NODES",
                    new Vector2(_cardRect.Center.X, _cardRect.Y + 174), 2.6f, Palette.TextDim);
                if (sub.NewTickRecord || sub.NewFootprintRecord)
                {
                    r.TextCentered("NEW RECORD!", new Vector2(_cardRect.Center.X, _cardRect.Y + 200), 3f, Palette.Accent);
                }
            }
        }
        else if (res.Error is { } e)
        {
            r.TextCenteredFit(e.Message.ToUpperInvariant(), new Vector2(_cardRect.Center.X, _cardRect.Y + 130), _cardRect.Width - 60, 30, Palette.TextDim);
        }
        else
        {
            r.TextCentered("WRONG OR MISSING OUTPUT AT THE SINK", new Vector2(_cardRect.Center.X, _cardRect.Y + 130), 2.6f, Palette.TextDim);
        }

        new UiButton(_retryBtn, "RETRY").Draw(r, _mouse);
        new UiButton(_levelsBtn, "LEVELS").Draw(r, _mouse);
        if (res.Outcome == LevelOutcome.Solved && NextLevelId() is not null)
        {
            new UiButton(_nextBtn, "NEXT >").Draw(r, _mouse);
        }
    }

    private string? NextLevelId()
    {
        var ids = _scenes.Catalog.LevelIds;
        for (int i = 0; i < ids.Count - 1; i++)
        {
            if (ids[i] == _levelId)
            {
                return ids[i + 1];
            }
        }

        return null;
    }

    private static Color ToolColor(Tool tool) => tool switch
    {
        Tool.MathAdd or Tool.MathMul => Palette.Math,
        Tool.Splitter => Palette.Splitter,
        Tool.Portal => Palette.Portal,
        Tool.Filter => Palette.Filter,
        Tool.Router => Palette.Router,
        Tool.Accumulator => Palette.Accumulator,
        _ => Palette.Belt,
    };

    private static string ToolIcon(Tool tool) => tool switch
    {
        Tool.MathAdd => "+",
        Tool.MathMul => "X",
        Tool.Splitter => "Y",
        Tool.Portal => "@",
        Tool.Filter => "F",
        Tool.Router => "R",
        Tool.Accumulator => "A",
        _ => ">",
    };

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
        Tool.Router => "node_router",
        Tool.Accumulator => "node_accumulator",
        _ => "node_belt",
    };

    private static string DefOf(PlacedNode n) => DefId(ToolOf(n));

    private static Tool ToolOf(PlacedNode n) => n.Kind switch
    {
        NodeKind.Belt => Tool.Belt,
        NodeKind.Splitter => Tool.Splitter,
        NodeKind.Portal => Tool.Portal,
        NodeKind.Filter => Tool.Filter,
        NodeKind.Router => Tool.Router,
        NodeKind.Accumulator => Tool.Accumulator,
        NodeKind.Math => n.Math!.Constant.HasValue ? Tool.MathMul : Tool.MathAdd,
        _ => Tool.Belt,
    };

    private static string GeneratorValues(GeneratorSpec g) =>
        string.Join(" ", g.Schedule.OrderBy(static s => s.Tick).Select(static s => s.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    private static string SinkValues(SinkSpec s) =>
        s.Expected.Count == 0 ? "-" : string.Join(" ", s.Expected.Select(static v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    private PlacedNode MakeNode(GridPoint cell) => _tool switch
    {
        Tool.Belt => PlacedNode.Belt(cell, _dir),
        Tool.MathAdd => PlacedNode.MathOp(cell, new MathConfig { Operation = MathOperation.Add, Output = _dir }),
        Tool.MathMul => PlacedNode.MathOp(cell, new MathConfig { Operation = MathOperation.Mul, Output = _dir, Constant = _mulConstant }),
        Tool.Splitter => PlacedNode.Split(cell, new SplitterConfig { OutputA = Cw(_dir), OutputB = Ccw(_dir) }),
        Tool.Portal => PlacedNode.TimePortal(cell, new PortalConfig { TimeOffset = _portalOffset, Output = _dir }),
        Tool.Filter => PlacedNode.Gate(cell, new FilterConfig { Comparison = _filterComparison, Constant = _filterConstant, Output = _dir }),
        Tool.Router => PlacedNode.Route(cell, new RouterConfig { Comparison = _routerComparison, Constant = _routerConstant, OutMatch = _dir, OutElse = Opp(_dir) }),
        Tool.Accumulator => PlacedNode.Accumulate(cell, new AccumulatorConfig { ReleaseWhen = _accComparison, Constant = _accConstant, Output = _dir }),
        _ => PlacedNode.Belt(cell, _dir),
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

    private static readonly Direction[] AllDirs = [Direction.Up, Direction.Right, Direction.Down, Direction.Left];

    private static Direction Cw(Direction d) => (Direction)(((int)d + 1) % 4);

    private static Direction Ccw(Direction d) => (Direction)(((int)d + 3) % 4);

    private static Direction Opp(Direction d) => (Direction)(((int)d + 2) % 4);

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
