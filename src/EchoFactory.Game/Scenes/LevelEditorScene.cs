using EchoFactory.Content;
using EchoFactory.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoFactory.Game;

/// <summary>
/// A simple in-game level editor: place generators/sinks, edit their value sequences, resize the
/// grid, test-play, and save to <c>data/levels</c> via <see cref="LevelWriter"/>.
/// </summary>
internal sealed class LevelEditorScene : IScene
{
    private sealed class EdGen
    {
        public GridPoint Pos;
        public Direction Output = Direction.Right;
        public List<int> Values = [];
    }

    private sealed class EdSink
    {
        public GridPoint Pos;
        public List<int> Expected = [];
    }

    private enum EdTool
    {
        Generator,
        Sink,
    }

    private readonly SceneManager _scenes;
    private readonly List<EdGen> _gens = [];
    private readonly List<EdSink> _sinks = [];
    private int _w = 8;
    private int _h = 6;
    private GridView _grid;
    private EdTool _tool = EdTool.Generator;
    private EdGen? _selGen;
    private EdSink? _selSink;
    private string _message = "PLACE A GENERATOR (G) AND A SINK (S), THEN ADD VALUES WITH 0-9";
    private Vector2 _mouse;
    private readonly Rectangle _playArea;

    public LevelEditorScene(SceneManager scenes)
    {
        _scenes = scenes;
        _playArea = new Rectangle(40, 116, scenes.ScreenW - 80, scenes.ScreenH - 116 - 120);
        _grid = new GridView(new GridSize(_w, _h), _playArea);
    }

    public void Update(float dt, InputState input)
    {
        _mouse = input.Mouse;
        bool ctrl = input.KeyDown(Keys.LeftControl) || input.KeyDown(Keys.RightControl);
        bool overGrid = _grid.TryScreenToCell(_mouse, out GridPoint cell);

        if (input.KeyPressed(Keys.Escape))
        {
            _scenes.Switch(new MainMenuScene(_scenes));
            return;
        }

        if (ctrl && input.KeyPressed(Keys.S))
        {
            Save();
            return;
        }

        if (input.KeyPressed(Keys.T))
        {
            TestPlay();
            return;
        }

        if (input.KeyPressed(Keys.G)) _tool = EdTool.Generator;
        if (input.KeyPressed(Keys.S) && !ctrl) _tool = EdTool.Sink;

        // grid resize
        if (input.KeyPressed(Keys.Right)) Resize(_w + 1, _h);
        if (input.KeyPressed(Keys.Left)) Resize(_w - 1, _h);
        if (input.KeyPressed(Keys.Up)) Resize(_w, _h - 1);
        if (input.KeyPressed(Keys.Down)) Resize(_w, _h + 1);

        // edit selected node
        for (int d = 0; d <= 9; d++)
        {
            if (input.KeyPressed(Keys.D0 + d))
            {
                Selected()?.Add(d);
                _scenes.Play(Sfx.Click);
            }
        }

        if (input.KeyPressed(Keys.Back) && Selected() is { Count: > 0 } list)
        {
            list.RemoveAt(list.Count - 1);
        }

        if (input.KeyPressed(Keys.Delete))
        {
            DeleteSelected();
        }

        if (input.KeyPressed(Keys.R) && _selGen is not null)
        {
            _selGen.Output = (Direction)(((int)_selGen.Output + 1) % 4);
        }

        if (input.LeftClick && overGrid)
        {
            ClickCell(cell);
        }
    }

    private List<int>? Selected() => _selGen?.Values ?? _selSink?.Expected;

    private void ClickCell(GridPoint cell)
    {
        EdGen? gen = _gens.FirstOrDefault(g => g.Pos == cell);
        EdSink? sink = _sinks.FirstOrDefault(s => s.Pos == cell);

        if (gen is not null)
        {
            _selGen = gen;
            _selSink = null;
        }
        else if (sink is not null)
        {
            _selSink = sink;
            _selGen = null;
        }
        else if (_tool == EdTool.Generator)
        {
            var g = new EdGen { Pos = cell };
            _gens.Add(g);
            _selGen = g;
            _selSink = null;
            _scenes.Play(Sfx.Place);
        }
        else
        {
            var s = new EdSink { Pos = cell };
            _sinks.Add(s);
            _selSink = s;
            _selGen = null;
            _scenes.Play(Sfx.Place);
        }
    }

    private void DeleteSelected()
    {
        if (_selGen is not null)
        {
            _gens.Remove(_selGen);
            _selGen = null;
        }
        else if (_selSink is not null)
        {
            _sinks.Remove(_selSink);
            _selSink = null;
        }

        _scenes.Play(Sfx.Remove);
    }

    private void Resize(int w, int h)
    {
        _w = Math.Clamp(w, 3, 14);
        _h = Math.Clamp(h, 3, 12);
        _grid = new GridView(new GridSize(_w, _h), _playArea);
        _gens.RemoveAll(g => !InBounds(g.Pos));
        _sinks.RemoveAll(s => !InBounds(s.Pos));
    }

    private bool InBounds(GridPoint p) => p.X >= 0 && p.Y >= 0 && p.X < _w && p.Y < _h;

