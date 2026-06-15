namespace EchoFactory.Core;

/// <summary>Read-only context passed to nodes during evaluation.</summary>
public readonly struct SimContext
{
    public readonly int Tick;
    public readonly GridSize Grid;

    public SimContext(int tick, GridSize grid)
    {
        Tick = tick;
        Grid = grid;
    }
}

/// <summary>
/// A node on the grid. <see cref="Evaluate"/> is a pure proposal step: it reads
/// <paramref name="current"/> (tick T) and writes proposals into the builder for tick T+1.
/// It must NOT mutate current state nor have side effects beyond the builder
/// (propose/commit model — simulation-engine.md §2).
/// </summary>
public interface INode
{
    NodeId Id { get; }

    GridPoint Position { get; }

    void Evaluate(in SimContext ctx, GridState current, GridStateBuilder builder);
}
