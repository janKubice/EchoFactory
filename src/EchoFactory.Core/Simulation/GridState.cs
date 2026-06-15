namespace EchoFactory.Core;

/// <summary>Kind of a visual-only event emitted during a tick (consumed by the frontend).</summary>
public enum VisualEventKind
{
    Spawn,
    Move,
    Consume,
    Math,
    Paradox,
}

/// <summary>
/// A visual-only event. The engine emits these for the renderer (tweening, effects);
/// the simulation itself never depends on them.
/// </summary>
public readonly record struct VisualEvent(VisualEventKind Kind, GridPoint From, GridPoint To, Item Item);

/// <summary>
/// Immutable snapshot of the grid at a single tick. In M1 at most one item occupies a
/// cell (merge-capable nodes generalize this later).
/// </summary>
public sealed class GridState
{
    private readonly Dictionary<GridPoint, Item> _items;
    private readonly IReadOnlyList<VisualEvent> _events;

    public GridState(int tick, Dictionary<GridPoint, Item> items, IReadOnlyList<VisualEvent> events)
    {
        Tick = tick;
        _items = items;
        _events = events;
    }

    public int Tick { get; }

    public IReadOnlyDictionary<GridPoint, Item> Items => _items;

    public int ItemCount => _items.Count;

    /// <summary>Visual events that occurred on the transition into this tick.</summary>
    public IReadOnlyList<VisualEvent> Events => _events;

    public bool TryGetItem(GridPoint p, out Item item) => _items.TryGetValue(p, out item);

    public static GridState Empty(int tick) => new(tick, new Dictionary<GridPoint, Item>(), []);
}
