namespace EchoFactory.Core;

/// <summary>
/// A time portal. An item on its cell is absorbed (removed from the forward flow) and an
/// emission is recorded for apply-tick <c>T - TimeOffset</c> at the exit cell. Emissions feed
/// the next compiler pass; the multi-pass fixed-point loop resolves the causality
/// (simulation-engine.md §3).
/// </summary>
public sealed class PortalNode : INode
{
    private readonly PortalConfig _config;

    public PortalNode(NodeId id, GridPoint position, PortalConfig config)
    {
        Id = id;
        Position = position;
        _config = config;
    }

    public NodeId Id { get; }

    public GridPoint Position { get; }

    public void Evaluate(in SimContext ctx, GridState current, GridStateBuilder builder)
    {
        if (current.TryGetItem(Position, out var item))
        {
            int applyTick = ctx.Tick - _config.TimeOffset;
            GridPoint exit = Position + _config.Output.Offset();
            var emitted = new Item(ItemId.FromPortal(Id, applyTick, item.Value), item.Value);
            builder.PortalAbsorb(Position, exit, applyTick, emitted);
        }
    }
}
