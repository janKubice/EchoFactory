using System.Text.Json;
using EchoFactory.Content;
using EchoFactory.Core;
using Xunit;

namespace EchoFactory.Content.Tests;

public class ContentTests
{
    [Fact]
    public void ParsesMathNodeDefinition()
    {
        const string json = """
        { "schema_version": 1, "id": "node_math_add", "name": "Adder",
          "type": "math", "category": "Math", "logic": { "operation": "add" } }
        """;

        NodeDefinition def = NodeRegistry.ParseDefinition(json, "test");

        Assert.Equal("node_math_add", def.Id);
        Assert.Equal(NodeKind.Math, def.Kind);
        Assert.Equal(MathOperation.Add, def.Operation);
    }

    [Fact]
    public void ParsesLevel_WithGeneratorAndSink()
    {
        const string json = """
        { "schema_version": 1, "id": "lvl_x", "grid": { "width": 6, "height": 3 }, "max_ticks": 20,
          "fixed_nodes": [
            { "id": "gen", "type": "generator", "position": { "x": 0, "y": 1 }, "direction": "right",
              "schedule": [ { "tick": 0, "value": 1 } ] },
            { "id": "sink", "type": "sink", "position": { "x": 5, "y": 1 }, "expected": [1] } ] }
        """;

        LevelDefinition level = LevelLoader.Parse(json, "test");

        Assert.Equal("lvl_x", level.Id);
        Assert.Equal(6, level.Grid.Width);
        Assert.Single(level.Generators);
        Assert.Single(level.Sinks);
        Assert.Equal(Direction.Right, level.Generators[0].Output);
    }

    [Fact]
    public void FullPipeline_JsonNodesSolveMathLevel()
    {
        var registry = new NodeRegistry();
        registry.Add(NodeRegistry.ParseDefinition("""{ "schema_version":1, "id":"node_belt", "type":"belt" }""", "belt"));
        registry.Add(NodeRegistry.ParseDefinition("""{ "schema_version":1, "id":"node_math_add", "type":"math", "logic":{"operation":"add"} }""", "add"));

        const string levelJson = """
        { "schema_version":1, "id":"lvl_adder", "grid":{"width":5,"height":3}, "max_ticks":12,
          "fixed_nodes":[
            { "id":"genA","type":"generator","position":{"x":0,"y":1},"direction":"right","schedule":[{"tick":0,"value":6}] },
            { "id":"genB","type":"generator","position":{"x":0,"y":0},"direction":"right","schedule":[{"tick":0,"value":3}] },
            { "id":"sink","type":"sink","position":{"x":4,"y":1},"expected":[9] } ] }
        """;
        const string solutionJson = """
        { "schema_version":1, "level_id":"lvl_adder", "placed_nodes":[
            { "node":"node_belt","position":{"x":1,"y":1},"direction":"right" },
            { "node":"node_belt","position":{"x":1,"y":0},"direction":"right" },
            { "node":"node_belt","position":{"x":2,"y":0},"direction":"down" },
            { "node":"node_math_add","position":{"x":2,"y":1},"direction":"right" },
            { "node":"node_belt","position":{"x":3,"y":1},"direction":"right" } ] }
        """;

        var level = LevelLoader.Parse(levelJson, "lvl");
        var solution = SolutionLoader.Parse(solutionJson, "sol", registry);
        var result = SimulationCompiler.Compile(level, solution.Build);

        Assert.Equal(LevelOutcome.Solved, result.Outcome);
        Assert.Equal(5, solution.Build.Footprint);
    }

    [Theory]
    [InlineData("{ not valid json")]
    [InlineData("""{ "schema_version": 1, "type": "math" }""")]            // missing id
    [InlineData("""{ "schema_version": 1, "id": "x", "type": "wat" }""")]  // unknown type
    [InlineData("""{ "schema_version": 1, "id": "x", "type": "math" }""")] // math without operation
    public void InvalidNodeDefinition_FailsLoud(string json)
    {
        Assert.Throws<ContentException>(() => NodeRegistry.ParseDefinition(json, "bad"));
    }

    [Theory]
    [InlineData(0)]   // below minimum
    [InlineData(99)]  // newer than supported
    public void UnsupportedSchemaVersion_Throws(int version)
    {
        string json = $$"""{ "schema_version": {{version}}, "id": "node_belt", "type": "belt" }""";
        Assert.Throws<ContentException>(() => NodeRegistry.ParseDefinition(json, "ver"));
    }

    [Fact]
    public void NodeDto_RoundTrips_StableThroughSerialize()
    {
        const string json = """
        { "schema_version": 1, "id": "node_math_mul", "name": "Multiplier",
          "type": "math", "logic": { "operation": "mul" },
          "visual": { "shape": "circle", "color_hex": "#9B59B6", "icon": "x" } }
        """;

        var first = JsonSerializer.Deserialize<NodeDefinitionDto>(json, JsonConfig.Options);
        string reserialized = JsonSerializer.Serialize(first, JsonConfig.Options);
        var second = JsonSerializer.Deserialize<NodeDefinitionDto>(reserialized, JsonConfig.Options);

        Assert.Equal(first!.Id, second!.Id);
        Assert.Equal(first.SchemaVersion, second.SchemaVersion);
        Assert.Equal(first.Logic!.Operation, second.Logic!.Operation);
        Assert.Equal(first.Visual!.ColorHex, second.Visual!.ColorHex);
    }

    [Fact]
    public void InventoryCheck_DetectsViolations()
    {
        var inv = new LevelInventory
        {
            Mode = InventoryMode.Whitelist,
            Listed = new HashSet<string> { "node_belt" },
            Limits = new Dictionary<string, int> { ["node_belt"] = 2 },
        };

        Assert.Null(InventoryCheck.Violation(null, ["anything"]));                       // no inventory
        Assert.Null(InventoryCheck.Violation(inv, ["node_belt", "node_belt"]));          // within limit
        Assert.NotNull(InventoryCheck.Violation(inv, ["node_portal"]));                  // not allowed
        Assert.NotNull(InventoryCheck.Violation(inv, ["node_belt", "node_belt", "node_belt"])); // over limit
    }

    [Fact]
    public void LevelLoader_ParsesInventory()
    {
        const string json = """
        { "schema_version": 1, "id": "lvl_x", "grid": { "width": 3, "height": 1 }, "max_ticks": 5,
          "inventory": { "mode": "whitelist", "allowed": ["node_belt"], "limits": { "node_belt": 4 } },
          "fixed_nodes": [] }
        """;

        LevelDefinition level = LevelLoader.Parse(json, "test");

        Assert.NotNull(level.Inventory);
        Assert.True(level.Inventory!.Allows("node_belt"));
        Assert.False(level.Inventory.Allows("node_portal"));
        Assert.Equal(4, level.Inventory.LimitFor("node_belt"));
    }
}
