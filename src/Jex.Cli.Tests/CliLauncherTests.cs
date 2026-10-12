using Jex.Cli.Process;

namespace Jex.Cli.Tests;

public sealed class CliLauncherTests
{
    private static readonly string s_dll = Path.Combine(Path.GetTempPath(), "Program Files", "jex cli", "Jex.Cli.dll");

    [Theory]
    [InlineData("dotnet.exe")]
    [InlineData("dotnet")]
    [InlineData("DOTNET.EXE")]
    public void Dotnet_host_needs_the_dll_as_first_argument(string host)
    {
        var dotnet = Path.Combine(Path.GetTempPath(), "Program Files", "dotnet", host);

        var launch = CliLauncher.Resolve(dotnet, s_dll);

        launch.Executable.ShouldBe(dotnet);
        launch.PrefixArguments.ShouldBe([s_dll]);
    }

    [Theory]
    [InlineData("jex.exe")]
    [InlineData("jex")]
    [InlineData("Jex.Cli.exe")]
    public void Apphost_or_tool_shim_starts_directly(string executable)
    {
        var path = Path.Combine(Path.GetTempPath(), "with space", ".dotnet", "tools", executable);

        var launch = CliLauncher.Resolve(path, s_dll);

        launch.Executable.ShouldBe(path);
        launch.PrefixArguments.ShouldBeEmpty();
    }

    [Fact]
    public void Unknown_process_path_falls_back_to_dotnet()
    {
        var launch = CliLauncher.Resolve(null, s_dll);

        launch.Executable.ShouldBe("dotnet");
        launch.PrefixArguments.ShouldBe([s_dll]);
    }

    [Fact]
    public void Paths_with_spaces_reach_the_matrix_as_single_arguments()
    {
        using var repo = new TempDirectory();
        repo.Write("Jex.CleanArchitecture.Template.csproj", "<Project />");
        var dotnet = Path.Combine(Path.GetTempPath(), "Program Files", "dotnet", "dotnet.exe");
        var harness = new CliHarness(repo.Path) { Self = CliLauncher.Resolve(dotnet, s_dll) };

        harness.Run("dev", "matrix", "--cells", "bare").ShouldBe(0);

        var arguments = harness.Runner.Invocations.Single().Arguments;
        arguments.ShouldContain(dotnet);
        arguments.ShouldContain(s_dll);
        arguments.SkipWhile(a => a != "--jex").ShouldBe(["--jex", dotnet, "--jex-arg", s_dll]);
    }
}
