using EchoFactory.Core;

namespace EchoFactory.Content;

/// <summary>Validates that a set of placed node ids respects a level's inventory.</summary>
public static class InventoryCheck
{
    /// <summary>Returns null if the build respects the inventory, otherwise a human-readable reason.</summary>
    public static string? Violation(LevelInventory? inventory, IReadOnlyList<string> nodeIds)
    {
        ArgumentNullException.ThrowIfNull(nodeIds);
        if (inventory is null)
        {
            return null;
        }

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string id in nodeIds)
        {
            if (!inventory.Allows(id))
            {
                return $"node '{id}' is not allowed on this level";
            }

            counts[id] = counts.GetValueOrDefault(id) + 1;
        }

        foreach (var (id, used) in counts)
        {
            int limit = inventory.LimitFor(id);
            if (used > limit)
            {
                return $"node '{id}' used {used}x but the limit is {limit}";
            }
        }

        return null;
    }
}
