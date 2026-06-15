namespace EchoFactory.Core;

/// <summary>Alternates incoming items between two outputs (stateful L/R toggle).</summary>
public sealed class SplitterNode : INode
{
    private readonly SplitterConfig _config;
    private bool _useA;

    public SplitterNode(NodeId id, GridPoint position, SplitterConfig config)
    {
        Id = id;
        Position = position;
        _config = config;
        _useA = config.StartWithA;
    }

    public NodeId Id { get; }

    public GridPoint Position { get; }

    public void Evaluate(in SimContext ctx, GridState current, GridStateBuilder builder)
    {
        if (current.TryGetItem(Position, out var item))
        {
            Direction dir = _useA ? _config.OutputA : _config.OutputB;
            builder.Move(Position, Position + dir.Offset(), item, Id);
            _useA = !_useA;
        }
    }
}
