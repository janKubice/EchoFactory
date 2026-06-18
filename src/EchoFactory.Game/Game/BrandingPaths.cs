namespace EchoFactory.Game;

/// <summary>Resolves configurable branding asset paths (logo, etc.) relative to the executable.</summary>
internal static class BrandingPaths
{
    public static string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
    }
}
