namespace EchoFactory.Core;

/// <summary>Whether the inventory lists what is allowed or what is forbidden.</summary>
public enum InventoryMode
{
    Whitelist,
    Blacklist,
}

/// <summary>
/// Level constraint on which node types a player may place and how many. Pure level metadata —
/// it does not affect simulation; the editor and content validation enforce it. Keyed by node
/// definition id (e.g. "node_math_add"). A null inventory on a level means "anything, unlimited".
/// </summary>
public sealed class LevelInventory
{
    public InventoryMode Mode { get; init; } = InventoryMode.Whitelist;

    /// <summary>Allowed ids (whitelist) or forbidden ids (blacklist).</summary>
    public IReadOnlySet<string> Listed { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Per-node-id maximum counts. Missing id = unlimited.</summary>
    public IReadOnlyDictionary<string, int> Limits { get; init; } = new Dictionary<string, int>(StringComparer.Ordinal);

    public bool Allows(string nodeId) =>
        Mode == InventoryMode.Whitelist ? Listed.Contains(nodeId) : !Listed.Contains(nodeId);

    public int LimitFor(string nodeId) => Limits.TryGetValue(nodeId, out int n) ? n : int.MaxValue;
}
