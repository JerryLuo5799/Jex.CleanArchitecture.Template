using System.CommandLine;
using Jex.Cli.Project;

namespace Jex.Cli.Commands;

/// <summary>Build, test and run commands of a generated solution (found by walking up to jex.json).</summary>
internal static class ProjectCommands
{
    public static IEnumerable<Command> Create(CliContext context) =>
        [Build(context), Format(context), Test(context), Check(context), Run(context), Watch(context), Init(context)];

    private static Command Build(CliContext context) =>
        new Command("build", "Build the solution.").Handle(context, _ =>
        {
            var project = context.Project();
            context.Run(project.Root, "dotnet", ["build", project.SolutionFile]);
        });

    private static Command Format(CliContext context)
    {
        var check = new Option<bool>("--check") { Description = "Verify formatting without changing files (dotnet format --verify-no-changes)." };
        return new Command("format", "Apply code formatting to the solution.") { check }.Handle(context, parseResult =>
        {
            var project = context.Project();
            context.Run(project.Root, "dotnet", parseResult.GetValue(check)
                ? ["format", project.SolutionFile, "--verify-no-changes"]
                : ["format", project.SolutionFile]);
        });
    }

    private static Command Test(CliContext context)
    {
        var suite = new Argument<string?>("suite")
        {
            Description = "Run only one test project: unit, integration or arch. Default: the whole solution.",
            Arity = ArgumentArity.ZeroOrOne,
        };
        suite.AcceptOnlyFromAmong("unit", "integration", "arch");
        return new Command("test", "Run the tests.") { suite }.Handle(context, parseResult =>
        {
            var project = context.Project();
            var selected = parseResult.GetValue(suite);
            context.Run(project.Root, "dotnet", selected is null
                ? ["test", "--solution", project.SolutionFile]
                : ["test", "--project", project.ResolveTestProject(selected)]);
        });
    }

    private static Command Check(CliContext context)
    {
        var ci = new Option<bool>("--ci")
        {
            Description = "Same as CI: verify formatting, Release build with warnings as errors, tests without rebuilding.",
        };
        return new Command("check", "The acceptance gate: format, build, test. Stops at the first failing step.") { ci }
            .Handle(context, parseResult => CheckRunner.Run(context, context.Project(), parseResult.GetValue(ci)));
    }

    private static Command Run(CliContext context)
    {
        var aspire = new Option<bool>("--aspire") { Description = "Run the .NET Aspire AppHost instead of the API." };
        var api = CommandExtensions.ApiProjectOption();
        return new Command("run", "Run the API (Development), or the Aspire AppHost with --aspire.") { aspire, api }.Handle(context, parseResult =>
        {
            var project = context.Project();
            var target = parseResult.GetValue(aspire)
                ? project.ResolveAppHostProject()
                : project.ResolveApiProject(parseResult.GetValue(api));
            context.Run(project.Root, "dotnet", ["run", "--project", target]);
        });
    }

    private static Command Watch(CliContext context)
    {
        var api = CommandExtensions.ApiProjectOption();
        return new Command("watch", "Run the API with hot reload.") { api }.Handle(context, parseResult =>
        {
            var project = context.Project();
            context.Run(project.Root, "dotnet", ["watch", "--project", project.ResolveApiProject(parseResult.GetValue(api))]);
        });
    }

    private static Command Init(CliContext context) =>
        new Command("init", "Mark the current directory (a solution generated before jex existed) as a jex project by writing jex.json.")
            .Handle(context, _ =>
            {
                var directory = context.CurrentDirectory;
                var manifestPath = Path.Combine(directory, JexManifest.FileName);
                if (File.Exists(manifestPath))
                {
                    throw new JexException($"{manifestPath} already exists.");
                }

                var solutions = Directory.GetFiles(directory, "*.slnx");
                if (solutions.Length != 1)
                {
                    throw new JexException($"Run 'jex init' in the solution root: expected exactly one *.slnx in {directory}, found {solutions.Length}.");
                }

                // The version this project was generated with is unknown; scaffold then asks for --template-version.
                new JexManifest(JexManifest.CurrentSchemaVersion, JexManifest.TemplateName, null).Save(manifestPath);
                context.Out.WriteLine($"Wrote {manifestPath}. Scaffolding commands will ask for --template-version until you record it in templateVersion.");
            });
}
