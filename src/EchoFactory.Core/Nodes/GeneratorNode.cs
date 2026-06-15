namespace EchoFactory.Core;

/// <summary>Spawns scheduled items onto the cell in its output direction.</summary>
public sealed class GeneratorNode : INode
{
    private readonly IReadOnlyList<SpawnEntry> _schedule;

    public GeneratorNode(NodeId id, GridPoint position, Direction output, IReadOnlyList<SpawnEntry> schedule)
    {
        Id = id;
        Position = position;
        Output = output;
        _schedule = schedule;
    }

    public NodeId Id { get; }

    public GridPoint Position { get; }

    public Direction Output { get; }

    public void Evaluate(in SimContext ctx, GridState current, GridStateBuilder builder)
    {
        GridPoint target = Position + Output.Offset();
        int sequence = 0;

        foreach (var entry in _schedule)
        {
            if (entry.Tick == ctx.Tick)
            {
                var id = ItemId.FromSpawn(Id, ctx.Tick, sequence);
                builder.Spawn(target, new Item(id, entry.Value), Id);
                sequence++;
            }
        }
    }
}
