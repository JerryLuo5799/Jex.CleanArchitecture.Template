using System.CommandLine;
using Jex.Cli.Process;
using Jex.Cli.Project;
using Jex.Cli.Templates;

namespace Jex.Cli.Commands;

/// <summary>Tasks for developing the template repository itself (found by walking up to the template package project).</summary>
internal static class DevCommands
{
    private const string DevVersion = "0.0.0-dev";
    private const string CliPackageId = "Jex.Cli";

    public static Command Create(CliContext context) =>
        new Command("dev", "Develop the Jex template repository: install templates from source, generate sandboxes, run the matrix.")
        {
            Install(context),
            Uninstall(context),
            Pack(context),
            New(context),
            Matrix(context),
            InstallCli(context),
            Check(context),
            Clean(context),
        };

    /// <summary>True when running from the global tool store (~/.dotnet/tools/.store/jex.cli/...).</summary>
    public static bool IsGlobalToolInstall(string baseDirectory)
    {
        var normalized = baseDirectory.Replace('\\', '/');
        return normalized.Contains("/.store/jex.cli/", StringComparison.OrdinalIgnoreCase);
    }

    private static Command Install(CliContext context)
    {
        var fromPackage = new Option<bool>("--from-package") { Description = "Pack the template package and install the nupkg (release acceptance path)." };
        var version = new Option<string>("--version") { Description = "Package version for --from-package.", DefaultValueFactory = _ => DevVersion };
        return new Command("install", "Install the templates from this checkout (dev loop, no pack needed).") { fromPackage, version }.Handle(context, parseResult =>
        {
            var repo = context.Repo();
            var installer = new TemplateInstaller(context);
            string source;
            if (parseResult.GetValue(fromPackage))
            {
                var packageVersion = SemVer.Parse(parseResult.GetValue(version)!).ToString();
                context.Run(repo.Root, "dotnet", ["pack", RepoContext.MarkerFile, "-c", "Release", $"-p:Version={packageVersion}", "-o", repo.ArtifactsDirectory]);
                source = Path.Combine(repo.ArtifactsDirectory, $"{TemplatePolicy.PackageId}.{packageVersion}.nupkg");
            }
            else
            {
                source = repo.TemplatesDirectory;
            }

            // Two installs of the same templates make 'dotnet new jex' ambiguous: remove the other one first.
            foreach (var install in installer.ListInstalls())
            {
                context.RunAllowingFailure(repo.Root, "dotnet", ["new", "uninstall", install.Source]);
            }

            context.Run(repo.Root, "dotnet", ["new", "install", source, "--force"]);
            context.Run(repo.Root, "dotnet", ["new", "list", TemplateInstaller.SolutionShortName]);
        });
    }

    private static Command Uninstall(CliContext context) =>
        new Command("uninstall", "Remove the Jex templates (directory and package installs).").Handle(context, _ =>
        {
            var installs = new TemplateInstaller(context).ListInstalls();
            if (installs.Count == 0)
            {
                context.Out.WriteLine("The Jex templates are not installed.");
            }

            foreach (var install in installs)
            {
                context.RunAllowingFailure(context.CurrentDirectory, "dotnet", ["new", "uninstall", install.Source]);
            }
        });

    private static Command Pack(CliContext context)
    {
        var version = new Option<string>("--version") { Description = "Version of both packages.", DefaultValueFactory = _ => DevVersion };
        var output = new Option<string>("--output", "-o") { Description = "Output directory.", DefaultValueFactory = _ => "artifacts" };
        return new Command("pack", "Pack the template package and the Jex.Cli tool package with the same version.") { version, output }.Handle(context, parseResult =>
        {
            var repo = context.Repo();
            var packageVersion = SemVer.Parse(parseResult.GetValue(version)!).ToString();
            var outputDirectory = Path.GetFullPath(parseResult.GetValue(output)!, repo.Root);
            context.Run(repo.Root, "dotnet", ["pack", RepoContext.SolutionFile, "-c", "Release", $"-p:Version={packageVersion}", "-o", outputDirectory]);
        });
    }

    private static Command New(CliContext context)
    {
        var name = new Argument<string>("name") { Description = "Project name (artifacts/sandbox/<name>), e.g. Acme, Acme.Shop, my-api." };
        var database = new Option<string>("--database") { Description = "Database provider.", DefaultValueFactory = _ => "sqlite" };
        database.AcceptOnlyFromAmong("sqlite", "postgresql", "sqlserver", "mysql");
        var aspire = new Option<bool>("--aspire") { Description = "--use-aspire.", DefaultValueFactory = _ => false };
        var ai = new Option<bool>("--ai") { Description = "--include-ai-docs.", DefaultValueFactory = _ => true };
        var docker = new Option<bool>("--docker") { Description = "--include-docker.", DefaultValueFactory = _ => true };
        var check = new Option<bool>("--check") { Description = "Then run the strict gate (check --ci) in the sandbox, in this process." };

        return new Command("new", "Regenerate artifacts/sandbox/<name> from the installed templates and assert its structure.")
        {
            name, database, aspire, ai, docker, check,
        }.Handle(context, parseResult =>
        {
            var repo = context.Repo();
            var projectName = parseResult.GetValue(name)!;
            var target = repo.ResolveSandbox(projectName);
            var db = parseResult.GetValue(database)!;
            var flags = new[] { parseResult.GetValue(aspire), parseResult.GetValue(ai), parseResult.GetValue(docker) }.Select(CommandExtensions.Bool).ToArray();

            if (Directory.Exists(target))
            {
                context.Out.WriteLine($"Deleting {Path.GetRelativePath(repo.Root, target)}");
                FileSystem.DeleteDirectory(target);
            }

            context.Run(repo.Root, "dotnet", ["new", TemplateInstaller.SolutionShortName, "-n", projectName, "-o", target, "--database", db,
                "--use-aspire", flags[0], "--include-ai-docs", flags[1], "--include-docker", flags[2]]);
            context.Run(repo.Root, "dotnet", ["run", "--file", Path.Combine(repo.ScriptsDirectory, "assert-structure.cs"), "--",
                target, projectName, db, flags[0], flags[1], flags[2]]);

            if (parseResult.GetValue(check))
            {
                CheckRunner.Run(context, ProjectContext.Discover(target), ci: true);
            }
        });
    }

