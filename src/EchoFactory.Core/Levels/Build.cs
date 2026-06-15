namespace EchoFactory.Core;

/// <summary>A belt placed by the player.</summary>
public readonly record struct BeltPlacement(GridPoint Position, Direction Direction);

/// <summary>
/// The player's solution: the nodes they placed on the grid. In M1 only belts are
/// placeable; math nodes, splitters and portals are added in later milestones.
/// </summary>
public sealed class Build
{
    public IReadOnlyList<BeltPlacement> Belts { get; init; } = [];

    /// <summary>Footprint metric: number of player-placed nodes (leaderboard).</summary>
    public int Footprint => Belts.Count;
}
