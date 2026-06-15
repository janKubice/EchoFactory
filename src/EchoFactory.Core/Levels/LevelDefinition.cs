namespace EchoFactory.Core;

/// <summary>Reference targets for the 3-star rating / leaderboard expectations.</summary>
public sealed class LevelPar
{
    public required int Ticks { get; init; }

    public required int Footprint { get; init; }
}

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

    /// <summary>Cap on temporal fixed-point iterations before declaring a TemporalParadox.</summary>
    public int MaxTemporalPasses { get; init; } = 5;

    /// <summary>If true, sinks will require exact arrival ticks (reserved for a later milestone).</summary>
    public bool StrictTiming { get; init; }

    public IReadOnlyList<GeneratorSpec> Generators { get; init; } = [];

    public IReadOnlyList<SinkSpec> Sinks { get; init; } = [];

    /// <summary>Optional par targets for star rating.</summary>
    public LevelPar? Par { get; init; }
}
