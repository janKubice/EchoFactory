using System.Text.Json;
using EchoFactory.Core;

namespace EchoFactory.Content;

/// <summary>Parses a level JSON into the engine's <see cref="LevelDefinition"/>.</summary>
public static class LevelLoader
{
    public static LevelDefinition LoadFile(string path) =>
        Parse(File.ReadAllText(path), Path.GetFileName(path));

    public static LevelDefinition Parse(string json, string source)
    {
        LevelDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<LevelDto>(json, JsonConfig.Options);
        }
        catch (JsonException e)
        {
            throw new ContentException($"{source}: invalid JSON ({e.Message})", e);
        }

        if (dto is null)
        {
            throw new ContentException($"{source}: empty level");
        }

        SchemaVersions.Ensure(source, dto.SchemaVersion, SchemaVersions.Level);
        string id = NodeRegistry.Require(dto.Id, "id", source);

        if (dto.Grid is null)
        {
            throw new ContentException($"{source}: missing grid");
        }

        if (dto.MaxTicks <= 0)
        {
            throw new ContentException($"{source}: max_ticks must be > 0");
        }

        var generators = new List<GeneratorSpec>();
        var sinks = new List<SinkSpec>();

        foreach (var fn in dto.FixedNodes ?? [])
        {
            string fid = NodeRegistry.Require(fn.Id, "fixed_nodes[].id", source);
            string where = $"{source} (fixed '{fid}')";
            NodeKind kind = Tokens.Kind(fn.Type, where);
            GridPoint pos = Point(fn.Position, where);

            switch (kind)
            {
                case NodeKind.Generator:
                    if (fn.Schedule is null || fn.Schedule.Count == 0)
                    {
                        throw new ContentException($"{where}: generator requires a non-empty schedule");
                    }

                    generators.Add(new GeneratorSpec
                    {
                        Id = fid,
                        Position = pos,
                        Output = Tokens.Dir(fn.Direction, where),
                        Schedule = fn.Schedule.ConvertAll(s => new SpawnEntry(s.Tick, s.Value)),
                    });
                    break;

                case NodeKind.Sink:
                    sinks.Add(new SinkSpec
                    {
                        Id = fid,
                        Position = pos,
                        Expected = fn.Expected ?? [],
                    });
                    break;

                default:
                    throw new ContentException($"{where}: fixed node type '{fn.Type}' not allowed (only generator/sink)");
            }
        }

        return new LevelDefinition
        {
            Id = id,
            Grid = new GridSize(dto.Grid.Width, dto.Grid.Height),
            MaxTicks = dto.MaxTicks,
            MaxTemporalPasses = dto.MaxTemporalPasses,
            StrictTiming = dto.StrictTiming,
            Generators = generators,
            Sinks = sinks,
            Par = dto.Par is null ? null : new LevelPar { Ticks = dto.Par.Ticks, Footprint = dto.Par.Footprint },
        };
    }

    internal static GridPoint Point(PointDto? p, string where) =>
        p is null ? throw new ContentException($"{where}: missing position") : new GridPoint(p.X, p.Y);
}
