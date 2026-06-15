namespace EchoFactory.Core;

/// <summary>
/// Computes a 0–3 star rating: 1 star for solving, +1 for meeting the footprint par,
/// +1 for meeting the tick par (meta-services.md — the two leaderboard axes).
/// </summary>
public static class StarRating
{
    public static int Compute(SimulationResult result, LevelPar? par)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Outcome != LevelOutcome.Solved)
        {
            return 0;
        }

        if (par is null)
        {
            return 1;
        }

        int stars = 1;
        if (result.Stats.Footprint <= par.Footprint)
        {
            stars++;
        }

        if (result.Stats.FinalTick <= par.Ticks)
        {
            stars++;
        }

        return stars;
    }
}
