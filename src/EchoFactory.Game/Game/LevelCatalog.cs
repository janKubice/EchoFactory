using EchoFactory.Content;
using EchoFactory.Core;

namespace EchoFactory.Game;

/// <summary>Loads the node registry and the list of playable levels from a data directory.</summary>
internal sealed class LevelCatalog
{
    public LevelCatalog(string dataDir)
    {
        DataDir = dataDir;
        Registry = NodeRegistry.LoadFromDirectory(Path.Combine(dataDir, "nodes"));
        Reload();
    }

    public string DataDir { get; }

    public NodeRegistry Registry { get; }

    public IReadOnlyList<string> LevelIds { get; private set; } = [];

    /// <summary>Re-scans the levels directory (so editor-saved levels appear).</summary>
    public void Reload()
    {
        string levelsDir = Path.Combine(DataDir, "levels");
        LevelIds = Directory.Exists(levelsDir)
            ? Directory.EnumerateFiles(levelsDir, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(static id => id is not null)
                .Select(static id => id!)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToList()
            : [];
    }

    public LevelDefinition LoadLevel(string id) =>
        LevelLoader.LoadFile(Path.Combine(DataDir, "levels", id + ".json"));

    /// <summary>Finds a reference solution for the level (used by the "load solution" shortcut).</summary>
    public SolutionInfo? LoadReferenceSolution(string levelId)
    {
        string solutionsDir = Path.Combine(DataDir, "solutions");
        if (!Directory.Exists(solutionsDir))
        {
            return null;
        }

        foreach (string file in Directory.EnumerateFiles(solutionsDir, "*.json").OrderBy(static f => f, StringComparer.Ordinal))
        {
            try
            {
                SolutionInfo solution = SolutionLoader.LoadFile(file, Registry);
                if (solution.LevelId == levelId)
                {
                    return solution;
                }
            }
            catch (ContentException)
            {
                // skip invalid solution files
            }
        }

        return null;
    }
}
