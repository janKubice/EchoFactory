using EchoFactory.Content;
using EchoFactory.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoFactory.Game;

/// <summary>
/// In-game level editor with a fully clickable UI: place generators/sinks, edit their value
/// sequences with an on-screen keypad (multi-digit + negative), resize the grid and max-ticks
/// with steppers, name the level, test-play, and save to <c>data/levels</c>.
/// Keyboard shortcuts still work as accelerators.
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

    private enum PadAction
    {
        Digit,
        Sign,
        Backspace,
    }

    private enum TextField
    {
        None,
        Name,
        Campaign,
    }

    private readonly record struct PadKey(Rectangle Rect, string Label, PadAction Action, int Digit);

    private readonly SceneManager _scenes;
    private readonly List<EdGen> _gens = [];
    private readonly List<EdSink> _sinks = [];
    private int _w = 8;
    private int _h = 6;
    private int _maxTicks = 60;
    private string _name = string.Empty;
    private string _campaign = string.Empty;
    private string _entry = string.Empty;
    private TextField _editing = TextField.None;
    private GridView _grid;
    private EdTool _tool = EdTool.Generator;
    private EdGen? _selGen;
    private EdSink? _selSink;
    private string _message = "PLACE A GENERATOR AND A SINK, SELECT IT, THEN ADD VALUES WITH THE KEYPAD";
    private Vector2 _mouse;

    private readonly Rectangle _playArea;
    private readonly Rectangle _panel;
    private readonly UiButton _backBtn;
    private readonly UiButton _testBtn;
    private readonly UiButton _saveBtn;
    private readonly UiButton _genTool;
    private readonly UiButton _sinkTool;
    private readonly Rectangle _nameField;
    private readonly Rectangle _campaignField;
    private readonly UiButton _wMinus;
    private readonly UiButton _wPlus;
    private readonly UiButton _hMinus;
    private readonly UiButton _hPlus;
    private readonly UiButton _tMinus;
    private readonly UiButton _tPlus;
    private readonly UiButton _addBtn;
    private readonly UiButton _clrBtn;
    private readonly UiButton _rotateBtn;
    private readonly UiButton _removeBtn;
    private readonly int _padTop;

    public LevelEditorScene(SceneManager scenes)
    {
        _scenes = scenes;
        int w = scenes.ScreenW;
        int h = scenes.ScreenH;

        _panel = new Rectangle(w - 272, 64, 252, h - 64 - 40);
        _playArea = new Rectangle(40, 150, _panel.X - 40 - 20, h - 150 - 44);
        _grid = new GridView(new GridSize(_w, _h), _playArea);

        _backBtn = new UiButton(new Rectangle(16, 12, 120, 34), "< MENU");
        _testBtn = new UiButton(new Rectangle(w - 264, 12, 120, 34), "TEST [T]");
        _saveBtn = new UiButton(new Rectangle(w - 136, 12, 120, 34), "SAVE [S]");

        _genTool = new UiButton(new Rectangle(16, 64, 150, 38), "GENERATOR");
        _sinkTool = new UiButton(new Rectangle(174, 64, 150, 38), "SINK");
        int fieldsLeft = 340;
        int fieldsW = (_panel.X - 20) - fieldsLeft;
        int half = (fieldsW - 12) / 2;
        _nameField = new Rectangle(fieldsLeft, 64, half, 38);
        _campaignField = new Rectangle(fieldsLeft + half + 12, 64, half, 38);

        int px = _panel.X + 12;
        int valX = _panel.X + 150;
        _wMinus = new UiButton(new Rectangle(valX, _panel.Y + 30, 32, 30), "-");
        _wPlus = new UiButton(new Rectangle(valX + 70, _panel.Y + 30, 32, 30), "+");
        _hMinus = new UiButton(new Rectangle(valX, _panel.Y + 66, 32, 30), "-");
        _hPlus = new UiButton(new Rectangle(valX + 70, _panel.Y + 66, 32, 30), "+");
        _tMinus = new UiButton(new Rectangle(valX, _panel.Y + 102, 32, 30), "-");
        _tPlus = new UiButton(new Rectangle(valX + 70, _panel.Y + 102, 32, 30), "+");

        _padTop = _panel.Y + 262;
        int bw = (_panel.Width - 24 - 16) / 3;
        int bh = 38;
        const int gap = 8;
        int addY = _padTop + (4 * (bh + gap));
        _addBtn = new UiButton(new Rectangle(px, addY, (2 * bw) + gap, bh), "ADD");
        _clrBtn = new UiButton(new Rectangle(px + (2 * (bw + gap)), addY, bw, bh), "CLR");
        _rotateBtn = new UiButton(new Rectangle(px, addY + bh + gap, _panel.Width - 24, bh), "ROTATE OUT");
        _removeBtn = new UiButton(new Rectangle(px, addY + (2 * (bh + gap)), _panel.Width - 24, bh), "REMOVE NODE");
    }

    public void Update(float dt, InputState input)
    {
        _mouse = input.Mouse;
        bool ctrl = input.KeyDown(Keys.LeftControl) || input.KeyDown(Keys.RightControl);

        // A text field captures all typing while active.
        if (_editing != TextField.None)
        {
            UpdateFieldEditing(input);
            return;
        }

        if (input.KeyPressed(Keys.Escape))
        {
            _scenes.Switch(new MainMenuScene(_scenes));
            return;
        }

        // --- top bar + toolbar buttons ---
        if (input.LeftClick)
        {
            if (_backBtn.Hit(_mouse)) { _scenes.Switch(new MainMenuScene(_scenes)); return; }
            if (_testBtn.Hit(_mouse)) { TestPlay(); return; }
            if (_saveBtn.Hit(_mouse)) { Save(); return; }
            if (_genTool.Hit(_mouse)) { _tool = EdTool.Generator; _scenes.Play(Sfx.Click); return; }
            if (_sinkTool.Hit(_mouse)) { _tool = EdTool.Sink; _scenes.Play(Sfx.Click); return; }
            if (_nameField.Contains(Point(_mouse))) { _editing = TextField.Name; _scenes.Play(Sfx.Click); return; }
            if (_campaignField.Contains(Point(_mouse))) { _editing = TextField.Campaign; _scenes.Play(Sfx.Click); return; }

            if (_wMinus.Hit(_mouse)) { Resize(_w - 1, _h); return; }
            if (_wPlus.Hit(_mouse)) { Resize(_w + 1, _h); return; }
            if (_hMinus.Hit(_mouse)) { Resize(_w, _h - 1); return; }
            if (_hPlus.Hit(_mouse)) { Resize(_w, _h + 1); return; }
            if (_tMinus.Hit(_mouse)) { _maxTicks = Math.Clamp(_maxTicks - 5, 5, 999); return; }
            if (_tPlus.Hit(_mouse)) { _maxTicks = Math.Clamp(_maxTicks + 5, 5, 999); return; }

            if (HandlePanelClick()) { return; }

            if (_grid.TryScreenToCell(_mouse, out GridPoint cell))
            {
                ClickCell(cell);
                return;
            }
        }

        // --- keyboard accelerators ---
        if (ctrl && input.KeyPressed(Keys.S)) { Save(); return; }
        if (input.KeyPressed(Keys.T)) { TestPlay(); return; }
        if (input.KeyPressed(Keys.G)) { _tool = EdTool.Generator; }
        if (input.KeyPressed(Keys.Right)) { Resize(_w + 1, _h); }
        if (input.KeyPressed(Keys.Left)) { Resize(_w - 1, _h); }
        if (input.KeyPressed(Keys.Up)) { Resize(_w, _h - 1); }
        if (input.KeyPressed(Keys.Down)) { Resize(_w, _h + 1); }
        if (input.KeyPressed(Keys.Delete)) { DeleteSelected(); }
        if (input.KeyPressed(Keys.R) && _selGen is not null)
        {
            _selGen.Output = (Direction)(((int)_selGen.Output + 1) % 4);
        }

        // Typed digits / sign build the entry buffer for the selected node.
        if (Selected() is not null)
        {
            foreach (char ch in input.Typed)
            {
                AppendEntry(ch);
            }

            if (input.KeyPressed(Keys.Enter)) { CommitEntry(); }
            if (input.KeyPressed(Keys.Back)) { Backspace(); }
        }
    }

    private void UpdateFieldEditing(InputState input)
    {
        string current = _editing == TextField.Name ? _name : _campaign;
        foreach (char ch in input.Typed)
        {
            if ((char.IsLetterOrDigit(ch) || ch is ' ' or '_' or '-') && current.Length < 24)
            {
                current += ch;
            }
        }

        if (input.KeyPressed(Keys.Back) && current.Length > 0)
        {
            current = current[..^1];
        }

        if (_editing == TextField.Name)
        {
            _name = current;
        }
        else
        {
            _campaign = current;
        }

        Rectangle active = _editing == TextField.Name ? _nameField : _campaignField;
        if (input.KeyPressed(Keys.Enter) || input.KeyPressed(Keys.Escape) ||
            (input.LeftClick && !active.Contains(Point(_mouse))))
        {
            _editing = TextField.None;
        }
    }

    private bool HandlePanelClick()
    {
        if (Selected() is not { } list)
        {
            return false;
        }

        foreach (PadKey key in KeypadKeys())
        {
            if (key.Rect.Contains(Point(_mouse)))
            {
                switch (key.Action)
                {
                    case PadAction.Digit: AppendEntry((char)('0' + key.Digit)); break;
                    case PadAction.Sign: ToggleSign(); break;
                    case PadAction.Backspace: Backspace(); break;
                }

                _scenes.Play(Sfx.Click);
                return true;
            }
        }

        if (_addBtn.Hit(_mouse)) { CommitEntry(); return true; }
        if (_clrBtn.Hit(_mouse)) { list.Clear(); _scenes.Play(Sfx.Remove); return true; }
        if (_removeBtn.Hit(_mouse)) { DeleteSelected(); return true; }
        if (_rotateBtn.Hit(_mouse) && _selGen is not null)
        {
            _selGen.Output = (Direction)(((int)_selGen.Output + 1) % 4);
            _scenes.Play(Sfx.Click);
            return true;
        }

        return false;
    }

    private void AppendEntry(char ch)
    {
        if (char.IsDigit(ch))
        {
            if (_entry.Replace("-", string.Empty).Length < 5)
            {
                _entry += ch;
            }
        }
        else if (ch == '-' && _entry.Length == 0)
        {
            _entry = "-";
        }
    }

    private void ToggleSign() => _entry = _entry.StartsWith('-') ? _entry[1..] : "-" + _entry;

    private void Backspace()
    {
        if (_entry.Length > 0)
        {
            _entry = _entry[..^1];
        }
        else if (Selected() is { Count: > 0 } list)
        {
            list.RemoveAt(list.Count - 1);
        }
    }

    private void CommitEntry()
    {
        string trimmed = _entry == "-" ? string.Empty : _entry;
        if (trimmed.Length > 0 && int.TryParse(trimmed, System.Globalization.CultureInfo.InvariantCulture, out int v))
        {
            Selected()?.Add(v);
            _scenes.Play(Sfx.Place);
        }

        _entry = string.Empty;
    }

    private List<int>? Selected() => _selGen?.Values ?? _selSink?.Expected;

    private void ClickCell(GridPoint cell)
    {
        EdGen? gen = _gens.FirstOrDefault(g => g.Pos == cell);
        EdSink? sink = _sinks.FirstOrDefault(s => s.Pos == cell);
        _entry = string.Empty;

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

        _entry = string.Empty;
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
        Name = string.IsNullOrWhiteSpace(_name) ? id : _name.Trim(),
        Campaign = _campaign.Trim(),
        Grid = new GridSize(_w, _h),
        MaxTicks = _maxTicks,
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

        _scenes.Switch(new GameplayScene(_scenes, "editor-test", BuildLevel("editor_test"), this));
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

    private List<PadKey> KeypadKeys()
    {
        var keys = new List<PadKey>(12);
        int bx = _panel.X + 12;
        int bw = (_panel.Width - 24 - 16) / 3;
        int bh = 38;
        const int gap = 8;
        for (int i = 0; i < 9; i++)
        {
            int col = i % 3;
            int row = i / 3;
            var rect = new Rectangle(bx + (col * (bw + gap)), _padTop + (row * (bh + gap)), bw, bh);
            keys.Add(new PadKey(rect, (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), PadAction.Digit, i + 1));
        }

        int r3 = _padTop + (3 * (bh + gap));
        keys.Add(new PadKey(new Rectangle(bx, r3, bw, bh), "+/-", PadAction.Sign, 0));
        keys.Add(new PadKey(new Rectangle(bx + (bw + gap), r3, bw, bh), "0", PadAction.Digit, 0));
        keys.Add(new PadKey(new Rectangle(bx + (2 * (bw + gap)), r3, bw, bh), "DEL", PadAction.Backspace, 0));
        return keys;
    }

    private static Point Point(Vector2 v) => new((int)v.X, (int)v.Y);

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

        DrawTopBar(r);
        DrawPanel(r);

        r.Text(_message, new Vector2(40, r.Height - 30), 2.2f, Palette.Item);
    }

    private void DrawTopBar(Renderer r)
    {
        r.FillRect(0, 0, r.Width, 56, Palette.Panel);
        r.Text("LEVEL EDITOR", new Vector2(160, 16), 3.4f, Palette.Text);
        _backBtn.Draw(r, _mouse);
        _testBtn.Draw(r, _mouse);
        _saveBtn.Draw(r, _mouse);

        // toolbar
        DrawToolButton(r, _genTool, _tool == EdTool.Generator, Palette.Generator);
        DrawToolButton(r, _sinkTool, _tool == EdTool.Sink, Palette.Sink);

        DrawTextField(r, _nameField, TextField.Name, _name, "NAME");
        DrawTextField(r, _campaignField, TextField.Campaign, _campaign, "CAMPAIGN");
    }

    private void DrawTextField(Renderer r, Rectangle field, TextField which, string value, string placeholder)
    {
        bool active = _editing == which;
        bool hover = active || field.Contains(Point(_mouse));
        r.FillRect(field.X, field.Y, field.Width, field.Height, Palette.Panel);
        r.RectOutline(field.X, field.Y, field.Width, field.Height, 2, hover ? Palette.Accent : Palette.GridLine);
        string shown = value.Length == 0 ? placeholder : value.ToUpperInvariant() + (active ? "_" : string.Empty);
        r.TextCenteredFit(shown, new Vector2(field.Center.X, field.Center.Y), field.Width - 16, 18, value.Length == 0 ? Palette.TextDim : Palette.Text);
    }

    private void DrawToolButton(Renderer r, UiButton btn, bool active, Color color)
    {
        Rectangle rect = btn.Rect;
        bool hover = btn.Hit(_mouse);
        r.FillRect(rect.X, rect.Y, rect.Width, rect.Height, active ? Palette.PanelHi : Palette.Panel);
        r.RectOutline(rect.X, rect.Y, rect.Width, rect.Height, active ? 3f : 2f, active ? color : (hover ? Palette.Accent : Palette.GridLine));
        r.TextCentered(btn.Label, new Vector2(rect.Center.X, rect.Center.Y), 2.6f, active ? color : Palette.TextDim);
    }

    private void DrawPanel(Renderer r)
    {
        r.FillRect(_panel.X, _panel.Y, _panel.Width, _panel.Height, Palette.Panel);
        r.RectOutline(_panel.X, _panel.Y, _panel.Width, _panel.Height, 2, Palette.GridLine);

        StepperRow(r, _panel.Y + 30, "GRID WIDTH", _w, _wMinus, _wPlus);
        StepperRow(r, _panel.Y + 66, "GRID HEIGHT", _h, _hMinus, _hPlus);
        StepperRow(r, _panel.Y + 102, "MAX TICKS", _maxTicks, _tMinus, _tPlus);

        r.FillRect(_panel.X + 10, _panel.Y + 142, _panel.Width - 20, 2, Palette.GridLine);

        if (Selected() is not { } list)
        {
            r.TextCenteredFit("CLICK A CELL TO PLACE OR SELECT A NODE", new Vector2(_panel.Center.X, _panel.Y + 200), _panel.Width - 24, 22, Palette.TextDim);
            return;
        }

        bool isGen = _selGen is not null;
        Color col = isGen ? Palette.Generator : Palette.Sink;
        string title = isGen ? "GENERATOR" : "SINK";
        if (isGen)
        {
            title += "  OUT " + _selGen!.Output.ToString().ToUpperInvariant();
        }

        r.Text(title, new Vector2(_panel.X + 12, _panel.Y + 150), 2.4f, col);
        r.Text(isGen ? "EMITS (ONE PER TICK):" : "EXPECTS (IN ORDER):", new Vector2(_panel.X + 12, _panel.Y + 176), 1.9f, Palette.TextDim);

        // committed values
        var valuesBox = new Rectangle(_panel.X + 12, _panel.Y + 196, _panel.Width - 24, 24);
        r.RectOutline(valuesBox.X, valuesBox.Y, valuesBox.Width, valuesBox.Height, 1, Palette.GridLine);
        string vals = list.Count == 0 ? "(empty)" : string.Join(" ", list);
        r.TextCenteredFit(vals, new Vector2(valuesBox.Center.X, valuesBox.Center.Y), valuesBox.Width - 8, 18, Palette.Item);

        // entry buffer
        var entryBox = new Rectangle(_panel.X + 12, _panel.Y + 226, _panel.Width - 24, 28);
        r.RectOutline(entryBox.X, entryBox.Y, entryBox.Width, entryBox.Height, 2, Palette.Accent);
        r.TextCenteredFit(_entry.Length == 0 ? "TYPE A NUMBER" : _entry + "_", new Vector2(entryBox.Center.X, entryBox.Center.Y), entryBox.Width - 8, 20, _entry.Length == 0 ? Palette.TextDim : Palette.Text);

        foreach (PadKey key in KeypadKeys())
        {
            new UiButton(key.Rect, key.Label).Draw(r, _mouse);
        }

        _addBtn.Draw(r, _mouse);
        _clrBtn.Draw(r, _mouse);
        if (isGen)
        {
            _rotateBtn.Draw(r, _mouse);
        }

        _removeBtn.Draw(r, _mouse);
    }

    private void StepperRow(Renderer r, int y, string label, int value, UiButton minus, UiButton plus)
    {
        r.Text(label, new Vector2(_panel.X + 12, y + 6), 2.1f, Palette.Text);
        minus.Draw(r, _mouse);
        plus.Draw(r, _mouse);
        r.TextCentered(value.ToString(System.Globalization.CultureInfo.InvariantCulture), new Vector2((minus.Rect.Right + plus.Rect.Left) / 2f, y + 15), 2.6f, Palette.Item);
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
