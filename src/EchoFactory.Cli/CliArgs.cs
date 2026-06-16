namespace EchoFactory.Cli;

/// <summary>Splits args into a command, positionals, the data dir and generic <c>--key value</c> options.</summary>
internal static class CliArgs
{
    public static (string? Command, List<string> Positionals, string DataDir, Dictionary<string, string> Options) Parse(string[] args)
    {
        string dataDir = "data";
        var rest = new List<string>();
        var options = new Dictionary<string, string>(StringComparer.Ordinal);

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal) && i + 1 < args.Length)
            {
                string key = args[i][2..];
                string value = args[++i];
                if (key == "data")
                {
                    dataDir = value;
                }
                else
                {
                    options[key] = value;
                }
            }
            else
            {
                rest.Add(args[i]);
            }
        }

        string? command = rest.Count > 0 ? rest[0] : null;
        List<string> positionals = rest.Count > 1 ? rest.GetRange(1, rest.Count - 1) : [];
        return (command, positionals, dataDir, options);
    }
}
