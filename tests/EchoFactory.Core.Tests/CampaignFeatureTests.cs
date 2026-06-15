using EchoFactory.Core;
using Xunit;

namespace EchoFactory.Core.Tests;

public class CampaignFeatureTests
{
    [Fact]
    public void UnaryMath_WithConstant_TransformsEachItem()
    {
        // 4 * 3 = 12, using a single-input math node with a constant operand.
        var level = new LevelDefinition
        {
            Id = "unary",
            Grid = new GridSize(5, 3),
            MaxTicks = 12,
            Generators =
            [
                new GeneratorSpec { Id = "gen", Position = new GridPoint(0, 1), Output = Direction.Right, Schedule = [new SpawnEntry(0, 4)] },
            ],
            Sinks = [new SinkSpec { Id = "sink", Position = new GridPoint(4, 1), Expected = [12] }],
        };
        var build = new Build
        {
            Nodes =
            [
                PlacedNode.Belt(new GridPoint(1, 1), Direction.Right),
                PlacedNode.MathOp(new GridPoint(2, 1), new MathConfig { Operation = MathOperation.Mul, Output = Direction.Right, Constant = 3 }),
                PlacedNode.Belt(new GridPoint(3, 1), Direction.Right),
            ],
        };

        var result = SimulationCompiler.Compile(level, build);

        Assert.Equal(LevelOutcome.Solved, result.Outcome);
    }

    [Fact]
    public void StarRating_ReflectsParThresholds()
    {
        // Reference line solution: solved at tick 7 with footprint 4.
        var result = SimulationCompiler.Compile(TestData.LineLevel(), TestData.LineSolution());

        Assert.Equal(3, StarRating.Compute(result, new LevelPar { Ticks = 7, Footprint = 4 })); // meets both
        Assert.Equal(2, StarRating.Compute(result, new LevelPar { Ticks = 6, Footprint = 4 })); // misses tick par
        Assert.Equal(1, StarRating.Compute(result, new LevelPar { Ticks = 6, Footprint = 3 })); // misses both
        Assert.Equal(1, StarRating.Compute(result, par: null));                                  // solved, unrated
    }

    [Fact]
    public void StarRating_IsZero_WhenNotSolved()
    {
        // Same level, but the line is one belt short → the item strands (Void paradox).
        var shortBuild = new Build
        {
            Nodes =
            [
                PlacedNode.Belt(new GridPoint(1, 1), Direction.Right),
                PlacedNode.Belt(new GridPoint(2, 1), Direction.Right),
            ],
        };

        var result = SimulationCompiler.Compile(TestData.LineLevel(), shortBuild);

        Assert.Equal(0, StarRating.Compute(result, new LevelPar { Ticks = 7, Footprint = 4 }));
    }
}
