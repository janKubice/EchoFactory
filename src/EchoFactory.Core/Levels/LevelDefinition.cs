namespace EchoFactory.Core;

/// <summary>One scheduled spawn from a generator.</summary>
public readonly record struct SpawnEntry(int Tick, int Value);

/// <summary>A fixed generator placed by the level.</summary>
public sealed class GeneratorSpec
{
    public required string Id { get; init; }

    public required GridPoint Position { get; init; }

    /// <summary>Direction the generator emits into (item appears on the adjacent cell).</summary>
    public required Direction Output { get; init; }

    public required IReadOnlyList<SpawnEntry> Schedule { get; init; }
}

/// <summary>A fixed sink (target) placed by the level.</summary>
public sealed class SinkSpec
{
    public required string Id { get; init; }

    public required GridPoint Position { get; init; }

    /// <summary>Required values, in arrival order. (Strict per-tick timing: future milestone.)</summary>
    public required IReadOnlyList<int> Expected { get; init; }
}

/// <summary>
/// Engine input contract describing a level. How this POCO is produced (in code now,
/// from JSON in M2) is separable from the engine.
/// </summary>
public sealed class LevelDefinition
{
    public required string Id { get; init; }

    public required GridSize Grid { get; init; }

    public required int MaxTicks { get; init; }

    /// <summary>If true, sinks will require exact arrival ticks (reserved for a later milestone).</summary>
    public bool StrictTiming { get; init; }

    public IReadOnlyList<GeneratorSpec> Generators { get; init; } = [];

    public IReadOnlyList<SinkSpec> Sinks { get; init; } = [];
}