    private LevelDefinition BuildLevel(string id) => new()
    {
        Id = id,
        Grid = new GridSize(_w, _h),
        MaxTicks = 60,
        MaxTemporalPasses = 5,
        Generators = _gens.Select((g, i) => new GeneratorSpec
        {
            Id = "gen" + i,
            Position = g.Pos,
            Output = g.Output,
            Schedule = g.Values.Select((v, t) => new SpawnEntry(t, v)).ToList(),
        }).ToList(),
        Sinks = _sinks.Select((s, i) => new SinkSpec
        {
            Id = "sink" + i,
            Position = s.Pos,
            Expected = s.Expected.ToList(),
        }).ToList(),
    };

    private void TestPlay()
    {
        if (!CanBuild())
        {
            return;
        }

        _scenes.Switch(new GameplayScene(_scenes, "editor-test", BuildLevel("editor_test")));
    }

    private void Save()
    {
        if (!CanBuild())
        {
            return;
        }

        try
        {
            string dir = Path.Combine(_scenes.Catalog.DataDir, "levels");
            Directory.CreateDirectory(dir);
            string id = NextId(dir);
            File.WriteAllText(Path.Combine(dir, id + ".json"), LevelWriter.Write(BuildLevel(id)));
            _scenes.Catalog.Reload();
            _message = "SAVED AS " + id.ToUpperInvariant() + " - PLAY IT FROM LEVEL SELECT";
            _scenes.Play(Sfx.Solved);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _message = "SAVE FAILED: " + e.Message.ToUpperInvariant();
            _scenes.Play(Sfx.Paradox);
        }
    }

    private bool CanBuild()
    {
        if (_gens.All(g => g.Values.Count == 0) || _sinks.Count == 0 || _sinks.All(s => s.Expected.Count == 0))
        {
            _message = "NEED A GENERATOR WITH VALUES AND A SINK WITH EXPECTED VALUES";
            _scenes.Play(Sfx.Paradox);
            return false;
        }

        return true;
    }

    private static string NextId(string dir)
    {
        for (int n = 1; ; n++)
        {
            string id = "lvl_custom_" + n.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
            if (!File.Exists(Path.Combine(dir, id + ".json")))
            {
                return id;
            }
        }
    }

    public void Draw(Renderer r)
    {
        // grid
        for (int y = 0; y < _h; y++)
        {
            for (int x = 0; x < _w; x++)
            {
                Vector2 tl = _grid.CellTopLeft(new GridPoint(x, y));
                r.FillRect(tl.X + 1, tl.Y + 1, _grid.CellSize - 2, _grid.CellSize - 2, Palette.Grid);
            }
        }

        if (_grid.TryScreenToCell(_mouse, out GridPoint hover))
        {
            Vector2 tl = _grid.CellTopLeft(hover);
            r.RectOutline(tl.X + 1, tl.Y + 1, _grid.CellSize - 2, _grid.CellSize - 2, 2, Palette.Accent);
        }

        foreach (var g in _gens)
        {
            DrawCell(r, g.Pos, Palette.Generator, "G", string.Join(" ", g.Values), g == _selGen);
        }

        foreach (var s in _sinks)
        {
            DrawCell(r, s.Pos, Palette.Sink, "S", string.Join(" ", s.Expected), s == _selSink);
        }

        // top bar
        r.FillRect(0, 0, r.Width, 60, Palette.Panel);
        r.Text("LEVEL EDITOR", new Vector2(40, 18), 4f, Palette.Text);
        r.TextCentered("TOOL: " + (_tool == EdTool.Generator ? "GENERATOR" : "SINK"), new Vector2(r.Width / 2f, 28), 3f, Palette.Accent);
        r.Text("GRID " + _w + "x" + _h, new Vector2(r.Width - 200, 22), 3f, Palette.TextDim);

        r.Text(_message, new Vector2(40, 74), 2.6f, Palette.Item);

        r.Text("CLICK PLACE/SELECT   G GENERATOR   S SINK   0-9 ADD VALUE   BACKSPACE DELETE VALUE   R ROTATE GEN",
            new Vector2(40, r.Height - 52), 2f, Palette.TextDim);
        r.Text("ARROWS RESIZE GRID   DEL REMOVE NODE   T TEST   CTRL+S SAVE   ESC MENU",
            new Vector2(40, r.Height - 28), 2f, Palette.TextDim);
    }

    private void DrawCell(Renderer r, GridPoint cell, Color color, string icon, string sub, bool selected)
    {
        Vector2 tl = _grid.CellTopLeft(cell);
        float s = _grid.CellSize;
        float pad = s * 0.14f;
        r.FillRect(tl.X + pad, tl.Y + pad, s - (2 * pad), s - (2 * pad), Palette.Panel);
        r.RectOutline(tl.X + pad, tl.Y + pad, s - (2 * pad), s - (2 * pad), selected ? 4f : MathF.Max(2f, s * 0.03f), selected ? Palette.Accent : color);

        Vector2 c = _grid.CellCenter(cell);
        if (string.IsNullOrEmpty(sub))
        {
            r.TextCenteredFit(icon, c, s * 0.4f, s * 0.4f, color);
        }
        else
        {
            r.TextCenteredFit(icon, c - new Vector2(0, s * 0.16f), s * 0.4f, s * 0.26f, color);
            r.TextCenteredFit(sub, c + new Vector2(0, s * 0.2f), s * 0.82f, s * 0.24f, Palette.Item);
        }
    }
}
