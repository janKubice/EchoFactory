namespace EchoFactory.Core;

/// <summary>
/// Integer cell coordinate. Value type, usable as a deterministic dictionary key.
/// Ordering is row-major (Y then X) for deterministic iteration.
/// </summary>
public readonly struct GridPoint : IEquatable<GridPoint>, IComparable<GridPoint>
{
    public readonly int X;
    public readonly int Y;

    public GridPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(GridPoint other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is GridPoint p && Equals(p);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public int CompareTo(GridPoint other)
    {
        int c = Y.CompareTo(other.Y);
        return c != 0 ? c : X.CompareTo(other.X);
    }

    public static GridPoint operator +(GridPoint a, GridPoint b) => new(a.X + b.X, a.Y + b.Y);

    public static bool operator ==(GridPoint a, GridPoint b) => a.Equals(b);

    public static bool operator !=(GridPoint a, GridPoint b) => !a.Equals(b);

    public override string ToString() => string.Create(
        System.Globalization.CultureInfo.InvariantCulture, $"({X},{Y})");
}
