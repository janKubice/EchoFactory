namespace EchoFactory.Core;

/// <summary>Moves the item on its cell one step in <see cref="Direction"/>.</summary>
public sealed class BeltNode : INode
{
    public BeltNode(NodeId id, GridPoint position, Direction direction)
    {
        Id = id;
        Position = position;
        Direction = direction;
    }

    public NodeId Id { get; }

    public GridPoint Position { get; }

    public Direction Direction { get; }

    public void Evaluate(in SimContext ctx, GridState current, GridStateBuilder builder)
    {
        if (current.TryGetItem(Position, out var item))
        {
            builder.Move(Position, Position + Direction.Offset(), item, Id);
        }
    }
}
