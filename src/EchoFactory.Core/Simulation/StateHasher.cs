namespace EchoFactory.Core;

/// <summary>
/// Deterministic 64-bit hash of a full timeline. Cells are visited in row-major order so
/// the hash never depends on dictionary iteration order. Used by determinism tests and,
/// later, leaderboard verification (meta-services.md).
/// </summary>
public static class StateHasher
{
    private const ulong Separator = 0xFFFF_FFFF_FFFF_FFFFUL;

    public static ulong Hash(IReadOnlyList<GridState> states)
    {
        ulong h = DeterministicHash.FnvOffset;

        foreach (var state in states)
        {
            h = DeterministicHash.Combine(h, state.Tick);

            foreach (var kv in state.Items.OrderBy(static kv => kv.Key))
            {
                h = DeterministicHash.Combine(h, kv.Key.X);
                h = DeterministicHash.Combine(h, kv.Key.Y);
                h = DeterministicHash.Combine(h, kv.Value.Id.Raw);
                h = DeterministicHash.Combine(h, kv.Value.Value);
            }

            h = DeterministicHash.Combine(h, Separator);
        }

        return h;
    }
}
