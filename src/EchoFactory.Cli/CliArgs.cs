namespace EchoFactory.Cli;

/// <summary>Tiny argument splitter: extracts <c>--data &lt;dir&gt;</c> and the positionals.</summary>
internal static class CliArgs
{
    public static (string? Command, List<string> Positionals, string DataDir) Parse(string[] args)
    {
        string dataDir = "data";
        var rest = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--data" && i + 1 < args.Length)
            {
                dataDir = args[++i];
            }
            else
            {
                rest.Add(args[i]);
            }
        }

        string? command = rest.Count > 0 ? rest[0] : null;
        List<string> positionals = rest.Count > 1 ? rest.GetRange(1, rest.Count - 1) : [];
        return (command, positionals, dataDir);
    }
}
