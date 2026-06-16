namespace EchoFactory.Core;

/// <summary>
/// Compiles a level + build into a full timeline. Without portals this is a single forward pass.
/// With time portals it iterates passes to a fixed point of the injection set (F(I*) = I*),
/// detecting oscillation and non-convergence as TemporalParadox (simulation-engine.md §3).
/// </summary>
public static class SimulationCompiler
{
    public static SimulationResult Compile(LevelDefinition level, Build build)
    {
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(build);

        int maxPasses = level.MaxTemporalPasses > 0 ? level.MaxTemporalPasses : 5;
        int footprint = build.Footprint;

        InjectionSet injections = InjectionSet.Empty;
        var seenHashes = new HashSet<ulong>();
        PassResult pass = default!;

        for (int passIndex = 0; passIndex <= maxPasses; passIndex++)
        {
            pass = RunSinglePass(level, build, injections);

            if (pass.Paradox is not null)
            {
                return SimulationResult.Paradox(
                    pass.Paradox, pass.States, new SimulationStats(pass.Paradox.Tick, footprint, passIndex + 1));
            }

            if (pass.Injections.SetEquals(injections))
            {
                // Fixed point: this pass is self-consistent (its assumed injections == produced).
                LevelOutcome outcome = EvaluateOutcome(level, pass.Receipts);
                int finalTick = outcome == LevelOutcome.Solved ? pass.LastDeliveryTick : level.MaxTicks;

                // Once solved there is nothing left to watch — trim the idle trailing ticks so
                // playback ends when the goal is met (keeps one frame past the last delivery).
                IReadOnlyList<GridState> states = pass.States;
                if (outcome == LevelOutcome.Solved)
                {
                    int keep = Math.Min(pass.States.Count, finalTick + 2);
                    states = pass.States.GetRange(0, keep);
                }

                return SimulationResult.Completed(
                    outcome, states, new SimulationStats(finalTick, footprint, passIndex + 1));
            }

            if (!seenHashes.Add(pass.Injections.Hash()))
            {
                return SimulationResult.Paradox(
                    ParadoxError.Temporal("oscillating (unstable) loop", passIndex + 1),
                    pass.States, new SimulationStats(level.MaxTicks, footprint, passIndex + 1));
            }

            injections = pass.Injections;
        }

        return SimulationResult.Paradox(
            ParadoxError.Temporal("loop did not converge", maxPasses + 1),
            pass.States, new SimulationStats(level.MaxTicks, footprint, maxPasses + 1));
    }

    private static PassResult RunSinglePass(LevelDefinition level, Build build, InjectionSet injections)
    {
        // Fresh nodes each pass => clean per-pass state (splitter toggle, math buffer).
        var nodes = NodeFactory.Create(level, build);

        var states = new List<GridState>(level.MaxTicks + 1) { GridState.Empty(0) };
        var receipts = new Dictionary<NodeId, List<(int Value, int Tick)>>();
        var emissions = new List<PortalEmission>();
        int lastDelivery = 0;

        var byTick = new Dictionary<int, List<Injection>>();
        foreach (var inj in injections.Items)
        {
            // Off-timeline injections (before the start / past the end) simply never apply.
            if (inj.ApplyTick < 0 || inj.ApplyTick >= level.MaxTicks)
            {
                continue;
            }

            if (!byTick.TryGetValue(inj.ApplyTick, out var list))
            {
                byTick[inj.ApplyTick] = list = [];
            }

            list.Add(inj);
        }

        for (int t = 0; t < level.MaxTicks; t++)
        {
            var ctx = new SimContext(t, level.Grid);
            var builder = new GridStateBuilder(states[t], level.Grid);

            if (byTick.TryGetValue(t, out var injectionsHere))
            {
                foreach (var inj in injectionsHere)
                {
                    builder.Inject(inj.ExitCell, new Item(inj.Id, inj.Value));
                }
            }

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

                list.Add((consumed.Item.Value, consumed.Tick));
                lastDelivery = consumed.Tick;
            }

            emissions.AddRange(builder.Emissions);

            if (!builder.TryCommit(out var next, out var paradox))
            {
                return new PassResult
                {
                    States = states,
                    Injections = InjectionSet.Empty,
                    Receipts = receipts,
                    Paradox = paradox,
                };
            }

            states.Add(next);
        }

        var produced = new InjectionSet(
            emissions.Select(e => new Injection(e.ExitCell, e.ApplyTick, e.Item.Value, e.Item.Id)));

        return new PassResult
        {
            States = states,
            Injections = produced,
            Receipts = receipts,
            LastDeliveryTick = lastDelivery,
        };
    }

    private static LevelOutcome EvaluateOutcome(LevelDefinition level, Dictionary<NodeId, List<(int Value, int Tick)>> receipts)
    {
        foreach (var sink in level.Sinks)
        {
            var id = NodeId.FromString(sink.Id);
            IReadOnlyList<(int Value, int Tick)> received = receipts.TryGetValue(id, out var list) ? list : [];

            if (!received.Select(static r => r.Value).SequenceEqual(sink.Expected))
            {
                return LevelOutcome.Failed;
            }

            if (level.StrictTiming
                && sink.ExpectedTicks is { } ticks
                && !received.Select(static r => r.Tick).SequenceEqual(ticks))
            {
                return LevelOutcome.Failed;
            }
        }

        return LevelOutcome.Solved;
    }

    private sealed class PassResult
    {
        public required List<GridState> States { get; init; }

        public required InjectionSet Injections { get; init; }

        public required Dictionary<NodeId, List<(int Value, int Tick)>> Receipts { get; init; }

        public int LastDeliveryTick { get; init; }

        public ParadoxError? Paradox { get; init; }
    }
}

/// <summary>Instantiates engine nodes from a level + build in a fixed, deterministic order.</summary>
internal static class NodeFactory
{
    public static List<INode> Create(LevelDefinition level, Build build)
    {
        var nodes = new List<INode>(level.Generators.Count + level.Sinks.Count + build.Nodes.Count);

        foreach (var g in level.Generators)
        {
            nodes.Add(new GeneratorNode(NodeId.FromString(g.Id), g.Position, g.Output, g.Schedule));
        }

        foreach (var s in level.Sinks)
        {
            nodes.Add(new SinkNode(NodeId.FromString(s.Id), s.Position, s.Expected));
        }

        foreach (var placed in build.Nodes)
        {
            nodes.Add(CreatePlaced(placed));
        }

        return nodes;
    }

    private static INode CreatePlaced(PlacedNode p)
    {
        NodeId id = NodeId.FromPlacement(p.Kind, p.Position);
        return p.Kind switch
        {
            NodeKind.Belt => new BeltNode(id, p.Position, p.Direction),
            NodeKind.Math => new GenericMathNode(id, p.Position, p.Math
                ?? throw new ArgumentException($"Math placement at {p.Position} has no MathConfig")),
            NodeKind.Splitter => new SplitterNode(id, p.Position, p.Splitter
                ?? throw new ArgumentException($"Splitter placement at {p.Position} has no SplitterConfig")),
            NodeKind.Portal => new PortalNode(id, p.Position, p.Portal
                ?? throw new ArgumentException($"Portal placement at {p.Position} has no PortalConfig")),
            NodeKind.Filter => new FilterNode(id, p.Position, p.Filter
                ?? throw new ArgumentException($"Filter placement at {p.Position} has no FilterConfig")),
            _ => throw new ArgumentException($"Unsupported placed node kind: {p.Kind}"),
        };
    }
}
