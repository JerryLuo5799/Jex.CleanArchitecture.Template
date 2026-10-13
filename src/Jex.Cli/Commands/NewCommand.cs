using System.CommandLine;
using Jex.Cli.Templates;

namespace Jex.Cli.Commands;

internal static class NewCommand
{
    public static Command Create(CliContext context)
    {
        var name = new Argument<string>("name") { Description = "Project name, e.g. Acme.Shop (also the root namespace)." };
        var output = new Option<string?>("--output", "-o") { Description = "Output directory. Default: ./<name>." };
        var database = new Option<string?>("--database", "-db") { Description = "Database provider: sqlite (default), postgresql, sqlserver, mysql." };
        database.AcceptOnlyFromAmong("sqlite", "postgresql", "sqlserver", "mysql");
        var useAspire = new Option<bool>("--use-aspire") { Description = "Add a .NET Aspire AppHost project." };
        var aiDocs = new Option<bool>("--include-ai-docs") { Description = "Include AGENTS.md, CLAUDE.md, skills and Copilot instructions (default true)." };
        var docker = new Option<bool>("--include-docker") { Description = "Include Dockerfile and docker-compose.yml (default true)." };
        var httpPort = new Option<int?>("--http-port") { Description = "HTTP port in launchSettings.json (random if omitted)." };
        var httpsPort = new Option<int?>("--https-port") { Description = "HTTPS port in launchSettings.json (random if omitted)." };
        var skipRestore = new Option<bool>("--skip-restore") { Description = "Do not restore packages after generating." };
        var templateVersion = new Option<string?>("--template-version")
        {
            Description = "Template package version to use (installed if needed). Default: the version of this jex.",
        };
        var noInstall = new Option<bool>("--no-install") { Description = "Never install or switch the template package; fail instead." };

        var command = new Command("new", "Create a solution from the Jex template (installs the matching template package if needed). Other options are passed to 'dotnet new jex'.")
        {
            name, output, database, useAspire, aiDocs, docker, httpPort, httpsPort, skipRestore, templateVersion, noInstall,
        };
        command.TreatUnmatchedTokensAsErrors = false;

        return command.Handle(context, parseResult =>
        {
            var requested = parseResult.GetValue(templateVersion) is { } version ? SemVer.Parse(version) : null;
            new TemplateInstaller(context).EnsureForNew(requested, parseResult.GetValue(noInstall));

            var projectName = parseResult.GetValue(name)!;
            List<string> arguments = ["new", TemplateInstaller.SolutionShortName, "-n", projectName];
            var outputDirectory = parseResult.GetValue(output);
            if (outputDirectory is not null)
            {
                arguments.AddRange(["-o", outputDirectory]);
            }

            if (parseResult.GetValue(database) is { } db)
            {
                arguments.AddRange(["--database", db]);
            }

            AddBool(useAspire, "--use-aspire");
            AddBool(aiDocs, "--include-ai-docs");
            AddBool(docker, "--include-docker");
            AddBool(skipRestore, "--skip-restore");
            if (parseResult.GetValue(httpPort) is { } http)
            {
                arguments.AddRange(["--http-port", http.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            }

            if (parseResult.GetValue(httpsPort) is { } https)
            {
                arguments.AddRange(["--https-port", https.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            }

            arguments.AddRange(parseResult.UnmatchedTokens);
            context.Run(context.CurrentDirectory, "dotnet", arguments);

            var created = Path.GetRelativePath(context.CurrentDirectory, Path.GetFullPath(outputDirectory ?? projectName, context.CurrentDirectory));
            context.Out.WriteLine();
            context.Out.WriteLine("Next:");
            context.Out.WriteLine($"  cd {(created.Contains(' ', StringComparison.Ordinal) ? $"\"{created}\"" : created)}");
            context.Out.WriteLine("  jex check");

            void AddBool(Option<bool> option, string flag)
            {
                if (parseResult.GetResult(option) is { Implicit: false })
                {
                    arguments.AddRange([flag, CommandExtensions.Bool(parseResult.GetValue(option))]);
                }
            }
        });
    }
}
