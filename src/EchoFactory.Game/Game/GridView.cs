using EchoFactory.Core;
using Microsoft.Xna.Framework;

namespace EchoFactory.Game;

/// <summary>Maps grid cells to/from screen space, fitting the grid into a screen rectangle.</summary>
internal sealed class GridView
{
    public GridView(GridSize grid, Rectangle area)
    {
        Grid = grid;
        float cellW = area.Width / (float)grid.Width;
        float cellH = area.Height / (float)grid.Height;
        CellSize = MathF.Min(cellW, cellH);

        float gridW = CellSize * grid.Width;
        float gridH = CellSize * grid.Height;
        Origin = new Vector2(area.X + ((area.Width - gridW) / 2f), area.Y + ((area.Height - gridH) / 2f));
    }

    public GridSize Grid { get; }

    public float CellSize { get; }

    public Vector2 Origin { get; }

    public Vector2 CellTopLeft(GridPoint p) => Origin + new Vector2(p.X * CellSize, p.Y * CellSize);

    public Vector2 CellCenter(GridPoint p) => CellTopLeft(p) + new Vector2(CellSize / 2f, CellSize / 2f);

    public Vector2 CellCenter(float x, float y) => Origin + new Vector2((x + 0.5f) * CellSize, (y + 0.5f) * CellSize);

    public bool TryScreenToCell(Vector2 screen, out GridPoint cell)
    {
        Vector2 local = screen - Origin;
        int cx = (int)MathF.Floor(local.X / CellSize);
        int cy = (int)MathF.Floor(local.Y / CellSize);
        cell = new GridPoint(cx, cy);
        return Grid.Contains(cell);
    }
}
