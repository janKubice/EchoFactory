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

    /// <summary>Required values, in arrival order.</summary>
    public required IReadOnlyList<int> Expected { get; init; }

    /// <summary>Required arrival ticks, parallel to <see cref="Expected"/>. Used only when the level
    /// has <c>StrictTiming</c>; null means "any tick".</summary>
    public IReadOnlyList<int>? ExpectedTicks { get; init; }
}

/// <summary>
/// Engine input contract describing a level. How this POCO is produced (in code now,
/// from JSON in M2) is separable from the engine.
/// </summary>
public sealed class LevelDefinition
{
    public required string Id { get; init; }

    /// <summary>Display name (defaults to the id).</summary>
    public string Name { get; init; } = "";

    /// <summary>Short objective / hint shown to the player.</summary>
    public string Description { get; init; } = "";

    /// <summary>Campaign ordering (lower = earlier). Unordered levels sort last.</summary>
    public int Order { get; init; } = 1000;

    /// <summary>Campaign this level belongs to (groups the level select). Blank = uncategorised.</summary>
    public string Campaign { get; init; } = "";

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

    /// <summary>Optional inventory constraint (which nodes may be placed, and how many). Null = unlimited.</summary>
    public LevelInventory? Inventory { get; init; }
}
