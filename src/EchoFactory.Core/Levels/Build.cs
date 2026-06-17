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

    /// <summary>Set when Kind == Portal.</summary>
    public PortalConfig? Portal { get; init; }

    /// <summary>Set when Kind == Filter.</summary>
    public FilterConfig? Filter { get; init; }

    /// <summary>Set when Kind == Router.</summary>
    public RouterConfig? Router { get; init; }

    /// <summary>Set when Kind == Accumulator.</summary>
    public AccumulatorConfig? Accumulator { get; init; }

    public static PlacedNode Belt(GridPoint position, Direction direction) =>
        new() { Kind = NodeKind.Belt, Position = position, Direction = direction };

    public static PlacedNode MathOp(GridPoint position, MathConfig config) =>
        new() { Kind = NodeKind.Math, Position = position, Math = config };

    public static PlacedNode Split(GridPoint position, SplitterConfig config) =>
        new() { Kind = NodeKind.Splitter, Position = position, Splitter = config };

    public static PlacedNode TimePortal(GridPoint position, PortalConfig config) =>
        new() { Kind = NodeKind.Portal, Position = position, Portal = config };

    public static PlacedNode Gate(GridPoint position, FilterConfig config) =>
        new() { Kind = NodeKind.Filter, Position = position, Filter = config };

    public static PlacedNode Route(GridPoint position, RouterConfig config) =>
        new() { Kind = NodeKind.Router, Position = position, Router = config };

    public static PlacedNode Accumulate(GridPoint position, AccumulatorConfig config) =>
        new() { Kind = NodeKind.Accumulator, Position = position, Accumulator = config };
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
