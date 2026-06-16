namespace EchoFactory.Core;

/// <summary>Passes an item to its output only if the value satisfies the condition; else drops it.</summary>
public sealed class FilterNode : INode
{
    private readonly FilterConfig _config;

    public FilterNode(NodeId id, GridPoint position, FilterConfig config)
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
            if (_config.Passes(item.Value))
            {
                builder.Move(Position, Position + _config.Output.Offset(), item, Id);
            }
            else
            {
                builder.Drop(Position, item);
            }
        }
    }
}
