using EchoFactory.Core;

namespace EchoFactory.Content;

/// <summary>Verdict of verifying a leaderboard submission.</summary>
public sealed record VerifyResult(bool Accepted, string Reason, int Ticks, int Footprint, int Stars);

/// <summary>
/// Server-side anti-cheat: re-simulates a submitted solution and confirms it respects the level's
/// inventory, actually solves the level, and matches the claimed metrics (meta-services.md §2).
/// Because the engine is deterministic this verdict is reproducible on any machine.
/// </summary>
public static class SubmissionVerifier
{
    public static VerifyResult Verify(LevelDefinition level, SolutionInfo solution, int? claimedTicks, int? claimedFootprint)
    {
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(solution);

        string? inventoryViolation = InventoryCheck.Violation(level.Inventory, solution.NodeIds);
        if (inventoryViolation is not null)
        {
            return new VerifyResult(false, "inventory: " + inventoryViolation, 0, 0, 0);
        }

        SimulationResult result = SimulationCompiler.Compile(level, solution.Build);
        if (result.Outcome != LevelOutcome.Solved)
        {
            string detail = result.Error is { } e ? $" ({e.Message})" : string.Empty;
            return new VerifyResult(false, $"does not solve the level - {result.Outcome}{detail}", 0, 0, 0);
        }

        int ticks = result.Stats.FinalTick;
        int footprint = result.Stats.Footprint;
        int stars = StarRating.Compute(result, level.Par);

        if (claimedTicks is int ct && ct != ticks)
        {
            return new VerifyResult(false, $"claimed {ct} ticks but the solution runs in {ticks}", ticks, footprint, stars);
        }

        if (claimedFootprint is int cf && cf != footprint)
        {
            return new VerifyResult(false, $"claimed {cf} nodes but the solution uses {footprint}", ticks, footprint, stars);
        }

        return new VerifyResult(true, "accepted", ticks, footprint, stars);
    }
}
