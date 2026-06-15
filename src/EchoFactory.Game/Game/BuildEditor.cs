using EchoFactory.Core;

namespace EchoFactory.Game;

/// <summary>The player's editable build: placing/removing nodes on free grid cells.</summary>
internal sealed class BuildEditor
{
    private readonly LevelDefinition _level;
    private readonly Dictionary<GridPoint, PlacedNode> _nodes = [];
    private readonly HashSet<GridPoint> _fixed = [];

    public BuildEditor(LevelDefinition level)
    {
        _level = level;
        foreach (var g in level.Generators)
        {
            _fixed.Add(g.Position);
        }

        foreach (var s in level.Sinks)
        {
            _fixed.Add(s.Position);
        }
    }

    public IEnumerable<PlacedNode> Nodes => _nodes.Values;

    public int Count => _nodes.Count;

    public bool CanPlace(GridPoint p) => _level.Grid.Contains(p) && !_fixed.Contains(p);

    public void Place(PlacedNode node)
    {
        if (CanPlace(node.Position))
        {
            _nodes[node.Position] = node;
        }
    }

    public void Remove(GridPoint p)
    {
        if (!_fixed.Contains(p))
        {
            _nodes.Remove(p);
        }
    }

    public PlacedNode? At(GridPoint p) => _nodes.GetValueOrDefault(p);

    public void Clear() => _nodes.Clear();

    public void LoadFrom(Build build)
    {
        _nodes.Clear();
        foreach (var node in build.Nodes)
        {
            if (CanPlace(node.Position))
            {
                _nodes[node.Position] = node;
            }
        }
    }

    public Build ToBuild() => new() { Nodes = _nodes.Values.OrderBy(static n => n.Position).ToList() };
}
