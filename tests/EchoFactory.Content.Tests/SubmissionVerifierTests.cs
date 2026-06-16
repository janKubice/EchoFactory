using EchoFactory.Content;
using EchoFactory.Core;
using Xunit;

namespace EchoFactory.Content.Tests;

public class SubmissionVerifierTests
{
    private const string LevelJson = """
    { "schema_version": 1, "id": "lvl", "grid": { "width": 6, "height": 3 }, "max_ticks": 20,
      "par": { "ticks": 7, "footprint": 4 },
      "inventory": { "mode": "whitelist", "allowed": ["node_belt"], "limits": { "node_belt": 6 } },
      "fixed_nodes": [
        { "id": "gen", "type": "generator", "position": { "x": 0, "y": 1 }, "direction": "right",
          "schedule": [ { "tick": 0, "value": 1 }, { "tick": 1, "value": 2 }, { "tick": 2, "value": 3 } ] },
        { "id": "sink", "type": "sink", "position": { "x": 5, "y": 1 }, "expected": [1, 2, 3] } ] }
    """;

    private const string GoodSolution = """
    { "schema_version": 1, "level_id": "lvl", "placed_nodes": [
      { "node": "node_belt", "position": { "x": 1, "y": 1 }, "direction": "right" },
      { "node": "node_belt", "position": { "x": 2, "y": 1 }, "direction": "right" },
      { "node": "node_belt", "position": { "x": 3, "y": 1 }, "direction": "right" },
      { "node": "node_belt", "position": { "x": 4, "y": 1 }, "direction": "right" } ] }
    """;

    private static (LevelDefinition Level, SolutionInfo Solution) Load(string solutionJson)
    {
        var registry = new NodeRegistry();
        registry.Add(NodeRegistry.ParseDefinition("""{ "schema_version":1, "id":"node_belt", "type":"belt" }""", "belt"));
        return (LevelLoader.Parse(LevelJson, "lvl"), SolutionLoader.Parse(solutionJson, "sol", registry));
    }

    [Fact]
    public void Accepts_CorrectSubmission_WithMatchingClaims()
    {
        var (level, solution) = Load(GoodSolution);

        Assert.True(SubmissionVerifier.Verify(level, solution, null, null).Accepted);

        VerifyResult verdict = SubmissionVerifier.Verify(level, solution, claimedTicks: 7, claimedFootprint: 4);
        Assert.True(verdict.Accepted);
        Assert.Equal(7, verdict.Ticks);
        Assert.Equal(4, verdict.Footprint);
        Assert.Equal(3, verdict.Stars);
    }

    [Fact]
    public void Rejects_FalseClaims()
    {
        var (level, solution) = Load(GoodSolution);

        Assert.False(SubmissionVerifier.Verify(level, solution, claimedTicks: 5, claimedFootprint: 4).Accepted);
        Assert.False(SubmissionVerifier.Verify(level, solution, claimedTicks: 7, claimedFootprint: 2).Accepted);
    }

    [Fact]
    public void Rejects_SolutionThatDoesNotSolve()
    {
        const string tooShort = """
        { "schema_version": 1, "level_id": "lvl", "placed_nodes": [
          { "node": "node_belt", "position": { "x": 1, "y": 1 }, "direction": "right" },
          { "node": "node_belt", "position": { "x": 2, "y": 1 }, "direction": "right" } ] }
        """;
        var (level, solution) = Load(tooShort);

        Assert.False(SubmissionVerifier.Verify(level, solution, null, null).Accepted);
    }
}
