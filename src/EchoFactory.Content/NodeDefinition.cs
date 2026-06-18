using EchoFactory.Core;

namespace EchoFactory.Content;

/// <summary>Visual metadata for a node type (used by the frontend; carried by the registry).</summary>
public sealed class NodeVisual
{
    public string Shape { get; init; } = "square";

    public string ColorHex { get; init; } = "#FFFFFF";

    public string Icon { get; init; } = "";
}

/// <summary>
/// A resolved node *type* parsed from a <c>/data/nodes/*.json</c> definition. The engine
/// instantiates concrete nodes from these plus per-placement config.
/// </summary>
public sealed class NodeDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required NodeKind Kind { get; init; }

    public string Category { get; init; } = "";

    /// <summary>Plain-language explanation for the in-game node codex / help overlay.</summary>
    public string Description { get; init; } = "";

    /// <summary>Set when <see cref="Kind"/> is <see cref="NodeKind.Math"/>.</summary>
    public MathOperation? Operation { get; init; }

    public int TickCost { get; init; } = 1;

    public NodeVisual Visual { get; init; } = new();
}
