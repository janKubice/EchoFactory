namespace EchoFactory.Core;

/// <summary>
/// A node placed by the player. Carries the kind, position and the per-placement
/// configuration relevant to that kind.
/// </summary>
public sealed class PlacedNode
{
    public required NodeKind Kind { get; init; }

    public required GridPoint Position { get; init; }

    /// <summary>Belt direction (Kind == Belt). Unused by other kinds.</summary>
    public Direction Direction { get; init; }

    /// <summary>Set when Kind == Math.</summary>
    public MathConfig? Math { get; init; }

    /// <summary>Set when Kind == Splitter.</summary>
    public SplitterConfig? Splitter { get; init; }

    public static PlacedNode Belt(GridPoint position, Direction direction) =>
        new() { Kind = NodeKind.Belt, Position = position, Direction = direction };

    public static PlacedNode MathOp(GridPoint position, MathConfig config) =>
        new() { Kind = NodeKind.Math, Position = position, Math = config };

    public static PlacedNode Split(GridPoint position, SplitterConfig config) =>
        new() { Kind = NodeKind.Splitter, Position = position, Splitter = config };
}

/// <summary>
/// The player's solution: the nodes they placed on the grid. Fixed nodes (generators,
/// sinks) come from the <see cref="LevelDefinition"/>.
/// </summary>
public sealed class Build
{
    public IReadOnlyList<PlacedNode> Nodes { get; init; } = [];

    /// <summary>Footprint metric: number of player-placed nodes (leaderboard).</summary>
    public int Footprint => Nodes.Count;
}
