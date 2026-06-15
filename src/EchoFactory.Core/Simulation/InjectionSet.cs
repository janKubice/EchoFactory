namespace EchoFactory.Core;

/// <summary>
/// A single scheduled portal injection in a candidate timeline: a value re-appearing at
/// <see cref="ExitCell"/> on <see cref="ApplyTick"/>. Value-based identity is what the fixed
/// point compares (the <see cref="Id"/> is deterministically derived from the same data).
/// </summary>
internal readonly record struct Injection(GridPoint ExitCell, int ApplyTick, int Value, ItemId Id)
    : IComparable<Injection>
{
    public int CompareTo(Injection other)
    {
        int c = ExitCell.CompareTo(other.ExitCell);
        if (c != 0)
        {
            return c;
        }

        c = ApplyTick.CompareTo(other.ApplyTick);
        if (c != 0)
        {
            return c;
        }

        c = Value.CompareTo(other.Value);
        return c != 0 ? c : Id.CompareTo(other.Id);
    }
}

/// <summary>
/// The full set of portal injections for one candidate timeline, normalized (sorted) so that
/// equality and hashing are deterministic — the basis of fixed-point and oscillation detection.
/// </summary>
internal sealed class InjectionSet
{
    public static readonly InjectionSet Empty = new([]);

    private readonly Injection[] _sorted;

    public InjectionSet(IEnumerable<Injection> items)
    {
        _sorted = items.ToArray();
        Array.Sort(_sorted);
    }

    public int Count => _sorted.Length;

    public IReadOnlyList<Injection> Items => _sorted;

    public bool SetEquals(InjectionSet other)
    {
        if (_sorted.Length != other._sorted.Length)
        {
            return false;
        }

        for (int i = 0; i < _sorted.Length; i++)
        {
            if (!_sorted[i].Equals(other._sorted[i]))
            {
                return false;
            }
        }

        return true;
    }

    public ulong Hash()
    {
        ulong h = DeterministicHash.FnvOffset;
        foreach (var inj in _sorted)
        {
            h = DeterministicHash.Combine(h, inj.ExitCell.X);
            h = DeterministicHash.Combine(h, inj.ExitCell.Y);
            h = DeterministicHash.Combine(h, inj.ApplyTick);
            h = DeterministicHash.Combine(h, inj.Value);
            h = DeterministicHash.Combine(h, inj.Id.Raw);
        }

        return h;
    }
}
