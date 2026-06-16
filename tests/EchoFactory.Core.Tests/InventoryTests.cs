using EchoFactory.Core;
using Xunit;

namespace EchoFactory.Core.Tests;

public class InventoryTests
{
    [Fact]
    public void Whitelist_AllowsOnlyListed()
    {
        var inv = new LevelInventory { Mode = InventoryMode.Whitelist, Listed = new HashSet<string> { "node_belt" } };

        Assert.True(inv.Allows("node_belt"));
        Assert.False(inv.Allows("node_portal"));
    }

    [Fact]
    public void Blacklist_BlocksOnlyListed()
    {
        var inv = new LevelInventory { Mode = InventoryMode.Blacklist, Listed = new HashSet<string> { "node_portal" } };

        Assert.False(inv.Allows("node_portal"));
        Assert.True(inv.Allows("node_belt"));
    }

    [Fact]
    public void LimitFor_DefaultsToUnlimited()
    {
        var inv = new LevelInventory { Limits = new Dictionary<string, int> { ["node_belt"] = 3 } };

        Assert.Equal(3, inv.LimitFor("node_belt"));
        Assert.Equal(int.MaxValue, inv.LimitFor("node_portal"));
    }
}
