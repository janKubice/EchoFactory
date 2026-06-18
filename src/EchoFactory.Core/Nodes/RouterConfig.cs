namespace EchoFactory.Core;

/// <summary>
/// Configuration for a router (conditional splitter): an arriving item is sent to
/// <see cref="OutMatch"/> when <c>value &lt;op&gt; Constant</c> holds, otherwise to
/// <see cref="OutElse"/>. Unlike a filter it never drops items — it always routes them,
/// which is what enables process loops ("back into the loop vs. out to the goal").
/// </summary>
public sealed class RouterConfig
{
    public required Comparison Comparison { get; init; }

    public required int Constant { get; init; }

    /// <summary>Direction taken when the condition is satisfied.</summary>
    public required Direction OutMatch { get; init; }

    /// <summary>Direction taken when the condition is not satisfied.</summary>
    public required Direction OutElse { get; init; }

    public bool Matches(int value) => Comparison switch
    {
        Comparison.Eq => value == Constant,
        Comparison.Ne => value != Constant,
        Comparison.Lt => value < Constant,
        Comparison.Le => value <= Constant,
        Comparison.Gt => value > Constant,
        Comparison.Ge => value >= Constant,
        _ => false,
    };
}
