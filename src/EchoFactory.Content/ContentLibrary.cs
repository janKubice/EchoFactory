using EchoFactory.Core;

namespace EchoFactory.Content;

/// <summary>Outcome of validating a content directory.</summary>
public sealed class ValidationReport
{
    public List<string> Loaded { get; } = [];

    public List<string> Errors { get; } = [];

    public bool Ok => Errors.Count == 0;
}

/// <summary>
/// Loads and validates a whole <c>/data</c> tree (nodes, levels, solutions) and collects every
/// error instead of stopping at the first — the basis of the CLI <c>validate</c> command.
/// Reference solutions are re-simulated and must actually solve their level.
/// </summary>
public static class ContentLibrary
{
    public static ValidationReport Validate(string dataDir)
    {
        var report = new ValidationReport();

        NodeRegistry? registry = null;
        try
        {
            registry = NodeRegistry.LoadFromDirectory(Path.Combine(dataDir, "nodes"));
            report.Loaded.Add($"nodes: {registry.Count} definition(s)");
        }
        catch (ContentException e)
        {
            report.Errors.Add(e.Message);
        }

        foreach (string file in NodeRegistry.EnumerateJson(Path.Combine(dataDir, "levels")))
        {
            try
            {
                LevelDefinition level = LevelLoader.LoadFile(file);
                report.Loaded.Add($"level: {level.Id}");
            }
            catch (ContentException e)
            {
                report.Errors.Add(e.Message);
            }
        }

        if (registry is not null)
        {
            foreach (string file in NodeRegistry.EnumerateJson(Path.Combine(dataDir, "solutions")))
            {
                ValidateSolution(dataDir, registry, file, report);
            }
        }

        return report;
    }

    private static void ValidateSolution(string dataDir, NodeRegistry registry, string file, ValidationReport report)
    {
        string name = Path.GetFileName(file);
        try
        {
            SolutionInfo solution = SolutionLoader.LoadFile(file, registry);
            string levelPath = Path.Combine(dataDir, "levels", solution.LevelId + ".json");
            if (!File.Exists(levelPath))
            {
                report.Errors.Add($"{name}: references unknown level '{solution.LevelId}'");
                return;
            }

            LevelDefinition level = LevelLoader.LoadFile(levelPath);
            SimulationResult result = SimulationCompiler.Compile(level, solution.Build);
            if (result.Outcome != LevelOutcome.Solved)
            {
                string detail = result.Error is { } err ? $" ({err.Message})" : string.Empty;
                report.Errors.Add($"{name}: does not solve '{solution.LevelId}' — outcome {result.Outcome}{detail}");
                return;
            }

            report.Loaded.Add($"solution: {name} solves {solution.LevelId} @ tick {result.Stats.FinalTick}, footprint {result.Stats.Footprint}");
        }
        catch (ContentException e)
        {
            report.Errors.Add(e.Message);
        }
    }
}
