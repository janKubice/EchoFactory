namespace EchoFactory.Core;

/// <summary>
/// Universal math node driven by a <see cref="MathConfig"/>. Items delivered onto its cell
/// are absorbed into an internal buffer (one per tick); once enough operands have arrived it
/// emits the result onto the output cell. Operand order follows arrival order, so non-commutative
/// operations (sub, div, mod) are well defined. Division by zero / overflow -> MathParadox.
/// </summary>
public sealed class GenericMathNode : INode
{
    private readonly MathConfig _config;
    private readonly List<int> _buffer = [];
    private int _emitSequence;

    public GenericMathNode(NodeId id, GridPoint position, MathConfig config)
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
            builder.Absorb(Position, item);
            _buffer.Add(item.Value);
        }

        if (_buffer.Count < _config.Arity)
        {
            return;
        }

        int a = _buffer[0];
        int b = _config.Arity == 1 ? _config.Constant!.Value : _buffer[1];

        if (_config.Operation is MathOperation.Div or MathOperation.Mod && b == 0)
        {
            builder.Report(ParadoxError.Math(ctx.Tick, Position, "division by zero"));
            return;
        }

        long r = _config.Operation switch
        {
            MathOperation.Add => (long)a + b,
            MathOperation.Sub => (long)a - b,
            MathOperation.Mul => (long)a * b,
            MathOperation.Div => a / b,
            MathOperation.Mod => a % b,
            MathOperation.Min => Math.Min(a, b),
            MathOperation.Max => Math.Max(a, b),
            _ => throw new ArgumentOutOfRangeException(nameof(ctx)),
        };

        if (r is < int.MinValue or > int.MaxValue)
        {
            builder.Report(ParadoxError.Math(ctx.Tick, Position, "integer overflow"));
            return;
        }

        var result = new Item(ItemId.FromSpawn(Id, ctx.Tick, _emitSequence), (int)r);
        _emitSequence++;
        builder.Spawn(Position + _config.Output.Offset(), result, Id);
        _buffer.RemoveRange(0, _config.Arity);
    }
}
