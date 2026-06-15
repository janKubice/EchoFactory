using EchoFactory.Core;
using Xunit;

namespace EchoFactory.Core.Tests;

[Trait("Category", "Determinism")]
public class TemporalTests
{
    // Seed -> belts -> portal(Δ=2, exit down) -> belts -> sink.
    // The portal sends the item 2 ticks back, so it reaches the sink earlier than its
    // forward path ever could. Stable causal loop -> converges in 2 passes.
    private static (LevelDefinition Level, Build Build) BackwardDelivery()
    {
        var level = new LevelDefinition
        {
            Id = "loop_back",
            Grid = new GridSize(6, 4),
            MaxTicks = 15,
            Generators =
            [
                new GeneratorSpec { Id = "gen", Position = new GridPoint(0, 1), Output = Direction.Right, Schedule = [new SpawnEntry(0, 7)] },
            ],
            Sinks = [new SinkSpec { Id = "sink", Position = new GridPoint(5, 2), Expected = [7] }],
        };
        var build = new Build
        {
            Nodes =
            [
                PlacedNode.Belt(new GridPoint(1, 1), Direction.Right),
                PlacedNode.Belt(new GridPoint(2, 1), Direction.Right),
                PlacedNode.TimePortal(new GridPoint(3, 1), new PortalConfig { TimeOffset = 2, Output = Direction.Down }),
                PlacedNode.Belt(new GridPoint(3, 2), Direction.Right),
                PlacedNode.Belt(new GridPoint(4, 2), Direction.Right),
            ],
        };
        return (level, build);
    }

    [Fact]
    public void BackwardPortal_StableLoop_Converges_AndDeliversEarlier()
    {
        var (level, build) = BackwardDelivery();

        var result = SimulationCompiler.Compile(level, build);

        Assert.Equal(LevelOutcome.Solved, result.Outcome);
        Assert.Equal(2, result.Stats.Passes);   // fixed point reached on the 2nd pass
        Assert.Equal(4, result.Stats.FinalTick); // delivered at tick 4 (it travelled back in time)
    }

    [Fact]
    public void TemporalCompile_IsDeterministic()
    {
        var (level, build) = BackwardDelivery();

        var a = SimulationCompiler.Compile(level, build);
        var b = SimulationCompiler.Compile(level, build);

        Assert.Equal(StateHasher.Hash(a.States), StateHasher.Hash(b.States));
        Assert.Equal(a.Stats, b.Stats);
    }

    [Fact]
    public void ForwardPortal_Delay_Converges()
    {
        var level = new LevelDefinition
        {
            Id = "loop_fwd",
            Grid = new GridSize(6, 3),
            MaxTicks = 15,
            Generators =
            [
                new GeneratorSpec { Id = "gen", Position = new GridPoint(0, 1), Output = Direction.Right, Schedule = [new SpawnEntry(0, 3)] },
            ],
            Sinks = [new SinkSpec { Id = "sink", Position = new GridPoint(4, 1), Expected = [3] }],
        };
        var build = new Build
        {
            Nodes =
            [
                PlacedNode.Belt(new GridPoint(1, 1), Direction.Right),
                PlacedNode.TimePortal(new GridPoint(2, 1), new PortalConfig { TimeOffset = -3, Output = Direction.Right }),
                PlacedNode.Belt(new GridPoint(3, 1), Direction.Right),
            ],
        };

        var result = SimulationCompiler.Compile(level, build);

        Assert.Equal(LevelOutcome.Solved, result.Outcome);
        Assert.Equal(2, result.Stats.Passes);
    }

    [Fact]
    public void SelfFeedingLoop_NeverConverges_IsTemporalParadox()
    {
        // Portal exit feeds a belt straight back into the portal, re-absorbing forever.
        var level = new LevelDefinition
        {
            Id = "loop_unstable",
            Grid = new GridSize(6, 3),
            MaxTicks = 20,
            Generators =
            [
                new GeneratorSpec { Id = "gen", Position = new GridPoint(0, 1), Output = Direction.Right, Schedule = [new SpawnEntry(0, 5)] },
            ],
            Sinks = [],
        };
        var build = new Build
        {
            Nodes =
            [
                PlacedNode.Belt(new GridPoint(1, 1), Direction.Right),
                PlacedNode.Belt(new GridPoint(2, 1), Direction.Right),
                PlacedNode.TimePortal(new GridPoint(3, 1), new PortalConfig { TimeOffset = 1, Output = Direction.Right }),
                PlacedNode.Belt(new GridPoint(4, 1), Direction.Left), // exit feeds back into the portal
            ],
        };

        var result = SimulationCompiler.Compile(level, build);

        Assert.Equal(LevelOutcome.Paradox, result.Outcome);
        Assert.Equal(ParadoxKind.Temporal, result.Error!.Kind);
    }
}
