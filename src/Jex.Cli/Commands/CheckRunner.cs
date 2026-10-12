using Jex.Cli.Project;

namespace Jex.Cli.Commands;

/// <summary>The acceptance gate, shared by <c>jex check</c>, <c>jex dev check</c> and <c>jex dev new --check</c> (in process).</summary>
public static class CheckRunner
{
    public static void Run(CliContext context, ProjectContext project, bool ci)
    {
        var solution = project.SolutionFile;
        if (ci)
        {
            context.Run(project.Root, "dotnet", ["format", solution, "--verify-no-changes"]);
            context.Run(project.Root, "dotnet", ["build", solution, "-c", "Release", "-p:ContinuousIntegrationBuild=true"]);
            context.Run(project.Root, "dotnet", ["test", "--solution", solution, "-c", "Release", "--no-build"]);
        }
        else
        {
            context.Run(project.Root, "dotnet", ["format", solution]);
            context.Run(project.Root, "dotnet", ["build", solution]);
            context.Run(project.Root, "dotnet", ["test", "--solution", solution]);
        }
    }
}
