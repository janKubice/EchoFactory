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

/// <summary>Configuration for a math node. All M2 operations are binary (arity 2).</summary>
public sealed class MathConfig
{
    public required MathOperation Operation { get; init; }

    /// <summary>Direction the computed result is emitted into.</summary>
    public required Direction Output { get; init; }

    /// <summary>Number of operands consumed per result. Binary for the M2 operation set.</summary>
    public int Arity => 2;
}

/// <summary>Configuration for a splitter: alternates incoming items between two outputs.</summary>
public sealed class SplitterConfig
{
    public required Direction OutputA { get; init; }

    public required Direction OutputB { get; init; }

    /// <summary>Which output the first item takes.</summary>
    public bool StartWithA { get; init; } = true;
}
