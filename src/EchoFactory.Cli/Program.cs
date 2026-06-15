using EchoFactory.Cli;
using EchoFactory.Core;

if (args.Length == 0 || args[0] is "help" or "-h" or "--help")
{
    Console.WriteLine("EchoFactory headless CLI");
    Console.WriteLine();
    Console.WriteLine("Usage: echofactory <command>");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  demo    Compile the built-in line example and print the timeline.");
    Console.WriteLine("  help    Show this help.");
    Console.WriteLine();
    Console.WriteLine("(validate / verify / bench arrive in M2 with the JSON content pipeline.)");
    return 0;
}

switch (args[0])
{
    case "demo":
        return RunDemo();

    default:
        Console.Error.WriteLine($"Unknown command: '{args[0]}'. Try 'help'.");
        return 1;
}

static int RunDemo()
{
    var level = Examples.LineDemoLevel();
    var build = Examples.LineDemoSolution();
    var result = SimulationCompiler.Compile(level, build);

    TracePrinter.Print(level, build, result);

    // Exit code: 0 solved, 3 failed goal, 2 paradox.
    return result.Outcome switch
    {
        LevelOutcome.Solved => 0,
        LevelOutcome.Paradox => 2,
        _ => 3,
    };
}
