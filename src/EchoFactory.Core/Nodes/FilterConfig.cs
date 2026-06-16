namespace EchoFactory.Core;

/// <summary>Comparison operators for a filter/gate node.</summary>
public enum Comparison
{
    Eq,
    Ne,
    Lt,
    Le,
    Gt,
    Ge,
}

/// <summary>
/// Configuration for a filter (gate): an item is passed to the output only if its value
/// satisfies <c>value &lt;op&gt; Constant</c>; otherwise it is dropped.
/// </summary>
public sealed class FilterConfig
{
    public required Comparison Comparison { get; init; }

    public required int Constant { get; init; }

    public required Direction Output { get; init; }

    public bool Passes(int value) => Comparison switch
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
