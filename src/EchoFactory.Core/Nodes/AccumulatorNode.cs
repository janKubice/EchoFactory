namespace EchoFactory.Core;

/// <summary>
/// Stateful running-sum register. Each tick it absorbs an item arriving on its cell, adding
/// the value to an internal sum; the moment the sum satisfies the release condition it emits
/// the total and resets. The sum is kept as a <see cref="long"/> and overflow on release is a
/// MathParadox. Per-pass state is clean because the compiler builds fresh nodes each pass.
/// </summary>
public sealed class AccumulatorNode : INode
{
    private readonly AccumulatorConfig _config;
    private long _sum;
    private int _emitSequence;

    public AccumulatorNode(NodeId id, GridPoint position, AccumulatorConfig config)
    {
        Id = id;
        Position = position;
        _config = config;
        _sum = config.Initial;
    }

    public NodeId Id { get; }

    public GridPoint Position { get; }

    public void Evaluate(in SimContext ctx, GridState current, GridStateBuilder builder)
    {
        // Release is only evaluated on ticks where an item is folded in, so a condition that
        // is already true at the start (e.g. release-when >= 0) does not emit forever.
        if (!current.TryGetItem(Position, out var item))
        {
            return;
        }

        builder.Absorb(Position, item);
        _sum += item.Value;

        if (!_config.Releases(_sum))
        {
            return;
        }

        if (_sum is < int.MinValue or > int.MaxValue)
        {
            builder.Report(ParadoxError.Math(ctx.Tick, Position, "accumulator overflow"));
            return;
        }

        var result = new Item(ItemId.FromSpawn(Id, ctx.Tick, _emitSequence), (int)_sum);
        _emitSequence++;
        builder.Spawn(Position + _config.Output.Offset(), result, Id);
        _sum = _config.Initial;
    }
}
