namespace EchoFactory.Core;

/// <summary>
/// Built-in example level + solution, shared by the CLI demo and as a sanity reference.
/// (Real content comes from JSON in M2.)
/// </summary>
public static class Examples
{
    /// <summary>A solvable line: generator (0,1) emits 1,2,3 → belts → sink (5,1).</summary>
    public static LevelDefinition LineDemoLevel() => new()
    {
        Id = "demo_line",
        Grid = new GridSize(6, 3),
        MaxTicks = 20,
        Generators =
        [
            new GeneratorSpec
            {
                Id = "gen",
                Position = new GridPoint(0, 1),
                Output = Direction.Right,
                Schedule = [new SpawnEntry(0, 1), new SpawnEntry(1, 2), new SpawnEntry(2, 3)],
            },
        ],
        Sinks =
        [
            new SinkSpec { Id = "sink", Position = new GridPoint(5, 1), Expected = [1, 2, 3] },
        ],
    };

    public static Build LineDemoSolution() => new()
    {
        Belts =
        [
            new BeltPlacement(new GridPoint(1, 1), Direction.Right),
            new BeltPlacement(new GridPoint(2, 1), Direction.Right),
            new BeltPlacement(new GridPoint(3, 1), Direction.Right),
            new BeltPlacement(new GridPoint(4, 1), Direction.Right),
        ],
    };
}
