namespace EchoFactory.Core;

/// <summary>Consumes whatever item lands on it; the compiler validates the received sequence.</summary>
public sealed class SinkNode : INode
{
    public SinkNode(NodeId id, GridPoint position, IReadOnlyList<int> expected)
    {
        Id = id;
        Position = position;
        Expected = expected;
    }

    public NodeId Id { get; }

    public GridPoint Position { get; }

    public IReadOnlyList<int> Expected { get; }

    public void Evaluate(in SimContext ctx, GridState current, GridStateBuilder builder)
    {
        if (current.TryGetItem(Position, out var item))
        {
            builder.Consume(Position, item, Id);
        }
    }
}
