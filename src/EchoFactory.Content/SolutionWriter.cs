using System.Text.Json;
using EchoFactory.Core;

namespace EchoFactory.Content;

/// <summary>Serializes a player <see cref="Build"/> back to solution JSON (save / share).</summary>
public static class SolutionWriter
{
    public static string Write(string levelId, Build build, string? levelHash = null)
    {
        ArgumentNullException.ThrowIfNull(build);

        var dto = new SolutionDto
        {
            SchemaVersion = SchemaVersions.Solution,
            LevelId = levelId,
            LevelHash = levelHash,
            PlacedNodes = build.Nodes.Select(ToDto).ToList(),
        };

        return JsonSerializer.Serialize(dto, JsonConfig.Options);
    }

    private static PlacedNodeDto ToDto(PlacedNode n) => n.Kind switch
    {
        NodeKind.Belt => new PlacedNodeDto { Node = "node_belt", Position = Point(n.Position), Direction = Dir(n.Direction) },
        NodeKind.Math => new PlacedNodeDto { Node = MathDef(n.Math!.Operation), Position = Point(n.Position), Direction = Dir(n.Math!.Output), Constant = n.Math!.Constant },
        NodeKind.Splitter => new PlacedNodeDto { Node = "node_splitter", Position = Point(n.Position), OutputA = Dir(n.Splitter!.OutputA), OutputB = Dir(n.Splitter!.OutputB), StartWithA = n.Splitter!.StartWithA },
        NodeKind.Portal => new PlacedNodeDto { Node = "node_portal", Position = Point(n.Position), Direction = Dir(n.Portal!.Output), TimeOffset = n.Portal!.TimeOffset },
        NodeKind.Filter => new PlacedNodeDto { Node = "node_filter", Position = Point(n.Position), Direction = Dir(n.Filter!.Output), Comparison = Cmp(n.Filter!.Comparison), Constant = n.Filter!.Constant },
        NodeKind.Router => new PlacedNodeDto { Node = "node_router", Position = Point(n.Position), Comparison = Cmp(n.Router!.Comparison), Constant = n.Router!.Constant, OutMatch = Dir(n.Router!.OutMatch), OutElse = Dir(n.Router!.OutElse) },
        NodeKind.Accumulator => new PlacedNodeDto { Node = "node_accumulator", Position = Point(n.Position), Comparison = Cmp(n.Accumulator!.ReleaseWhen), Constant = n.Accumulator!.Constant, Direction = Dir(n.Accumulator!.Output), Initial = n.Accumulator!.Initial == 0 ? null : n.Accumulator!.Initial },
        _ => throw new ContentException($"cannot serialize node kind {n.Kind}"),
    };

    internal static PointDto Point(GridPoint g) => new() { X = g.X, Y = g.Y };

    internal static string Dir(Direction d) => d.ToString().ToLowerInvariant();

    internal static string Cmp(Comparison c) => c.ToString().ToLowerInvariant();

    private static string MathDef(MathOperation op) => "node_math_" + op.ToString().ToLowerInvariant();
}
