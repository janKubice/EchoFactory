namespace EchoFactory.Core;

/// <summary>Operations supported by <see cref="GenericMathNode"/> (fixed set — ADR-0004).</summary>
public enum MathOperation
{
    Add,
    Sub,
    Mul,
    Div,
    Mod,
    Min,
    Max,
}

/// <summary>Configuration for a math node. Binary by default; unary when a constant operand is set.</summary>
public sealed class MathConfig
{
    public required MathOperation Operation { get; init; }

    /// <summary>Direction the computed result is emitted into.</summary>
    public required Direction Output { get; init; }

    /// <summary>If set, the node is unary: it computes <c>op(item, Constant)</c> on each arriving item.</summary>
    public int? Constant { get; init; }

    /// <summary>Number of operands consumed per result: 1 when a constant is configured, else 2.</summary>
    public int Arity => Constant.HasValue ? 1 : 2;
}

/// <summary>Configuration for a splitter: alternates incoming items between two outputs.</summary>
public sealed class SplitterConfig
{
    public required Direction OutputA { get; init; }

    public required Direction OutputB { get; init; }

    /// <summary>Which output the first item takes.</summary>
    public bool StartWithA { get; init; } = true;
}
