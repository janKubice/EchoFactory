namespace EchoFactory.Content;

// Internal JSON DTOs. Mutable for System.Text.Json; resolved into immutable Core/Content types.

internal sealed class NodeDefinitionDto
{
    public int SchemaVersion { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
    public LogicDto? Logic { get; set; }
    public VisualDto? Visual { get; set; }
    public int TickCost { get; set; } = 1;
}

internal sealed class LogicDto
{
    public string? Operation { get; set; }
}

internal sealed class VisualDto
{
    public string? Shape { get; set; }
    public string? ColorHex { get; set; }
    public string? Icon { get; set; }
}

internal sealed class LevelDto
{
    public int SchemaVersion { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public GridDto? Grid { get; set; }
    public int MaxTicks { get; set; }
    public int MaxTemporalPasses { get; set; } = 5;
    public bool StrictTiming { get; set; }
    public List<FixedNodeDto>? FixedNodes { get; set; }
}

internal sealed class GridDto
{
    public int Width { get; set; }
    public int Height { get; set; }
}

internal sealed class PointDto
{
    public int X { get; set; }
    public int Y { get; set; }
}

internal sealed class SpawnDto
{
    public int Tick { get; set; }
    public int Value { get; set; }
}

internal sealed class FixedNodeDto
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public PointDto? Position { get; set; }
    public string? Direction { get; set; }
    public List<SpawnDto>? Schedule { get; set; }
    public List<int>? Expected { get; set; }
}

internal sealed class SolutionDto
{
    public int SchemaVersion { get; set; }
    public string? LevelId { get; set; }
    public string? LevelHash { get; set; }
    public List<PlacedNodeDto>? PlacedNodes { get; set; }
}

internal sealed class PlacedNodeDto
{
    public string? Node { get; set; }
    public PointDto? Position { get; set; }
    public string? Direction { get; set; }
    public string? OutputA { get; set; }
    public string? OutputB { get; set; }
    public bool StartWithA { get; set; } = true;
    public int? TimeOffset { get; set; }
}
