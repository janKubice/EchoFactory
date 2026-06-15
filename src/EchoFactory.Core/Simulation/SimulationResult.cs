namespace EchoFactory.Core;

public enum LevelOutcome
{
    Solved,
    Failed,
    Paradox,
}

/// <summary>Leaderboard-relevant metrics of a compiled run.</summary>
public readonly record struct SimulationStats(int FinalTick, int Footprint, int Passes);

/// <summary>The result of compiling a build against a level.</summary>
public sealed class SimulationResult
{
    private SimulationResult(
        LevelOutcome outcome,
        IReadOnlyList<GridState> states,
        ParadoxError? error,
        SimulationStats stats)
    {
        Outcome = outcome;
        States = states;
        Error = error;
        Stats = stats;
    }

    public LevelOutcome Outcome { get; }

    public bool IsSuccess => Outcome == LevelOutcome.Solved;

    /// <summary>Computed timeline (index = tick). On paradox, the prefix up to the failing tick.</summary>
    public IReadOnlyList<GridState> States { get; }

    public ParadoxError? Error { get; }

    public SimulationStats Stats { get; }

    public static SimulationResult Completed(LevelOutcome outcome, IReadOnlyList<GridState> states, SimulationStats stats) =>
        new(outcome, states, error: null, stats);

    public static SimulationResult Paradox(ParadoxError error, IReadOnlyList<GridState> states, SimulationStats stats) =>
        new(LevelOutcome.Paradox, states, error, stats);
}
