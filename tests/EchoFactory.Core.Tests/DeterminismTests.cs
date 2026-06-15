using EchoFactory.Core;
using Xunit;

namespace EchoFactory.Core.Tests;

[Trait("Category", "Determinism")]
public class DeterminismTests
{
    [Fact]
    public void Compile_ProducesBitIdenticalTimeline()
    {
        var first = SimulationCompiler.Compile(TestData.LineLevel(), TestData.LineSolution());
        var second = SimulationCompiler.Compile(TestData.LineLevel(), TestData.LineSolution());

        Assert.Equal(StateHasher.Hash(first.States), StateHasher.Hash(second.States));
        Assert.Equal(first.Stats, second.Stats);
        Assert.Equal(first.Outcome, second.Outcome);
    }

    [Fact]
    public void DifferentBuilds_ProduceDifferentHashes()
    {
        var solved = SimulationCompiler.Compile(TestData.LineLevel(), TestData.LineSolution());

        // Drop one belt: the item strands and the timeline diverges.
        var shorter = new Build
        {
            Nodes =
            [
                PlacedNode.Belt(new GridPoint(1, 1), Direction.Right),
                PlacedNode.Belt(new GridPoint(2, 1), Direction.Right),
            ],
        };
        var partial = SimulationCompiler.Compile(TestData.LineLevel(), shorter);

        Assert.NotEqual(StateHasher.Hash(solved.States), StateHasher.Hash(partial.States));
    }
}
