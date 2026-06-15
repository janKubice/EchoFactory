namespace EchoFactory.Core;

/// <summary>A node's proposal to place an item on a target cell at the next tick.</summary>
internal readonly record struct Proposal(GridPoint Target, Item Item, NodeId Source, int Priority);

/// <summary>Record of an item consumed by a sink during a tick.</summary>
public readonly record struct Consumed(NodeId Sink, Item Item, int Tick);

/// <summary>A time-shifted emission recorded by a portal: which item appears where, and when.</summary>
public readonly record struct PortalEmission(GridPoint ExitCell, int ApplyTick, Item Item);

/// <summary>
/// Accumulates node proposals during the PROPOSE phase, then deterministically resolves
/// them into the next <see cref="GridState"/> during COMMIT (simulation-engine.md §2).
/// All non-determinism is isolated here, behind a single sorted resolution.
/// </summary>
public sealed class GridStateBuilder
{
    private readonly GridState _current;
    private readonly GridSize _grid;
    private readonly int _nextTick;
    private readonly List<Proposal> _proposals = [];
    private readonly HashSet<GridPoint> _serviced = [];
    private readonly List<Consumed> _consumed = [];
    private readonly List<PortalEmission> _emissions = [];
    private readonly List<VisualEvent> _events = [];
    private readonly List<ParadoxError> _reported = [];

    public GridStateBuilder(GridState current, GridSize grid)
    {
        _current = current;
        _grid = grid;
        _nextTick = current.Tick + 1;
    }

    /// <summary>Items consumed by sinks this tick.</summary>
    public IReadOnlyList<Consumed> Consumed => _consumed;

    /// <summary>Portal emissions recorded this tick (fed into the next compiler pass).</summary>
    public IReadOnlyList<PortalEmission> Emissions => _emissions;

    /// <summary>Move an existing item one step. Services the source cell.</summary>
    public void Move(GridPoint from, GridPoint to, Item item, NodeId source, int priority = 0)
    {
        _serviced.Add(from);
        _proposals.Add(new Proposal(to, item, source, priority));
        _events.Add(new VisualEvent(VisualEventKind.Move, from, to, item));
    }

    /// <summary>Spawn a brand-new item onto a cell (does not service any current cell).</summary>
    public void Spawn(GridPoint at, Item item, NodeId source)
    {
        _proposals.Add(new Proposal(at, item, source, Priority: 0));
        _events.Add(new VisualEvent(VisualEventKind.Spawn, at, at, item));
    }

    /// <summary>Consume an item off a cell (sink). Services the cell.</summary>
    public void Consume(GridPoint cell, Item item, NodeId sink)
    {
        _serviced.Add(cell);
        _consumed.Add(new Consumed(sink, item, _current.Tick));
        _events.Add(new VisualEvent(VisualEventKind.Consume, cell, cell, item));
    }

    /// <summary>Absorb an item into a node's internal buffer (math). Services the cell.</summary>
    public void Absorb(GridPoint cell, Item item)
    {
        _serviced.Add(cell);
        _events.Add(new VisualEvent(VisualEventKind.Math, cell, cell, item));
    }

    /// <summary>Report a paradox detected during the PROPOSE phase (e.g. math errors).</summary>
    public void Report(ParadoxError paradox) => _reported.Add(paradox);

    /// <summary>Absorb an item into a time portal and record its time-shifted emission. Services the cell.</summary>
    public void PortalAbsorb(GridPoint portalCell, GridPoint exitCell, int applyTick, Item emitted)
    {
        _serviced.Add(portalCell);
        _emissions.Add(new PortalEmission(exitCell, applyTick, emitted));
        _events.Add(new VisualEvent(VisualEventKind.PortalIn, portalCell, exitCell, emitted));
    }

    /// <summary>Inject a time-shifted item onto a cell (portal output from a previous pass).</summary>
    public void Inject(GridPoint cell, Item item)
    {
        _proposals.Add(new Proposal(cell, item, default, Priority: 0));
        _events.Add(new VisualEvent(VisualEventKind.PortalOut, cell, cell, item));
    }

    /// <summary>
    /// Resolve proposals into the next state, or report the first paradox. Resolution order
    /// is fully deterministic so the reported paradox is stable across runs and platforms.
    /// </summary>
    public bool TryCommit(out GridState next, out ParadoxError? paradox)
    {
        next = null!;
        paradox = null;

        // 0) Paradoxes reported during PROPOSE (e.g. math errors). Deterministic pick.
        if (_reported.Count > 0)
        {
            paradox = _reported
                .OrderBy(static p => p.Cell)
                .ThenBy(static p => (int)p.Kind)
                .First();
            return false;
        }

        // 1) VOID (unserviced): any current item whose cell no node handled is lost.
        foreach (var kv in _current.Items.OrderBy(static kv => kv.Key))
        {
            if (!_serviced.Contains(kv.Key))
            {
                paradox = ParadoxError.Void(_current.Tick, kv.Key, kv.Value.Id);
                return false;
            }
        }

        // 2) Resolve proposals grouped by target, in deterministic order.
        var result = new Dictionary<GridPoint, Item>();
        var ordered = _proposals
            .OrderBy(static p => p.Target)
            .ThenByDescending(static p => p.Priority)
            .ThenBy(static p => p.Item.Id)
            .GroupBy(static p => p.Target);

        foreach (var group in ordered)
        {
            GridPoint target = group.Key;

            // VOID (off-grid): item moved out of the valid grid.
            if (!_grid.Contains(target))
            {
                paradox = ParadoxError.Void(_nextTick, target, group.First().Item.Id);
                return false;
            }

            var items = group.ToList();
            if (items.Count == 1)
            {
                result[target] = items[0].Item;
            }
            else
            {
                // M1: no merge node exists yet -> any co-location is a collision.
                paradox = ParadoxError.Collision(_nextTick, target, items.ConvertAll(static p => p.Item));
                return false;
            }
        }

        next = new GridState(_nextTick, result, _events);
        return true;
    }
}
