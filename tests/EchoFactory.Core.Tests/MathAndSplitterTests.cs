using EchoFactory.Core;
using Xunit;

namespace EchoFactory.Core.Tests;

public class MathAndSplitterTests
{
    // Two generators feed an adder from the left and from above; result flows to the sink.
    private static LevelDefinition AdderLevel(int genBValue, MathOperation _) => new()
    {
        Id = "math",
        Grid = new GridSize(5, 3),
        MaxTicks = 12,
        Generators =
        [
            new GeneratorSpec { Id = "genA", Position = new GridPoint(0, 1), Output = Direction.Right, Schedule = [new SpawnEntry(0, 6)] },
            new GeneratorSpec { Id = "genB", Position = new GridPoint(0, 0), Output = Direction.Right, Schedule = [new SpawnEntry(0, genBValue)] },
        ],
        Sinks = [new SinkSpec { Id = "sink", Position = new GridPoint(4, 1), Expected = [] }],
    };

    private static Build AdderBuild(MathOperation op) => new()
    {
        Nodes =
        [
            PlacedNode.Belt(new GridPoint(1, 1), Direction.Right),
            PlacedNode.Belt(new GridPoint(1, 0), Direction.Right),
            PlacedNode.Belt(new GridPoint(2, 0), Direction.Down),
            PlacedNode.MathOp(new GridPoint(2, 1), new MathConfig { Operation = op, Output = Direction.Right }),
            PlacedNode.Belt(new GridPoint(3, 1), Direction.Right),
        ],
    };

    [Fact]
    public void Adder_SumsTwoOperands_FromJsonReadyConfig()
    {
        // 6 (from genA, absorbed first) + 3 (from genB) = 9.
        var level = AdderLevel(genBValue: 3, MathOperation.Add);
        level = new LevelDefinition
        {
            Id = level.Id, Grid = level.Grid, MaxTicks = level.MaxTicks,
            Generators = level.Generators,
            Sinks = [new SinkSpec { Id = "sink", Position = new GridPoint(4, 1), Expected = [9] }],
        };

        var result = SimulationCompiler.Compile(level, AdderBuild(MathOperation.Add));

        Assert.Equal(LevelOutcome.Solved, result.Outcome);
        Assert.Equal(5, result.Stats.Footprint); // 4 belts + 1 math
    }

    [Fact]
    public void Subtract_RespectsArrivalOrder()
    {
        // genA(6) absorbed first, genB(3) second -> 6 - 3 = 3.
        var level = new LevelDefinition
        {
            Id = "sub", Grid = new GridSize(5, 3), MaxTicks = 12,
            Generators =
            [
                new GeneratorSpec { Id = "genA", Position = new GridPoint(0, 1), Output = Direction.Right, Schedule = [new SpawnEntry(0, 6)] },
                new GeneratorSpec { Id = "genB", Position = new GridPoint(0, 0), Output = Direction.Right, Schedule = [new SpawnEntry(0, 3)] },
            ],
            Sinks = [new SinkSpec { Id = "sink", Position = new GridPoint(4, 1), Expected = [3] }],
        };

        var result = SimulationCompiler.Compile(level, AdderBuild(MathOperation.Sub));

        Assert.Equal(LevelOutcome.Solved, result.Outcome);
    }

    [Fact]
    public void DivisionByZero_IsMathParadox()
    {
        // genA(6) / genB(0) -> math paradox.
        var level = AdderLevel(genBValue: 0, MathOperation.Div);

        var result = SimulationCompiler.Compile(level, AdderBuild(MathOperation.Div));

        Assert.Equal(LevelOutcome.Paradox, result.Outcome);
        Assert.Equal(ParadoxKind.Math, result.Error!.Kind);
    }

    [Fact]
    public void Splitter_AlternatesBetweenTwoOutputs()
    {
        var level = new LevelDefinition
        {
            Id = "split",
            Grid = new GridSize(3, 3),
            MaxTicks = 12,
            Generators =
            [
                new GeneratorSpec
                {
                    Id = "gen",
                    Position = new GridPoint(0, 1),
                    Output = Direction.Right,
                    Schedule = [new SpawnEntry(0, 1), new SpawnEntry(2, 2)],
                },
            ],
            Sinks =
            [
                new SinkSpec { Id = "up", Position = new GridPoint(2, 0), Expected = [1] },   // first item
                new SinkSpec { Id = "down", Position = new GridPoint(2, 2), Expected = [2] }, // second item
            ],
        };
        var build = new Build
        {
            Nodes =
            [
                PlacedNode.Belt(new GridPoint(1, 1), Direction.Right),
                PlacedNode.Split(new GridPoint(2, 1), new SplitterConfig
                {
                    OutputA = Direction.Up,
                    OutputB = Direction.Down,
                    StartWithA = true,
                }),
            ],
        };

        var result = SimulationCompiler.Compile(level, build);

        Assert.Equal(LevelOutcome.Solved, result.Outcome);
    }
}