    private static Command Matrix(CliContext context)
    {
        var cells = new Option<string?>("--cells") { Description = "Comma-separated cells (sqlite, postgresql, sqlserver, mysql, aspire, bare). Default: all." };
        var package = new Option<string?>("--package") { Description = "Test this template nupkg instead of ./templates." };
        var docker = new Option<bool>("--docker") { Description = "Also docker build the cells that have a Dockerfile." };
        var jex = new Option<string?>("--jex") { Description = "Executable of the jex CLI under test. Default: this jex." };
        var jexArguments = new Option<string[]>("--jex-arg")
        {
            Description = "Argument placed before every jex command (repeatable), e.g. the jex.dll path when --jex is dotnet.",
        };

        return new Command("matrix", "Generate, assert, scaffold (with jex) and check every supported template combination.")
        {
            cells, package, docker, jex, jexArguments,
        }.Handle(context, parseResult =>
        {
            var repo = context.Repo();
            var prefix = parseResult.GetValue(jexArguments) ?? [];
            var launch = parseResult.GetValue(jex) is { } executable
                ? new CliLaunch(executable, prefix)
                : prefix.Length == 0 ? context.Self : throw new JexException("--jex-arg needs --jex.");

            List<string> arguments = ["run", "--file", Path.Combine(repo.ScriptsDirectory, "test-template-matrix.cs"), "--"];
            if (parseResult.GetValue(cells) is { } selected)
            {
                arguments.AddRange(["--cells", selected]);
            }

            if (parseResult.GetValue(package) is { } nupkg)
            {
                arguments.AddRange(["--package", Path.GetFullPath(nupkg, context.CurrentDirectory)]);
            }

            if (parseResult.GetValue(docker))
            {
                arguments.Add("--docker");
            }

            arguments.AddRange(["--jex", launch.Executable]);
            foreach (var argument in launch.PrefixArguments)
            {
                arguments.AddRange(["--jex-arg", argument]);
            }

            context.Run(repo.Root, "dotnet", arguments);
        });
    }

    private static Command InstallCli(CliContext context)
    {
        var output = new Option<string>("--output", "-o") { Description = "Directory for the packed tool.", DefaultValueFactory = _ => "artifacts" };
        return new Command("install-cli", $"Pack this checkout's CLI as {DevVersion} and (re)install it as the global 'jex' tool. Run it with 'dotnet run --project src/Jex.Cli -- dev install-cli'.")
        {
            output,
        }.Handle(context, parseResult =>
        {
            if (IsGlobalToolInstall(context.BaseDirectory))
            {
                throw new JexException(
                    "The global jex cannot replace itself (the running tool is locked on Windows). Run 'dotnet run --project src/Jex.Cli -- dev install-cli' from the repository instead.");
            }

            var repo = context.Repo();
            var outputDirectory = Path.GetFullPath(parseResult.GetValue(output)!, repo.Root);
            context.Run(repo.Root, "dotnet", ["pack", Path.Combine("src", "Jex.Cli", "Jex.Cli.csproj"), "-c", "Release", $"-p:Version={DevVersion}", "-o", outputDirectory]);
            context.RunAllowingFailure(repo.Root, "dotnet", ["tool", "uninstall", "-g", CliPackageId]);

            // Same version number every time: without this the install would reuse the previously extracted package.
            var cached = Path.Combine(context.NuGetPackagesDirectory, CliPackageId.ToLowerInvariant(), DevVersion);
            if (Directory.Exists(cached))
            {
                context.Out.WriteLine($"Deleting cached package {cached}");
                FileSystem.DeleteDirectory(cached);
            }

            context.Run(repo.Root, "dotnet", ["tool", "install", "-g", CliPackageId, "--source", outputDirectory, "--version", DevVersion]);
        });
    }

    private static Command Check(CliContext context)
    {
        var ci = new Option<bool>("--ci") { Description = "Same as CI: verify formatting, warnings as errors." };
        return new Command("check", "Format, build and test the template source (templates/solution), in this process.") { ci }
            .Handle(context, parseResult => CheckRunner.Run(context, ProjectContext.Discover(context.Repo().TemplateSolutionDirectory), parseResult.GetValue(ci)));
    }

    private static Command Clean(CliContext context) =>
        new Command("clean", "Delete bin/obj under templates/ and the artifacts/ directory.").Handle(context, _ =>
        {
            var repo = context.Repo();
            var directories = Directory.EnumerateDirectories(repo.TemplatesDirectory, "*", SearchOption.AllDirectories)
                .Where(d => Path.GetFileName(d) is "bin" or "obj")
                .ToList();
            foreach (var directory in directories.Where(Directory.Exists))
            {
                FileSystem.DeleteDirectory(directory);
            }

            FileSystem.DeleteDirectory(repo.ArtifactsDirectory);
            context.Out.WriteLine($"Removed {directories.Count} bin/obj directories and {Path.GetRelativePath(repo.Root, repo.ArtifactsDirectory)}/.");
        });
}
