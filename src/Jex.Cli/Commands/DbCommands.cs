using System.CommandLine;

namespace Jex.Cli.Commands;

internal static class DbCommands
{
    public static Command Create(CliContext context) =>
        new Command("db", "Database: migration scripts, applying migrations, resetting the local SQLite file.")
        {
            Reset(context),
            Script(context),
            Migrate(context),
        };

    private static Command Reset(CliContext context)
    {
        var api = CommandExtensions.ApiProjectOption();
        return new Command("reset", "Delete the local SQLite database files next to the API (recreated with seed data on the next run).") { api }
            .Handle(context, parseResult =>
            {
                var project = context.Project();
                var apiDirectory = Path.GetDirectoryName(Path.Combine(project.Root, project.ResolveApiProject(parseResult.GetValue(api))))!;
                var files = Directory.EnumerateFiles(apiDirectory, "*.db")
                    .Concat(Directory.EnumerateFiles(apiDirectory, "*.db-*"))
                    .ToList();
                foreach (var file in files)
                {
                    File.Delete(file);
                    context.Out.WriteLine($"Deleted {Path.GetRelativePath(project.Root, file)}");
                }

                if (files.Count == 0)
                {
                    context.Out.WriteLine($"No SQLite database files in {Path.GetRelativePath(project.Root, apiDirectory)}.");
                }
            });
    }

    private static Command Script(CliContext context)
    {
        var name = new Argument<string>("name")
        {
            Description = "Migration name, e.g. add-orders.",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => "update",
        };
        var directory = new Argument<string?>("dir")
        {
            Description = "Output directory relative to the solution root. Default: db/migrations.",
            Arity = ArgumentArity.ZeroOrOne,
        };
        var api = CommandExtensions.ApiProjectOption();
        return new Command("script", "Generate the next migration script from the entity model (db/migrations/NNNN_<name>.sql).") { name, directory, api }
            .Handle(context, parseResult =>
            {
                var project = context.Project();
                List<string> arguments = ["run", "--project", project.ResolveApiProject(parseResult.GetValue(api)), "--no-launch-profile", "--", "db-script", parseResult.GetValue(name)!];
                if (parseResult.GetValue(directory) is { } output)
                {
                    arguments.Add(output);
                }

                context.Run(project.Root, "dotnet", arguments);
            });
    }

    private static Command Migrate(CliContext context)
    {
        var dryRun = new Option<bool>("--dry-run") { Description = "List pending migrations without applying them." };
        var api = CommandExtensions.ApiProjectOption();
        return new Command("migrate", "Apply pending migrations to the configured database (builds the API first).") { dryRun, api }
            .Handle(context, parseResult =>
            {
                var project = context.Project();
                var apiProject = project.ResolveApiProject(parseResult.GetValue(api));
                context.Run(project.Root, "dotnet", ["build", apiProject]);
                List<string> arguments = ["run", "--project", apiProject, "--no-build", "--no-launch-profile", "--", "migrate"];
                if (parseResult.GetValue(dryRun))
                {
                    arguments.Add("--dry-run");
                }

                context.Run(project.Root, "dotnet", arguments);
            });
    }
}
