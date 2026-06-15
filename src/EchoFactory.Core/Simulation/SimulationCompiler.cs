namespace EchoFactory.Core;

/// <summary>
/// Compiles a level + build into a full timeline. M1 is a single forward pass (no temporal
/// portals yet — the multi-pass fixed-point loop arrives in M3, simulation-engine.md §3).
/// </summary>
public static class SimulationCompiler
{
    public static SimulationResult Compile(LevelDefinition level, Build build)
    {
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(build);

        var nodes = NodeFactory.Create(level, build);

        var states = new List<GridState>(level.MaxTicks + 1) { GridState.Empty(0) };

        // Sink receipts: sink id -> values received, in arrival order.
        var receipts = new Dictionary<NodeId, List<int>>();
        int lastDeliveryTick = 0;

        for (int t = 0; t < level.MaxTicks; t++)
        {
            var ctx = new SimContext(t, level.Grid);
            var builder = new GridStateBuilder(states[t], level.Grid);

            // PROPOSE — node order is irrelevant (propose/commit model).
            foreach (var node in nodes)
            {
                node.Evaluate(in ctx, states[t], builder);
            }

            foreach (var consumed in builder.Consumed)
            {
                if (!receipts.TryGetValue(consumed.Sink, out var list))
                {
                    receipts[consumed.Sink] = list = [];
                }

                list.Add(consumed.Item.Value);
                lastDeliveryTick = consumed.Tick;
            }

            // COMMIT.
            if (!builder.TryCommit(out var next, out var paradox))
            {
                var stats = new SimulationStats(t, build.Footprint, Passes: 1);
                return SimulationResult.Paradox(paradox!, states, stats);
            }

            states.Add(next);
        }

        var outcome = EvaluateOutcome(level, receipts);
        int finalTick = outcome == LevelOutcome.Solved ? lastDeliveryTick : level.MaxTicks;
        return SimulationResult.Completed(outcome, states, new SimulationStats(finalTick, build.Footprint, Passes: 1));
    }

    private static LevelOutcome EvaluateOutcome(LevelDefinition level, Dictionary<NodeId, List<int>> receipts)
    {
        foreach (var sink in level.Sinks)
        {
            var id = NodeId.FromString(sink.Id);
            IReadOnlyList<int> received = receipts.TryGetValue(id, out var list) ? list : [];

            if (!received.SequenceEqual(sink.Expected))
            {
                return LevelOutcome.Failed;
            }
        }

        return LevelOutcome.Solved;
    }
}

/// <summary>Instantiates engine nodes from a level + build in a fixed, deterministic order.</summary>
internal static class NodeFactory
{
    public static List<INode> Create(LevelDefinition level, Build build)
    {
        var nodes = new List<INode>(level.Generators.Count + level.Sinks.Count + build.Belts.Count);

        foreach (var g in level.Generators)
        {
            nodes.Add(new GeneratorNode(NodeId.FromString(g.Id), g.Position, g.Output, g.Schedule));
        }

        foreach (var s in level.Sinks)
        {
            nodes.Add(new SinkNode(NodeId.FromString(s.Id), s.Position, s.Expected));
        }

        foreach (var b in build.Belts)
        {
            nodes.Add(new BeltNode(NodeId.FromPlacement(NodeKind.Belt, b.Position), b.Position, b.Direction));
        }

        return nodes;
    }
}
