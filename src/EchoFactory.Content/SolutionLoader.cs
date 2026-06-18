using System.Text.Json;
using EchoFactory.Core;

namespace EchoFactory.Content;

/// <summary>A parsed solution: which level it targets plus the player <see cref="Build"/>.</summary>
public sealed class SolutionInfo
{
    public required string LevelId { get; init; }

    public string? LevelHash { get; init; }

    public required Build Build { get; init; }

    /// <summary>Node definition ids used, in placement order (for inventory checks).</summary>
    public required IReadOnlyList<string> NodeIds { get; init; }
}

/// <summary>Parses a solution JSON into a <see cref="Build"/>, resolving node types via the registry.</summary>
public static class SolutionLoader
{
    public static SolutionInfo LoadFile(string path, NodeRegistry registry) =>
        Parse(File.ReadAllText(path), Path.GetFileName(path), registry);

    public static SolutionInfo Parse(string json, string source, NodeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        SolutionDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<SolutionDto>(json, JsonConfig.Options);
        }
        catch (JsonException e)
        {
            throw new ContentException($"{source}: invalid JSON ({e.Message})", e);
        }

        if (dto is null)
        {
            throw new ContentException($"{source}: empty solution");
        }

        SchemaVersions.Ensure(source, dto.SchemaVersion, SchemaVersions.Solution);
        string levelId = NodeRegistry.Require(dto.LevelId, "level_id", source);

        var nodes = new List<PlacedNode>();
        var nodeIds = new List<string>();
        foreach (var pn in dto.PlacedNodes ?? [])
        {
            string nodeId = NodeRegistry.Require(pn.Node, "placed_nodes[].node", source);
            nodeIds.Add(nodeId);
            string where = $"{source} (node '{nodeId}')";
            NodeDefinition def = registry.Find(nodeId)
                ?? throw new ContentException($"{where}: unknown node id");
            GridPoint pos = LevelLoader.Point(pn.Position, where);

            switch (def.Kind)
            {
                case NodeKind.Belt:
                    nodes.Add(PlacedNode.Belt(pos, Tokens.Dir(pn.Direction, where)));
                    break;

                case NodeKind.Math:
                    nodes.Add(PlacedNode.MathOp(pos, new MathConfig
                    {
                        Operation = def.Operation
                            ?? throw new ContentException($"{where}: math definition has no operation"),
                        Output = Tokens.Dir(pn.Direction, where),
                        Constant = pn.Constant,
                    }));
                    break;

                case NodeKind.Splitter:
                    nodes.Add(PlacedNode.Split(pos, new SplitterConfig
                    {
                        OutputA = Tokens.Dir(pn.OutputA, where),
                        OutputB = Tokens.Dir(pn.OutputB, where),
                        StartWithA = pn.StartWithA,
                    }));
                    break;

                case NodeKind.Portal:
                    if (pn.TimeOffset is not int offset)
                    {
                        throw new ContentException($"{where}: portal requires a time_offset");
                    }

                    nodes.Add(PlacedNode.TimePortal(pos, new PortalConfig
                    {
                        TimeOffset = offset,
                        Output = Tokens.Dir(pn.Direction, where),
                    }));
                    break;

                case NodeKind.Filter:
                    if (pn.Constant is not int threshold)
                    {
                        throw new ContentException($"{where}: filter requires a constant");
                    }

                    nodes.Add(PlacedNode.Gate(pos, new FilterConfig
                    {
                        Comparison = Tokens.Comparison(pn.Comparison, where),
                        Constant = threshold,
                        Output = Tokens.Dir(pn.Direction, where),
                    }));
                    break;

                case NodeKind.Router:
                    if (pn.Constant is not int routerConstant)
                    {
                        throw new ContentException($"{where}: router requires a constant");
                    }

                    nodes.Add(PlacedNode.Route(pos, new RouterConfig
                    {
                        Comparison = Tokens.Comparison(pn.Comparison, where),
                        Constant = routerConstant,
                        OutMatch = Tokens.Dir(pn.OutMatch, where),
                        OutElse = Tokens.Dir(pn.OutElse, where),
                    }));
                    break;

                case NodeKind.Accumulator:
                    if (pn.Constant is not int releaseConstant)
                    {
                        throw new ContentException($"{where}: accumulator requires a constant");
                    }

                    nodes.Add(PlacedNode.Accumulate(pos, new AccumulatorConfig
                    {
                        ReleaseWhen = Tokens.Comparison(pn.Comparison, where),
                        Constant = releaseConstant,
                        Output = Tokens.Dir(pn.Direction, where),
                        Initial = pn.Initial ?? 0,
                    }));
                    break;

                default:
                    throw new ContentException($"{where}: cannot place a node of kind {def.Kind} in a solution");
            }
        }

        return new SolutionInfo
        {
            LevelId = levelId,
            LevelHash = dto.LevelHash,
            Build = new Build { Nodes = nodes },
            NodeIds = nodeIds,
        };
    }
}
