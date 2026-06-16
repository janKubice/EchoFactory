using EchoFactory.Core;

namespace EchoFactory.Game;

/// <summary>The player's editable build: placing/removing nodes with snapshot-based undo/redo.</summary>
internal sealed class BuildEditor
{
    private readonly LevelDefinition _level;
    private readonly HashSet<GridPoint> _fixed = [];
    private readonly Stack<Dictionary<GridPoint, PlacedNode>> _undo = new();
    private readonly Stack<Dictionary<GridPoint, PlacedNode>> _redo = new();
    private Dictionary<GridPoint, PlacedNode> _nodes = [];

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

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public bool CanPlace(GridPoint p) => _level.Grid.Contains(p) && !_fixed.Contains(p);

    /// <summary>Places a node; returns true if the build actually changed.</summary>
    public bool Place(PlacedNode node)
    {
        if (!CanPlace(node.Position))
        {
            return false;
        }

        if (_nodes.TryGetValue(node.Position, out var existing) && Signature(existing) == Signature(node))
        {
            return false; // identical node already there — no-op (avoids spamming undo while dragging)
        }

        Snapshot();
        _nodes[node.Position] = node;
        return true;
    }

    public bool Remove(GridPoint p)
    {
        if (_fixed.Contains(p) || !_nodes.ContainsKey(p))
        {
            return false;
        }

        Snapshot();
        _nodes.Remove(p);
        return true;
    }

    public PlacedNode? At(GridPoint p) => _nodes.GetValueOrDefault(p);

    public bool Clear()
    {
        if (_nodes.Count == 0)
        {
            return false;
        }

        Snapshot();
        _nodes.Clear();
        return true;
    }

    public void LoadFrom(Build build)
    {
        Snapshot();
        var next = new Dictionary<GridPoint, PlacedNode>();
        foreach (var node in build.Nodes)
        {
            if (CanPlace(node.Position))
            {
                next[node.Position] = node;
            }
        }

        _nodes = next;
    }

    public bool Undo()
    {
        if (_undo.Count == 0)
        {
            return false;
        }

        _redo.Push(new Dictionary<GridPoint, PlacedNode>(_nodes));
        _nodes = _undo.Pop();
        return true;
    }

    public bool Redo()
    {
        if (_redo.Count == 0)
        {
            return false;
        }

        _undo.Push(new Dictionary<GridPoint, PlacedNode>(_nodes));
        _nodes = _redo.Pop();
        return true;
    }

    public Build ToBuild() => new() { Nodes = _nodes.Values.OrderBy(static n => n.Position).ToList() };

    private void Snapshot()
    {
        _undo.Push(new Dictionary<GridPoint, PlacedNode>(_nodes));
        _redo.Clear();
    }

    private static string Signature(PlacedNode n) => n.Kind switch
    {
        NodeKind.Belt => $"b{n.Direction}",
        NodeKind.Math => $"m{n.Math!.Operation}{n.Math!.Output}{n.Math!.Constant}",
        NodeKind.Splitter => $"s{n.Splitter!.OutputA}{n.Splitter!.OutputB}{n.Splitter!.StartWithA}",
        NodeKind.Portal => $"p{n.Portal!.TimeOffset}{n.Portal!.Output}",
        NodeKind.Filter => $"f{n.Filter!.Comparison}{n.Filter!.Constant}{n.Filter!.Output}",
        _ => "?",
    };
}
