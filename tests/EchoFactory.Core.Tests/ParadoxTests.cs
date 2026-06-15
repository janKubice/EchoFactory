using EchoFactory.Core;
using Xunit;

namespace EchoFactory.Core.Tests;

public class ParadoxTests
{
    [Fact]
    public void StrandedItem_OnCellWithNoNode_IsVoidParadox()
    {
        var level = new LevelDefinition
        {
            Id = "void_stranded",
            Grid = new GridSize(4, 3),
            MaxTicks = 10,
            Generators =
            [
                new GeneratorSpec
                {
                    Id = "g",
                    Position = new GridPoint(0, 1),
                    Output = Direction.Right,
                    Schedule = [new SpawnEntry(0, 5)],
                },
            ],
            Sinks = [new SinkSpec { Id = "s", Position = new GridPoint(3, 1), Expected = [5] }],
        };

        // No belts: the spawned item has nowhere to go.
        var result = SimulationCompiler.Compile(level, new Build());

        Assert.Equal(LevelOutcome.Paradox, result.Outcome);
        Assert.Equal(ParadoxKind.Void, result.Error!.Kind);
    }

    [Fact]
    public void BeltPointingOffGrid_IsVoidParadox()
    {
        var level = new LevelDefinition
        {
            Id = "void_offgrid",
            Grid = new GridSize(3, 3),
            MaxTicks = 10,
            Generators =
            [
                new GeneratorSpec
                {
                    Id = "g",
                    Position = new GridPoint(0, 0),
                    Output = Direction.Right,
                    Schedule = [new SpawnEntry(0, 1)],
                },
            ],
        };
        var build = new Build
        {
            Belts =
            [
                new BeltPlacement(new GridPoint(1, 0), Direction.Right),
                new BeltPlacement(new GridPoint(2, 0), Direction.Right), // pushes off the right edge
            ],
        };

        var result = SimulationCompiler.Compile(level, build);

        Assert.Equal(ParadoxKind.Void, result.Error!.Kind);
    }

    [Fact]
    public void TwoItemsMeetingOnACell_IsCollisionParadox()
    {
        var level = new LevelDefinition
        {
            Id = "collision",
            Grid = new GridSize(3, 3),
            MaxTicks = 10,
            Generators =
            [
                new GeneratorSpec
                {
                    Id = "a",
                    Position = new GridPoint(0, 1),
                    Output = Direction.Right, // spawns (1,1)
                    Schedule = [new SpawnEntry(0, 1)],
                },
                new GeneratorSpec
                {
                    Id = "b",
                    Position = new GridPoint(2, 1),
                    Output = Direction.Left, // also spawns (1,1)
                    Schedule = [new SpawnEntry(0, 2)],
                },
            ],
        };

        var result = SimulationCompiler.Compile(level, new Build());

        Assert.Equal(ParadoxKind.Collision, result.Error!.Kind);
        Assert.Equal(2, result.Error!.Items.Count);
    }
}
