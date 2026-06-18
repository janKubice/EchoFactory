using System.Text.Json;
using EchoFactory.Core;

namespace EchoFactory.Content;

/// <summary>Registry of node *types* parsed from <c>/data/nodes/*.json</c>.</summary>
public sealed class NodeRegistry
{
    private readonly Dictionary<string, NodeDefinition> _defs = new(StringComparer.Ordinal);

    public int Count => _defs.Count;

    public IReadOnlyCollection<NodeDefinition> Definitions => _defs.Values;

    public NodeDefinition? Find(string id) => _defs.GetValueOrDefault(id);

    public NodeDefinition Get(string id) =>
        Find(id) ?? throw new ContentException($"unknown node id '{id}'");

    public void Add(NodeDefinition def)
    {
        if (!_defs.TryAdd(def.Id, def))
        {
            throw new ContentException($"duplicate node id '{def.Id}'");
        }
    }

    public static NodeRegistry LoadFromDirectory(string dir)
    {
        var registry = new NodeRegistry();
        foreach (string file in EnumerateJson(dir))
        {
            registry.Add(ParseDefinition(File.ReadAllText(file), Path.GetFileName(file)));
        }

        return registry;
    }

    public static NodeDefinition ParseDefinition(string json, string source)
    {
        NodeDefinitionDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<NodeDefinitionDto>(json, JsonConfig.Options);
        }
        catch (JsonException e)
        {
            throw new ContentException($"{source}: invalid JSON ({e.Message})", e);
        }

        if (dto is null)
        {
            throw new ContentException($"{source}: empty node definition");
        }

        SchemaVersions.Ensure(source, dto.SchemaVersion, SchemaVersions.Node);
        string id = Require(dto.Id, "id", source);
        string where = $"{source} (id '{id}')";
        NodeKind kind = Tokens.Kind(dto.Type, where);

        MathOperation? operation = null;
        if (kind == NodeKind.Math)
        {
            if (dto.Logic?.Operation is null)
            {
                throw new ContentException($"{where}: math node requires logic.operation");
            }

            operation = Tokens.Op(dto.Logic.Operation, where);
        }

        return new NodeDefinition
        {
            Id = id,
            Name = dto.Name ?? id,
            Kind = kind,
            Category = dto.Category ?? string.Empty,
            Description = dto.Description ?? string.Empty,
            Operation = operation,
            TickCost = dto.TickCost,
            Visual = new NodeVisual
            {
                Shape = dto.Visual?.Shape ?? "square",
                ColorHex = dto.Visual?.ColorHex ?? "#FFFFFF",
                Icon = dto.Visual?.Icon ?? string.Empty,
            },
        };
    }

    internal static string Require(string? value, string field, string source) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ContentException($"{source}: missing required '{field}'")
            : value;

    internal static IEnumerable<string> EnumerateJson(string dir) =>
        Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir, "*.json").OrderBy(static f => f, StringComparer.Ordinal)
            : [];
}
