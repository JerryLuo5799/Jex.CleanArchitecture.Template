using System.CommandLine;
using Jex.Cli.Project;

namespace Jex.Cli.Commands;

internal static class InfraCommands
{
    public static Command Create(CliContext context) =>
        new Command("infra", "Local database containers (docker compose profile of the configured provider).")
        {
            Step(context, "up", "Start the database container and wait until it is healthy.", ["up", "-d", "--wait"]),
            Step(context, "down", "Stop the database container and remove its volumes.", ["down", "-v"]),
        };

    private static Command Step(CliContext context, string name, string description, string[] composeArguments)
    {
        var profile = new Option<string?>("--profile")
        {
            Description = "docker compose profile. Default: derived from Database:Provider in the API's appsettings.json.",
        };
        profile.AcceptOnlyFromAmong(DatabaseProfile.Profiles);
        var api = CommandExtensions.ApiProjectOption();

        return new Command(name, description) { profile, api }.Handle(context, parseResult =>
        {
            var project = context.Project();
            var selected = parseResult.GetValue(profile)
                ?? DatabaseProfile.Resolve(project.Root, project.ResolveApiProject(parseResult.GetValue(api)));
            if (selected == DatabaseProfile.None)
            {
                context.Out.WriteLine("SQLite needs no infrastructure (database profile 'none').");
                return;
            }

            if (!project.HasDocker)
            {
                throw new JexException($"No docker-compose.yml in {project.Root} (the project was generated with --include-docker false).");
            }

            context.Run(project.Root, "docker", ["compose", "--profile", selected, .. composeArguments]);
        });
    }
}
