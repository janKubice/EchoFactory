using EchoFactory.Content;
using EchoFactory.Core;
using Xunit;

namespace EchoFactory.Content.Tests;

public class WriterTests
{
    [Fact]
    public void SolutionWriter_RoundTrips_AllNodeKinds()
    {
        var registry = new NodeRegistry();
        registry.Add(NodeRegistry.ParseDefinition("""{ "schema_version":1, "id":"node_belt", "type":"belt" }""", "b"));
        registry.Add(NodeRegistry.ParseDefinition("""{ "schema_version":1, "id":"node_math_mul", "type":"math", "logic":{"operation":"mul"} }""", "m"));
        registry.Add(NodeRegistry.ParseDefinition("""{ "schema_version":1, "id":"node_splitter", "type":"splitter" }""", "s"));
        registry.Add(NodeRegistry.ParseDefinition("""{ "schema_version":1, "id":"node_portal", "type":"portal" }""", "p"));
        registry.Add(NodeRegistry.ParseDefinition("""{ "schema_version":1, "id":"node_filter", "type":"filter" }""", "f"));

        var build = new Build
        {
            Nodes =
            [
                PlacedNode.Belt(new GridPoint(1, 1), Direction.Right),
                PlacedNode.MathOp(new GridPoint(2, 1), new MathConfig { Operation = MathOperation.Mul, Output = Direction.Right, Constant = 3 }),
                PlacedNode.Split(new GridPoint(3, 1), new SplitterConfig { OutputA = Direction.Up, OutputB = Direction.Down }),
                PlacedNode.TimePortal(new GridPoint(4, 1), new PortalConfig { TimeOffset = 2, Output = Direction.Down }),
                PlacedNode.Gate(new GridPoint(5, 1), new FilterConfig { Comparison = Comparison.Ge, Constant = 3, Output = Direction.Right }),
            ],
        };

        string json1 = SolutionWriter.Write("lvl", build);
        SolutionInfo parsed = SolutionLoader.Parse(json1, "sol", registry);
        string json2 = SolutionWriter.Write("lvl", parsed.Build);

        Assert.Equal(json1, json2);
        Assert.Equal(5, parsed.Build.Footprint);
    }

    [Fact]
    public void LevelWriter_RoundTrips_RichLevel()
    {
        var level = new LevelDefinition
        {
            Id = "lvl_rt",
            Grid = new GridSize(6, 3),
            MaxTicks = 20,
            MaxTemporalPasses = 5,
            StrictTiming = true,
            Par = new LevelPar { Ticks = 7, Footprint = 4 },
            Inventory = new LevelInventory
            {
                Mode = InventoryMode.Whitelist,
                Listed = new HashSet<string> { "node_belt" },
                Limits = new Dictionary<string, int> { ["node_belt"] = 6 },
            },
            Generators = [new GeneratorSpec { Id = "gen", Position = new GridPoint(0, 1), Output = Direction.Right, Schedule = [new SpawnEntry(0, 1), new SpawnEntry(1, 2)] }],
            Sinks = [new SinkSpec { Id = "sink", Position = new GridPoint(5, 1), Expected = [1, 2], ExpectedTicks = [6, 7] }],
        };

        string json1 = LevelWriter.Write(level);
        LevelDefinition back = LevelLoader.Parse(json1, "lvl");
        string json2 = LevelWriter.Write(back);

        Assert.Equal(json1, json2);
        Assert.True(back.StrictTiming);
        Assert.Equal(7, back.Par!.Ticks);
        Assert.True(back.Inventory!.Allows("node_belt"));
        Assert.Equal(6, back.Inventory.LimitFor("node_belt"));
        Assert.Equal([6, 7], back.Sinks[0].ExpectedTicks!);
    }
}
