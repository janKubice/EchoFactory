using System.Text.Json;
using EchoFactory.Core;

namespace EchoFactory.Content;

/// <summary>Serializes a <see cref="LevelDefinition"/> back to level JSON (in-game editor save).</summary>
public static class LevelWriter
{
    public static string Write(LevelDefinition level)
    {
        ArgumentNullException.ThrowIfNull(level);

        var fixedNodes = new List<FixedNodeDto>();
        foreach (var g in level.Generators)
        {
            fixedNodes.Add(new FixedNodeDto
            {
                Id = g.Id,
                Type = "generator",
                Position = SolutionWriter.Point(g.Position),
                Direction = SolutionWriter.Dir(g.Output),
                Schedule = g.Schedule.Select(s => new SpawnDto { Tick = s.Tick, Value = s.Value }).ToList(),
            });
        }

        foreach (var s in level.Sinks)
        {
            fixedNodes.Add(new FixedNodeDto
            {
                Id = s.Id,
                Type = "sink",
                Position = SolutionWriter.Point(s.Position),
                Expected = s.Expected.ToList(),
                ExpectedTicks = s.ExpectedTicks?.ToList(),
            });
        }

        var dto = new LevelDto
        {
            SchemaVersion = SchemaVersions.Level,
            Id = level.Id,
            Name = string.IsNullOrEmpty(level.Name) ? null : level.Name,
            Description = string.IsNullOrEmpty(level.Description) ? null : level.Description,
            Order = level.Order,
            Campaign = string.IsNullOrEmpty(level.Campaign) ? null : level.Campaign,
            Grid = new GridDto { Width = level.Grid.Width, Height = level.Grid.Height },
            MaxTicks = level.MaxTicks,
            MaxTemporalPasses = level.MaxTemporalPasses,
            StrictTiming = level.StrictTiming,
            Par = level.Par is null ? null : new ParDto { Ticks = level.Par.Ticks, Footprint = level.Par.Footprint },
            Inventory = ToInventoryDto(level.Inventory),
            FixedNodes = fixedNodes,
        };

        return JsonSerializer.Serialize(dto, JsonConfig.Options);
    }

    private static InventoryDto? ToInventoryDto(LevelInventory? inventory)
    {
        if (inventory is null)
        {
            return null;
        }

        var listed = inventory.Listed.OrderBy(static x => x, StringComparer.Ordinal).ToList();
        return new InventoryDto
        {
            Mode = inventory.Mode.ToString().ToLowerInvariant(),
            Allowed = inventory.Mode == InventoryMode.Whitelist ? listed : null,
            Blocked = inventory.Mode == InventoryMode.Blacklist ? listed : null,
            Limits = inventory.Limits.Count == 0 ? null : inventory.Limits.ToDictionary(static kv => kv.Key, static kv => kv.Value),
        };
    }
}
