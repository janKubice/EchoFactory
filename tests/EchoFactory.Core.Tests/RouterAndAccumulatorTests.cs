using EchoFactory.Core;
using Xunit;

namespace EchoFactory.Core.Tests;

public class RouterAndAccumulatorTests
{
    [Fact]
    public void RouterConfig_Matches_EvaluatesComparison()
    {
        var cfg = new RouterConfig { Comparison = Comparison.Ge, Constant = 5, OutMatch = Direction.Right, OutElse = Direction.Left };
        Assert.True(cfg.Matches(5));
        Assert.True(cfg.Matches(9));
        Assert.False(cfg.Matches(4));
    }

    [Fact]
    public void AccumulatorConfig_Releases_EvaluatesComparison()
    {
        var cfg = new AccumulatorConfig { ReleaseWhen = Comparison.Ge, Constant = 10, Output = Direction.Right };
        Assert.False(cfg.Releases(9));
        Assert.True(cfg.Releases(10));
        Assert.True(cfg.Releases(11));
    }

    /// <summary>
    /// A single seed value circles a 4-cell belt loop; each lap a unary +1 increments it and a
    /// router checks the running value, sending it back around until it reaches the target and
    /// then out to the sink. This is the player's "loop until done" process loop.
    /// </summary>
    [Fact]
    public void Router_SpatialLoop_CountsToTargetThenExits()
    {
        var level = new LevelDefinition
        {
            Id = "loop_counter",
            Grid = new GridSize(4, 3),
            MaxTicks = 40,
            Generators =
            [
                new GeneratorSpec
                {
                    Id = "gen",
                    Position = new GridPoint(0, 1),
                    Output = Direction.Right,
                    Schedule = [new SpawnEntry(0, 1)],
                },
            ],
            Sinks = [new SinkSpec { Id = "sink", Position = new GridPoint(3, 2), Expected = [5] }],
        };
        var build = new Build
        {
            Nodes =
            [
                PlacedNode.MathOp(new GridPoint(1, 1), new MathConfig { Operation = MathOperation.Add, Output = Direction.Right, Constant = 1 }),
                PlacedNode.Belt(new GridPoint(2, 1), Direction.Down),
                PlacedNode.Route(new GridPoint(2, 2), new RouterConfig { Comparison = Comparison.Ge, Constant = 5, OutMatch = Direction.Right, OutElse = Direction.Left }),
                PlacedNode.Belt(new GridPoint(1, 2), Direction.Up),
            ],
        };

        var result = SimulationCompiler.Compile(level, build);

        Assert.Equal(LevelOutcome.Solved, result.Outcome);
    }

    /// <summary>An accumulator folds a stream of ones and releases the total once it reaches the target.</summary>
    [Fact]
    public void Accumulator_SumsStream_ReleasesOnThreshold()
    {
        var level = new LevelDefinition
        {
            Id = "tally",
            Grid = new GridSize(4, 3),
            MaxTicks = 20,
            Generators =
            [
                new GeneratorSpec
                {
                    Id = "gen",
                    Position = new GridPoint(0, 1),
                    Output = Direction.Right,
                    Schedule = [new SpawnEntry(0, 1), new SpawnEntry(1, 1), new SpawnEntry(2, 1), new SpawnEntry(3, 1), new SpawnEntry(4, 1)],
                },
            ],
            Sinks = [new SinkSpec { Id = "sink", Position = new GridPoint(3, 1), Expected = [5] }],
        };
        var build = new Build
        {
            Nodes =
            [
                PlacedNode.Belt(new GridPoint(1, 1), Direction.Right),
                PlacedNode.Accumulate(new GridPoint(2, 1), new AccumulatorConfig { ReleaseWhen = Comparison.Ge, Constant = 5, Output = Direction.Right }),
            ],
        };

        var result = SimulationCompiler.Compile(level, build);

        Assert.Equal(LevelOutcome.Solved, result.Outcome);
    }

    [Fact]
    public void Accumulator_WithInitialOffset_AndReset_ReleasesTwice()
    {
        // Initial 10; release at >= 12 means two ones release a 12, reset to 10, two more release another 12.
        var level = new LevelDefinition
        {
            Id = "tally2",
            Grid = new GridSize(4, 3),
            MaxTicks = 20,
            Generators =
            [
                new GeneratorSpec
                {
                    Id = "gen",
                    Position = new GridPoint(0, 1),
                    Output = Direction.Right,
                    Schedule = [new SpawnEntry(0, 1), new SpawnEntry(1, 1), new SpawnEntry(2, 1), new SpawnEntry(3, 1)],
                },
            ],
            Sinks = [new SinkSpec { Id = "sink", Position = new GridPoint(3, 1), Expected = [12, 12] }],
        };
        var build = new Build
        {
            Nodes =
            [
                PlacedNode.Belt(new GridPoint(1, 1), Direction.Right),
                PlacedNode.Accumulate(new GridPoint(2, 1), new AccumulatorConfig { ReleaseWhen = Comparison.Ge, Constant = 12, Output = Direction.Right, Initial = 10 }),
            ],
        };

        var result = SimulationCompiler.Compile(level, build);

        Assert.Equal(LevelOutcome.Solved, result.Outcome);
    }

    [Fact]
    public void RouterAndAccumulator_AreDeterministic()
    {
        var level = new LevelDefinition
        {
            Id = "loop_counter",
            Grid = new GridSize(4, 3),
            MaxTicks = 40,
            Generators = [new GeneratorSpec { Id = "gen", Position = new GridPoint(0, 1), Output = Direction.Right, Schedule = [new SpawnEntry(0, 1)] }],
            Sinks = [new SinkSpec { Id = "sink", Position = new GridPoint(3, 2), Expected = [5] }],
        };
        var build = new Build
        {
            Nodes =
            [
                PlacedNode.MathOp(new GridPoint(1, 1), new MathConfig { Operation = MathOperation.Add, Output = Direction.Right, Constant = 1 }),
                PlacedNode.Belt(new GridPoint(2, 1), Direction.Down),
                PlacedNode.Route(new GridPoint(2, 2), new RouterConfig { Comparison = Comparison.Ge, Constant = 5, OutMatch = Direction.Right, OutElse = Direction.Left }),
                PlacedNode.Belt(new GridPoint(1, 2), Direction.Up),
            ],
        };

        var a = SimulationCompiler.Compile(level, build);
        var b = SimulationCompiler.Compile(level, build);

        Assert.Equal(StateHasher.Hash(a.States), StateHasher.Hash(b.States));
    }
}
