using EchoFactory.Content;
using Xunit;

namespace EchoFactory.Content.Tests;

public class LeaderboardTests
{
    [Fact]
    public void Submit_RecordsAndImprovesEachAxisIndependently()
    {
        var board = new Leaderboard();

        var first = board.Submit("lvl", ticks: 10, footprint: 5, stars: 2);
        Assert.True(first.NewTickRecord);
        Assert.True(first.NewFootprintRecord);
        Assert.Equal(10, board.Get("lvl")!.BestTicks);
        Assert.Equal(5, board.Get("lvl")!.BestFootprint);

        // Faster but bigger: improves ticks only.
        var second = board.Submit("lvl", ticks: 8, footprint: 7, stars: 1);
        Assert.True(second.NewTickRecord);
        Assert.False(second.NewFootprintRecord);
        Assert.Equal(8, board.Get("lvl")!.BestTicks);
        Assert.Equal(5, board.Get("lvl")!.BestFootprint);

        // Worse on both: no change.
        var third = board.Submit("lvl", ticks: 20, footprint: 9, stars: 3);
        Assert.False(third.NewTickRecord);
        Assert.False(third.NewFootprintRecord);
        Assert.Equal(8, board.Get("lvl")!.BestTicks);
        Assert.Equal(3, board.Get("lvl")!.BestStars); // stars keep the max
    }

    [Fact]
    public void RoundTrips_ThroughJson()
    {
        var board = new Leaderboard();
        board.Submit("a", 5, 3, 3);
        board.Submit("b", 12, 8, 1);

        Leaderboard back = LeaderboardStore.Deserialize(LeaderboardStore.Serialize(board));

        Assert.Equal(5, back.Get("a")!.BestTicks);
        Assert.Equal(8, back.Get("b")!.BestFootprint);
        Assert.Null(back.Get("missing"));
    }
}
