using System.CommandLine;
using Jex.Cli.Project;
using Jex.Cli.Templates;

namespace Jex.Cli.Commands;

/// <summary>Wraps the jex-entity / jex-usecase / jex-endpoint item templates, run from the solution root.</summary>
internal static class ScaffoldCommands
{
    public static Command Create(CliContext context) =>
        new Command("scaffold", "Generate code with the item templates. Existing files are never overwritten (the command fails instead).")
        {
            Entity(context),
            UseCase(context),
            Endpoint(context),
            Feature(context),
        };

    public static List<string> EntityArguments(string entity, string plural, string prefix, bool noTest) =>
        WithTest(["new", "jex-entity", "-n", entity, "--plural", plural, "--solution-name", prefix, "-o", "."], noTest);

    public static List<string> UseCaseArguments(
        string useCase, string feature, string type, string returnType, bool withId, bool paged, string prefix, bool noTest)
    {
        List<string> arguments = ["new", "jex-usecase", "-n", useCase, "--feature", feature, "--usecase-type", type, "--return-type", returnType];
        if (withId)
        {
            arguments.Add("--with-id");
        }

        if (paged)
        {
            arguments.Add("--paged");
        }

        arguments.AddRange(["--solution-name", prefix, "-o", "."]);
        return WithTest(arguments, noTest);
    }

    public static List<string> EndpointArguments(string feature, string? entity, string prefix, bool noTest)
    {
        List<string> arguments = ["new", "jex-endpoint", "-n", feature];
        if (!string.IsNullOrWhiteSpace(entity))
        {
            arguments.AddRange(["--entity", entity]);
        }

        arguments.AddRange(["--solution-name", prefix, "-o", "."]);
        return WithTest(arguments, noTest);
    }

    private static List<string> WithTest(List<string> arguments, bool noTest)
    {
        if (noTest)
        {
            arguments.AddRange(["--include-test", "false"]);
        }

        return arguments;
    }

    private static Command Entity(CliContext context)
    {
        var entity = new Argument<string>("entity") { Description = "Entity name, e.g. Order." };
        var plural = new Argument<string>("plural") { Description = "Plural / feature folder, e.g. Orders." };
        var common = new CommonOptions();
        var command = new Command("entity", "Entity, repository, FreeSql configuration and DTO.") { entity, plural };
        common.AddTo(command);
        return command.Handle(context, parseResult =>
        {
            var (project, prefix, noTest) = common.Prepare(context, parseResult);
            context.Run(project.Root, "dotnet", EntityArguments(parseResult.GetValue(entity)!, parseResult.GetValue(plural)!, prefix, noTest));
        });
    }

    private static Command UseCase(CliContext context)
    {
        var useCase = new Argument<string>("usecase") { Description = "Use case name, e.g. CreateOrder or GetOrdersWithPagination." };
        var feature = new Argument<string>("feature") { Description = "Feature folder, e.g. Orders." };
        var type = new Option<string>("--type") { Description = "command (write) or query (read).", DefaultValueFactory = _ => "command" };
        type.AcceptOnlyFromAmong("command", "query");
        var returnType = new Option<string>("--return") { Description = "Return type, e.g. long, OrderDto, List<OrderDto>.", DefaultValueFactory = _ => "Unit" };
        var withId = new Option<bool>("--with-id") { Description = "Request carries a long Id (get/update/delete by id)." };
        var paged = new Option<bool>("--paged") { Description = "Paged query (PaginatedList<T>; requires --type query)." };
        var common = new CommonOptions();
        var command = new Command("usecase", "Command or query with handler, validator and unit test.") { useCase, feature, type, returnType, withId, paged };
        common.AddTo(command);
        return command.Handle(context, parseResult =>
        {
            var (project, prefix, noTest) = common.Prepare(context, parseResult);
            context.Run(project.Root, "dotnet", UseCaseArguments(
                parseResult.GetValue(useCase)!,
                parseResult.GetValue(feature)!,
                parseResult.GetValue(type)!,
                parseResult.GetValue(returnType)!,
                parseResult.GetValue(withId),
                parseResult.GetValue(paged),
                prefix,
                noTest));
        });
    }

    private static Command Endpoint(CliContext context)
    {
        var feature = new Argument<string>("feature") { Description = "Feature / route group, e.g. Orders." };
        var entity = new Option<string?>("--entity") { Description = "Wire the CRUD use cases of this entity (scaffold them first)." };
        var common = new CommonOptions();
        var command = new Command("endpoint", "Minimal API endpoint group and integration test.") { feature, entity };
        common.AddTo(command);
        return command.Handle(context, parseResult =>
        {
            var (project, prefix, noTest) = common.Prepare(context, parseResult);
            context.Run(project.Root, "dotnet", EndpointArguments(parseResult.GetValue(feature)!, parseResult.GetValue(entity), prefix, noTest));
        });
    }

    private static Command Feature(CliContext context)
    {
        var entity = new Argument<string>("entity") { Description = "Entity name, e.g. Order." };
        var plural = new Argument<string>("plural") { Description = "Plural / feature folder, e.g. Orders." };
        var common = new CommonOptions();
        var command = new Command("feature", "Entity + Create<Entity> command + Get<Entity>ById query + endpoint group, in that order.") { entity, plural };
        common.AddTo(command);
        return command.Handle(context, parseResult =>
        {
            var (project, prefix, noTest) = common.Prepare(context, parseResult);
            var name = parseResult.GetValue(entity)!;
            var feature = parseResult.GetValue(plural)!;
            context.Run(project.Root, "dotnet", EntityArguments(name, feature, prefix, noTest));
            context.Run(project.Root, "dotnet", UseCaseArguments($"Create{name}", feature, "command", "long", withId: false, paged: false, prefix, noTest));
            context.Run(project.Root, "dotnet", UseCaseArguments($"Get{name}ById", feature, "query", $"{name}Dto", withId: true, paged: false, prefix, noTest));
            context.Run(project.Root, "dotnet", EndpointArguments(feature, name, prefix, noTest));
        });
    }

    private sealed class CommonOptions
    {
        private readonly Option<string?> _templateVersion = new("--template-version")
        {
            Description = "Template version to require instead of jex.json's templateVersion.",
        };

        private readonly Option<bool> _force = new("--force") { Description = "Skip the template version check." };

        private readonly Option<string?> _solutionName = new("--solution-name")
        {
            Description = "Project name prefix (src/<Prefix>.Domain ...). Default: inferred from the *.WebAPI project.",
        };

        private readonly Option<string?> _apiProject = CommandExtensions.ApiProjectOption();

        private readonly Option<bool> _noTest = new("--no-test") { Description = "Do not generate the test file." };

        public void AddTo(Command command)
        {
            command.Options.Add(_templateVersion);
            command.Options.Add(_force);
            command.Options.Add(_solutionName);
            command.Options.Add(_apiProject);
            command.Options.Add(_noTest);
        }

        public (ProjectContext Project, string Prefix, bool NoTest) Prepare(CliContext context, ParseResult parseResult)
        {
            var project = context.Project();
            var prefix = project.ResolveScaffoldPrefix(parseResult.GetValue(_solutionName), parseResult.GetValue(_apiProject));
            var requested = parseResult.GetValue(_templateVersion) is { } version ? SemVer.Parse(version) : null;
            new TemplateInstaller(context).CheckForScaffold(project.Manifest.TemplateVersion, requested, parseResult.GetValue(_force));
            return (project, prefix, parseResult.GetValue(_noTest));
        }
    }
}
