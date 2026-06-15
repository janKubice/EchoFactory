using EchoFactory.Core;

namespace EchoFactory.Core.Tests;

/// <summary>Hand-built levels and builds for engine tests (M1 has no JSON loading yet).</summary>
internal static class TestData
{
    /// <summary>A solvable horizontal line: generator (0,1) → belts → sink (5,1).</summary>
    public static LevelDefinition LineLevel(int maxTicks = 20, IReadOnlyList<int>? expected = null) => new()
    {
        Id = "test_line",
        Grid = new GridSize(6, 3),
        MaxTicks = maxTicks,
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
            new SinkSpec { Id = "sink", Position = new GridPoint(5, 1), Expected = expected ?? [1, 2, 3] },
        ],
    };

    /// <summary>The reference 4-belt solution for <see cref="LineLevel"/>.</summary>
    public static Build LineSolution() => new()
    {
        Nodes =
        [
            PlacedNode.Belt(new GridPoint(1, 1), Direction.Right),
            PlacedNode.Belt(new GridPoint(2, 1), Direction.Right),
            PlacedNode.Belt(new GridPoint(3, 1), Direction.Right),
            PlacedNode.Belt(new GridPoint(4, 1), Direction.Right),
        ],
    };
}
