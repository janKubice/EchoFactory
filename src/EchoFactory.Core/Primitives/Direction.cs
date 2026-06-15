namespace EchoFactory.Core;

/// <summary>Cardinal movement direction. Y grows downward (screen coordinates).</summary>
public enum Direction : byte
{
    Up,
    Right,
    Down,
    Left,
}

public static class DirectionExtensions
{
    /// <summary>Unit offset for the direction.</summary>
    public static GridPoint Offset(this Direction dir) => dir switch
    {
        Direction.Up => new GridPoint(0, -1),
        Direction.Right => new GridPoint(1, 0),
        Direction.Down => new GridPoint(0, 1),
        Direction.Left => new GridPoint(-1, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(dir)),
    };
}
