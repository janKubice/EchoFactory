namespace EchoFactory.Core;

/// <summary>
/// Routes each arriving item to one of two outputs based on a condition (a conditional
/// splitter). Stateless: the same value always takes the same exit, so it is deterministic
/// and safe inside spatial loops. Generalizes <see cref="FilterNode"/> — instead of dropping
/// rejected items it sends them to a second output (e.g. back around the loop).
/// </summary>
public sealed class RouterNode : INode
{
    private readonly RouterConfig _config;

    public RouterNode(NodeId id, GridPoint position, RouterConfig config)
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
            Direction dir = _config.Matches(item.Value) ? _config.OutMatch : _config.OutElse;
            builder.Move(Position, Position + dir.Offset(), item, Id);
        }
    }
}
