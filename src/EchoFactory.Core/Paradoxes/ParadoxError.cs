namespace EchoFactory.Core;

public enum ParadoxKind
{
    Collision,
    Void,
    Math,
    Temporal,
}

/// <summary>
/// A simulation paradox. This is a normal *outcome*, not an engine crash — the frontend
/// surfaces it as a hint about where/why the player's design broke
/// (simulation-engine.md §5).
/// </summary>
public sealed class ParadoxError
{
    private ParadoxError(ParadoxKind kind, int tick, GridPoint cell, IReadOnlyList<ItemId> items, string message)
    {
        Kind = kind;
        Tick = tick;
        Cell = cell;
        Items = items;
        Message = message;
    }

    public ParadoxKind Kind { get; }

    public int Tick { get; }

    public GridPoint Cell { get; }

    public IReadOnlyList<ItemId> Items { get; }

    public string Message { get; }

    public static ParadoxError Collision(int tick, GridPoint cell, IReadOnlyList<Item> items)
    {
        var ids = new ItemId[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            ids[i] = items[i].Id;
        }

        return new ParadoxError(
            ParadoxKind.Collision,
            tick,
            cell,
            ids,
            FormattableString.Invariant($"Collision at {cell} on tick {tick}: {items.Count} items met"));
    }

    public static ParadoxError Void(int tick, GridPoint cell, ItemId item) => new(
        ParadoxKind.Void,
        tick,
        cell,
        [item],
        FormattableString.Invariant($"Void at {cell} on tick {tick}: item left the valid system"));

    public override string ToString() => Message;
}
