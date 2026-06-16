using EchoFactory.Core;
using Xunit;

namespace EchoFactory.Core.Tests;

public class StrictTimingTests
{
    private static LevelDefinition Level(bool strict, IReadOnlyList<int>? expectedTicks) => new()
    {
        Id = "timed",
        Grid = new GridSize(5, 1),
        MaxTicks = 10,
        StrictTiming = strict,
        Generators =
        [
            new GeneratorSpec { Id = "g", Position = new GridPoint(0, 0), Output = Direction.Right, Schedule = [new SpawnEntry(0, 1)] },
        ],
        Sinks = [new SinkSpec { Id = "s", Position = new GridPoint(4, 0), Expected = [1], ExpectedTicks = expectedTicks }],
    };

    private static Build Belts() => new()
    {
        Nodes =
        [
            PlacedNode.Belt(new GridPoint(1, 0), Direction.Right),
            PlacedNode.Belt(new GridPoint(2, 0), Direction.Right),
            PlacedNode.Belt(new GridPoint(3, 0), Direction.Right),
        ],
    };

    [Fact]
    public void StrictTiming_RequiresExactArrivalTick()
    {
        Build build = Belts();

        // Discover the actual arrival tick from a loose (non-strict) run.
        var loose = SimulationCompiler.Compile(Level(strict: false, expectedTicks: null), build);
        Assert.Equal(LevelOutcome.Solved, loose.Outcome);
        int tick = loose.Stats.FinalTick;

        Assert.Equal(LevelOutcome.Solved, SimulationCompiler.Compile(Level(true, [tick]), build).Outcome);
        Assert.Equal(LevelOutcome.Failed, SimulationCompiler.Compile(Level(true, [tick + 1]), build).Outcome);
    }

    [Fact]
    public void NonStrict_IgnoresArrivalTick()
    {
        // A wrong tick list is ignored when the level is not strict — only values matter.
        var result = SimulationCompiler.Compile(Level(strict: false, expectedTicks: [999]), Belts());
        Assert.Equal(LevelOutcome.Solved, result.Outcome);
    }
}
