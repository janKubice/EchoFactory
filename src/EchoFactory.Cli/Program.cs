using EchoFactory.Cli;

var (command, positionals, dataDir, options) = CliArgs.Parse(args);

return command switch
{
    null or "help" or "-h" or "--help" => Help(),
    "demo" => Commands.Demo(),
    "validate" => Commands.Validate(dataDir),
    "list" => Commands.List(dataDir),
    "run" => Commands.Run(dataDir, positionals),
    "verify" => Commands.Verify(dataDir, positionals, options),
    "bench" => Commands.Bench(dataDir),
    _ => Unknown(command),
};

static int Help()
{
    Console.WriteLine("EchoFactory headless CLI");
    Console.WriteLine();
    Console.WriteLine("Usage: echofactory <command> [--data <dir>]   (default --data ./data)");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  demo                          Compile the built-in line example and print the timeline.");
    Console.WriteLine("  list                          List the node/level/solution ids in the data dir.");
    Console.WriteLine("  validate                      Load & validate all node/level/solution JSON in the data dir.");
    Console.WriteLine("  run <level_id> <solution_id>  Compile a JSON level + solution and print the timeline.");
    Console.WriteLine("  verify <level_id> <sol_id> [--ticks N] [--footprint M]");
    Console.WriteLine("                                Anti-cheat: re-simulate and accept only if it solves");
    Console.WriteLine("                                (and matches the claimed metrics, if given).");
    Console.WriteLine("  bench                         Time compilation of every reference solution.");
    Console.WriteLine("  help                          Show this help.");
    return 0;
}

static int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown command: '{command}'. Try 'help'.");
    return 1;
}
