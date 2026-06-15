namespace EchoFactory.Core;

/// <summary>Dimensions of the play grid.</summary>
public readonly struct GridSize
{
    public readonly int Width;
    public readonly int Height;

    public GridSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>True if the point lies inside the grid bounds.</summary>
    public bool Contains(GridPoint p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;
}
