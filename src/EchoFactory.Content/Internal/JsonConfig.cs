using System.Text.Json;
using EchoFactory.Core;

namespace EchoFactory.Content;

/// <summary>Shared System.Text.Json options. snake_case matches the JSON content format.</summary>
internal static class JsonConfig
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}

/// <summary>Supported schema versions + the version gate (migration framework hook point).</summary>
internal static class SchemaVersions
{
    public const int Node = 1;
    public const int Level = 1;
    public const int Solution = 1;

    public static void Ensure(string source, int version, int current)
    {
        if (version < 1)
        {
            throw new ContentException($"{source}: invalid schema_version {version} (must be >= 1)");
        }

        if (version > current)
        {
            throw new ContentException(
                $"{source}: schema_version {version} is newer than supported ({current}); update the game");
        }

        // version in [1, current]: future migrations would run here. Only v1 exists today.
    }
}

/// <summary>String → enum parsers with loud, specific errors for modders.</summary>
internal static class Tokens
{
    public static NodeKind Kind(string? s, string where) => Normalize(s) switch
    {
        "generator" => NodeKind.Generator,
        "sink" => NodeKind.Sink,
        "belt" => NodeKind.Belt,
        "math" => NodeKind.Math,
        "splitter" => NodeKind.Splitter,
        "portal" => NodeKind.Portal,
        _ => throw new ContentException($"{where}: unknown node type '{s}'"),
    };

    public static MathOperation Op(string? s, string where) => Normalize(s) switch
    {
        "add" => MathOperation.Add,
        "sub" => MathOperation.Sub,
        "mul" => MathOperation.Mul,
        "div" => MathOperation.Div,
        "mod" => MathOperation.Mod,
        "min" => MathOperation.Min,
        "max" => MathOperation.Max,
        _ => throw new ContentException($"{where}: unknown operation '{s}'"),
    };

    public static Direction Dir(string? s, string where) => Normalize(s) switch
    {
        "up" => Direction.Up,
        "right" => Direction.Right,
        "down" => Direction.Down,
        "left" => Direction.Left,
        _ => throw new ContentException($"{where}: unknown direction '{s}'"),
    };

    private static string Normalize(string? s) => (s ?? string.Empty).Trim().ToLowerInvariant();
}
