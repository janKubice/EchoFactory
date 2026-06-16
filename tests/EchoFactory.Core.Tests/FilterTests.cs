using EchoFactory.Core;
using Xunit;

namespace EchoFactory.Core.Tests;

public class FilterTests
{
    [Fact]
    public void FilterConfig_Passes_EvaluatesComparison()
    {
        Assert.True(new FilterConfig { Comparison = Comparison.Ge, Constant = 3, Output = Direction.Right }.Passes(3));
        Assert.False(new FilterConfig { Comparison = Comparison.Ge, Constant = 3, Output = Direction.Right }.Passes(2));
        Assert.True(new FilterConfig { Comparison = Comparison.Eq, Constant = 7, Output = Direction.Right }.Passes(7));
        Assert.True(new FilterConfig { Comparison = Comparison.Ne, Constant = 0, Output = Direction.Right }.Passes(5));
    }

    [Fact]
    public void FilterNode_PassesMatching_DropsRest()
    {
        // Generator emits 1..5; the gate passes only values >= 3; the sink wants [3,4,5].
        var level = new LevelDefinition
        {
            Id = "filter",
            Grid = new GridSize(6, 3),
            MaxTicks = 15,
            Generators =
            [
                new GeneratorSpec
                {
                    Id = "g",
                    Position = new GridPoint(0, 1),
                    Output = Direction.Right,
                    Schedule = [new SpawnEntry(0, 1), new SpawnEntry(1, 2), new SpawnEntry(2, 3), new SpawnEntry(3, 4), new SpawnEntry(4, 5)],
                },
            ],
            Sinks = [new SinkSpec { Id = "s", Position = new GridPoint(4, 1), Expected = [3, 4, 5] }],
        };
        var build = new Build
        {
            Nodes =
            [
                PlacedNode.Belt(new GridPoint(1, 1), Direction.Right),
                PlacedNode.Gate(new GridPoint(2, 1), new FilterConfig { Comparison = Comparison.Ge, Constant = 3, Output = Direction.Right }),
                PlacedNode.Belt(new GridPoint(3, 1), Direction.Right),
            ],
        };

        var result = SimulationCompiler.Compile(level, build);

        Assert.Equal(LevelOutcome.Solved, result.Outcome);
    }
}
