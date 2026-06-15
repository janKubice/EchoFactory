namespace EchoFactory.Game;

/// <summary>Finds the <c>/data</c> content directory by walking up from the executable.</summary>
internal static class DataLocator
{
    public static string FindDataDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "data");
            if (Directory.Exists(Path.Combine(candidate, "nodes")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "data");
    }
}
