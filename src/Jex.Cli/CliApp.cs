using System.CommandLine;
using System.CommandLine.Invocation;
using Jex.Cli.Commands;

namespace Jex.Cli;

public static class CliApp
{
    public static RootCommand Build(CliContext context)
    {
        var root = new RootCommand("jex - create, scaffold, build and run Jex Clean Architecture solutions.");

        // Print the parsed CLI version (no +commit suffix) so it always equals the installed package version.
        foreach (var version in root.Options.OfType<VersionOption>())
        {
            version.Action = new PrintVersionAction(context);
        }

        foreach (var command in ProjectCommands.Create(context))
        {
            root.Subcommands.Add(command);
        }

        root.Subcommands.Add(InfraCommands.Create(context));
        root.Subcommands.Add(DbCommands.Create(context));
        root.Subcommands.Add(ScaffoldCommands.Create(context));
        root.Subcommands.Add(NewCommand.Create(context));
        root.Subcommands.Add(DevCommands.Create(context));
        return root;
    }

    public static int Run(string[] args, CliContext context) =>
        Build(context).Parse(args).Invoke(new InvocationConfiguration { Output = context.Out, Error = context.Error });

    private sealed class PrintVersionAction(CliContext context) : SynchronousCommandLineAction
    {
        public override int Invoke(ParseResult parseResult)
        {
            context.Out.WriteLine(context.Version);
            return 0;
        }
    }
}
