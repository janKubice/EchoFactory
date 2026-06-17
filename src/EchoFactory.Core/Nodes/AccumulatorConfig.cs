namespace EchoFactory.Core;

/// <summary>
/// Configuration for an accumulator (running-sum register). It folds every arriving item
/// into an internal sum (starting at <see cref="Initial"/>); when the sum satisfies the
/// release condition <c>sum &lt;op&gt; Constant</c> it emits an item carrying the total to
/// <see cref="Output"/> and resets to <see cref="Initial"/>. This lets a loop "sum N values
/// and emit the result once the condition holds".
/// </summary>
public sealed class AccumulatorConfig
{
    /// <summary>Comparison used to decide when the running sum is released.</summary>
    public required Comparison ReleaseWhen { get; init; }

    /// <summary>Right-hand side of the release comparison.</summary>
    public required int Constant { get; init; }

    /// <summary>Direction the released total is emitted into.</summary>
    public required Direction Output { get; init; }

    /// <summary>Starting value of the running sum (and the value it resets to after a release).</summary>
    public int Initial { get; init; }

    public bool Releases(long sum) => ReleaseWhen switch
    {
        Comparison.Eq => sum == Constant,
        Comparison.Ne => sum != Constant,
        Comparison.Lt => sum < Constant,
        Comparison.Le => sum <= Constant,
        Comparison.Gt => sum > Constant,
        Comparison.Ge => sum >= Constant,
        _ => false,
    };
}
