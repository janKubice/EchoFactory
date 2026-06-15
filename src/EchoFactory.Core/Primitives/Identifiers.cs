namespace EchoFactory.Core;

/// <summary>
/// Deterministic 64-bit hashing (FNV-1a). Stable across processes and platforms —
/// unlike <see cref="HashCode"/>, which is randomized per process. Used for logical
/// identities (<see cref="ItemId"/>, <see cref="NodeId"/>) and state hashing.
/// </summary>
internal static class DeterministicHash
{
    public const ulong FnvOffset = 14695981039346656037UL;
    public const ulong FnvPrime = 1099511628211UL;

    public static ulong Combine(ulong hash, ulong value)
    {
        for (int i = 0; i < 8; i++)
        {
            hash ^= (byte)(value >> (i * 8));
            hash *= FnvPrime;
        }

        return hash;
    }

    public static ulong Combine(ulong hash, int value) => Combine(hash, (uint)value);

    public static ulong Of(ReadOnlySpan<char> text)
    {
        ulong h = FnvOffset;
        foreach (char c in text)
        {
            h ^= c;
            h *= FnvPrime;
        }

        return h;
    }
}

/// <summary>Deterministic, stable identity of a node (see ADR-0003).</summary>
public readonly struct NodeId : IEquatable<NodeId>, IComparable<NodeId>
{
    public readonly ulong Raw;

    public NodeId(ulong raw) => Raw = raw;

    /// <summary>Identity from a stable string id (e.g. "gen_a" in a level).</summary>
    public static NodeId FromString(string id) => new(DeterministicHash.Of(id));

    /// <summary>Identity from a player placement: deterministic from kind + position.</summary>
    public static NodeId FromPlacement(NodeKind kind, GridPoint pos)
    {
        ulong h = DeterministicHash.FnvOffset;
        h = DeterministicHash.Combine(h, (int)kind);
        h = DeterministicHash.Combine(h, pos.X);
        h = DeterministicHash.Combine(h, pos.Y);
        return new NodeId(h);
    }

    public bool Equals(NodeId other) => Raw == other.Raw;

    public override bool Equals(object? obj) => obj is NodeId n && Equals(n);

    public override int GetHashCode() => Raw.GetHashCode();

    public int CompareTo(NodeId other) => Raw.CompareTo(other.Raw);

    public static bool operator ==(NodeId a, NodeId b) => a.Equals(b);

    public static bool operator !=(NodeId a, NodeId b) => !a.Equals(b);

    public override string ToString() => string.Create(
        System.Globalization.CultureInfo.InvariantCulture, $"N:{Raw:x16}");
}

/// <summary>
/// Deterministic identity of an item, derived from its provenance
/// (source node, spawn tick, sequence). NEVER a random Guid (ADR-0003) —
/// stable identity is required to compare passes when resolving temporal loops.
/// </summary>
public readonly struct ItemId : IEquatable<ItemId>, IComparable<ItemId>
{
    public readonly ulong Raw;

    public ItemId(ulong raw) => Raw = raw;

    public static ItemId FromSpawn(NodeId source, int tick, int sequence)
    {
        ulong h = DeterministicHash.FnvOffset;
        h = DeterministicHash.Combine(h, source.Raw);
        h = DeterministicHash.Combine(h, tick);
        h = DeterministicHash.Combine(h, sequence);
        return new ItemId(h);
    }

    public bool Equals(ItemId other) => Raw == other.Raw;

    public override bool Equals(object? obj) => obj is ItemId i && Equals(i);

    public override int GetHashCode() => Raw.GetHashCode();

    public int CompareTo(ItemId other) => Raw.CompareTo(other.Raw);

    public static bool operator ==(ItemId a, ItemId b) => a.Equals(b);

    public static bool operator !=(ItemId a, ItemId b) => !a.Equals(b);

    public override string ToString() => string.Create(
        System.Globalization.CultureInfo.InvariantCulture, $"I:{Raw:x16}");
}
