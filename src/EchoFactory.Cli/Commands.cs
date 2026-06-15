using System.Diagnostics;
using EchoFactory.Content;
using EchoFactory.Core;

namespace EchoFactory.Cli;

/// <summary>Headless commands wiring the JSON content pipeline to the engine.</summary>
internal static class Commands
{
    public static int Demo()
    {
        var level = Examples.LineDemoLevel();
        var build = Examples.LineDemoSolution();
        var result = SimulationCompiler.Compile(level, build);
        TracePrinter.Print(level, build, result);
        return ExitFor(result.Outcome);
    }

    public static int Validate(string dataDir)
    {
        ValidationReport report = ContentLibrary.Validate(dataDir);

        foreach (string line in report.Loaded)
        {
            Console.WriteLine($"  ok   {line}");
        }

        foreach (string error in report.Errors)
        {
            Console.Error.WriteLine($"  ERR  {error}");
        }

        Console.WriteLine();
        Console.WriteLine(report.Ok
            ? $"VALID — {report.Loaded.Count} item(s), 0 errors"
            : $"INVALID — {report.Errors.Count} error(s)");
        return report.Ok ? 0 : 1;
    }

    public static int Run(string dataDir, IReadOnlyList<string> args) => Guard(() =>
    {
        if (args.Count < 2)
        {
            Console.Error.WriteLine("usage: run <level_id> <solution_id>");
            return 1;
        }

        var (level, solution) = Load(dataDir, args[0], args[1]);
        var result = SimulationCompiler.Compile(level, solution.Build);
        TracePrinter.Print(level, solution.Build, result);
        return ExitFor(result.Outcome);
    });

    public static int Verify(string dataDir, IReadOnlyList<string> args) => Guard(() =>
    {
        if (args.Count < 2)
        {
            Console.Error.WriteLine("usage: verify <level_id> <solution_id>");
            return 1;
        }

        var (level, solution) = Load(dataDir, args[0], args[1]);
        var result = SimulationCompiler.Compile(level, solution.Build);

        Console.WriteLine($"Level    : {level.Id}");
        Console.WriteLine($"Outcome  : {result.Outcome}");
        if (result.Error is { } e)
        {
            Console.WriteLine($"Paradox  : {e.Message}");
        }

        Console.WriteLine($"Ticks    : {result.Stats.FinalTick}");
        Console.WriteLine($"Footprint: {result.Stats.Footprint}");
        if (level.Par is { } par)
        {
            int stars = StarRating.Compute(result, par);
            Console.WriteLine($"Par      : {par.Ticks} ticks / {par.Footprint} nodes");
            Console.WriteLine($"Stars    : {new string('*', stars)}{new string('.', 3 - stars)} ({stars}/3)");
        }

        return result.Outcome == LevelOutcome.Solved ? 0 : 1;
    });

    public static int List(string dataDir) => Guard(() =>
    {
        PrintGroup("nodes", Path.Combine(dataDir, "nodes"));
        PrintGroup("levels", Path.Combine(dataDir, "levels"));
        PrintGroup("solutions", Path.Combine(dataDir, "solutions"));
        return 0;
    });

    private static void PrintGroup(string label, string dir)
    {
        var files = Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir, "*.json").OrderBy(static f => f, StringComparer.Ordinal).ToList()
            : [];

        Console.WriteLine($"{label} ({files.Count}):");
        foreach (string f in files)
        {
            Console.WriteLine($"  {Path.GetFileNameWithoutExtension(f)}");
        }
    }

    public static int Bench(string dataDir) => Guard(() =>
    {
        const int iterations = 2000;
        var registry = NodeRegistry.LoadFromDirectory(Path.Combine(dataDir, "nodes"));

        foreach (string file in Directory.EnumerateFiles(Path.Combine(dataDir, "solutions"), "*.json")
                     .OrderBy(static f => f, StringComparer.Ordinal))
        {
            var solution = SolutionLoader.LoadFile(file, registry);
            var level = LevelLoader.LoadFile(Path.Combine(dataDir, "levels", solution.LevelId + ".json"));

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                SimulationCompiler.Compile(level, solution.Build);
            }

            sw.Stop();
            double perCompile = sw.Elapsed.TotalMilliseconds / iterations;
            Console.WriteLine($"{Path.GetFileName(file),-24} {iterations} compiles  {sw.ElapsedMilliseconds,5} ms  ({perCompile:F4} ms/compile)");
        }

        return 0;
    });

    private static (LevelDefinition Level, SolutionInfo Solution) Load(string dataDir, string levelId, string solutionId)
    {
        var registry = NodeRegistry.LoadFromDirectory(Path.Combine(dataDir, "nodes"));
        var level = LevelLoader.LoadFile(Path.Combine(dataDir, "levels", levelId + ".json"));
        var solution = SolutionLoader.LoadFile(Path.Combine(dataDir, "solutions", solutionId + ".json"), registry);
        return (level, solution);
    }

    private static int ExitFor(LevelOutcome outcome) => outcome switch
    {
        LevelOutcome.Solved => 0,
        LevelOutcome.Paradox => 2,
        _ => 3,
    };

    private static int Guard(Func<int> body)
    {
        try
        {
            return body();
        }
        catch (ContentException e)
        {
            Console.Error.WriteLine($"Content error: {e.Message}");
            return 1;
        }
        catch (IOException e)
        {
            Console.Error.WriteLine($"IO error: {e.Message}");
            return 1;
        }
    }
}
