using System.Globalization;
using System.Text;
using EchoFactory.Core;

namespace EchoFactory.Cli;

/// <summary>Renders a compiled timeline as an ASCII trace for the headless demo.</summary>
internal static class TracePrinter
{
    public static void Print(LevelDefinition level, Build build, SimulationResult result)
    {
        var layout = new Dictionary<GridPoint, string>();
        foreach (var g in level.Generators)
        {
            layout[g.Position] = "G";
        }

        foreach (var s in level.Sinks)
        {
            layout[s.Position] = "S";
        }

        foreach (var n in build.Nodes)
        {
            layout[n.Position] = n.Kind switch
            {
                NodeKind.Belt => Arrow(n.Direction),
                NodeKind.Math => "M",
                NodeKind.Splitter => "X",
                NodeKind.Portal => "@",
                _ => "?",
            };
        }

        Console.WriteLine(Invariant(
            $"Level: {level.Id}   grid {level.Grid.Width}x{level.Grid.Height}   maxTicks {level.MaxTicks}"));
        Console.WriteLine("Legend: G generator, S sink, > < ^ v belts, number = item value");
        Console.WriteLine();

        int last = result.Error is not null
            ? result.States.Count - 1
            : Math.Min(result.States.Count - 1, result.Stats.FinalTick + 1);

        for (int t = 0; t <= last; t++)
        {
            Console.WriteLine(Invariant($"-- tick {result.States[t].Tick} --"));
            PrintGrid(level.Grid, layout, result.States[t]);
            Console.WriteLine();
        }

        Console.WriteLine(Invariant($"Outcome  : {result.Outcome}"));
        if (result.Error is { } e)
        {
            Console.WriteLine(Invariant($"Paradox  : {e.Kind} - {e.Message}"));
        }

        Console.WriteLine(Invariant($"Footprint: {result.Stats.Footprint} node(s)"));
        Console.WriteLine(Invariant($"FinalTick: {result.Stats.FinalTick}"));
        Console.WriteLine(Invariant($"Passes   : {result.Stats.Passes}"));
    }

    private static void PrintGrid(GridSize grid, IReadOnlyDictionary<GridPoint, string> layout, GridState state)
    {
        for (int y = 0; y < grid.Height; y++)
        {
            var row = new StringBuilder();
            for (int x = 0; x < grid.Width; x++)
            {
                var p = new GridPoint(x, y);
                if (state.TryGetItem(p, out var item))
                {
                    row.Append(Cell(item.Value.ToString(CultureInfo.InvariantCulture)));
                }
                else if (layout.TryGetValue(p, out var glyph))
                {
                    row.Append(Cell(glyph));
                }
                else
                {
                    row.Append(" . ");
                }
            }

            Console.WriteLine(row.ToString());
        }
    }

    private static string Cell(string s)
    {
        if (s.Length >= 3)
        {
            return s[..3];
        }

        int pad = 3 - s.Length;
        int left = pad / 2;
        return new string(' ', left) + s + new string(' ', pad - left);
    }

    private static string Arrow(Direction d) => d switch
    {
        Direction.Up => "^",
        Direction.Right => ">",
        Direction.Down => "v",
        Direction.Left => "<",
        _ => "?",
    };

    private static string Invariant(FormattableString s) => FormattableString.Invariant(s);
}
