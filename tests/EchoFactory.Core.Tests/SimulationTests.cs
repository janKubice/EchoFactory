using EchoFactory.Core;
using Xunit;

namespace EchoFactory.Core.Tests;

public class SimulationTests
{
    [Fact]
    public void ReferenceSolution_Solves_WithExpectedStats()
    {
        var result = SimulationCompiler.Compile(TestData.LineLevel(), TestData.LineSolution());

        Assert.True(result.IsSuccess);
        Assert.Equal(LevelOutcome.Solved, result.Outcome);
        Assert.Null(result.Error);
        Assert.Equal(4, result.Stats.Footprint);   // 4 belts
        Assert.Equal(7, result.Stats.FinalTick);    // last item delivered on tick 7
        Assert.Equal(1, result.Stats.Passes);       // single pass (no portals in M1)
    }

    [Fact]
    public void Belt_MovesItem_OneCellPerTick()
    {
        var result = SimulationCompiler.Compile(TestData.LineLevel(), TestData.LineSolution());

        // First item (value 1) spawns at (1,1) in state[1] and marches right one cell per tick.
        Assert.True(result.States[1].TryGetItem(new GridPoint(1, 1), out var spawned));
        Assert.Equal(1, spawned.Value);
        Assert.True(result.States[2].TryGetItem(new GridPoint(2, 1), out _));
        Assert.True(result.States[3].TryGetItem(new GridPoint(3, 1), out _));
    }

    [Fact]
    public void WrongExpectedSequence_Fails_WithoutParadox()
    {
        // Line delivers [1,2,3]; level demands a 4th item that never arrives.
        var level = TestData.LineLevel(expected: [1, 2, 3, 4]);

        var result = SimulationCompiler.Compile(level, TestData.LineSolution());

        Assert.Equal(LevelOutcome.Failed, result.Outcome);
        Assert.Null(result.Error); // Failed goal is not a paradox
    }

    [Fact]
    public void ItemId_IsDeterministic_ByProvenance()
    {
        var a = ItemId.FromSpawn(NodeId.FromString("gen"), tick: 3, sequence: 0);
        var b = ItemId.FromSpawn(NodeId.FromString("gen"), tick: 3, sequence: 0);
        var c = ItemId.FromSpawn(NodeId.FromString("gen"), tick: 3, sequence: 1);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
