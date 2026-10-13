using System.CommandLine;

namespace Jex.Cli.Tests;

public sealed class ParserTests
{
    private static readonly RootCommand s_root = CliApp.Build(new CliHarness(Path.GetTempPath()).Context);

    [Theory]
    [InlineData("check --ci")]
    [InlineData("test unit")]
    [InlineData("run --aspire")]
    [InlineData("infra up --profile postgresql")]
    [InlineData("db script add-orders")]
    [InlineData("db migrate --dry-run")]
    [InlineData("scaffold usecase GetOrders Orders --type query --return List<OrderDto> --paged --no-test")]
    [InlineData("scaffold feature Order Orders --template-version 2.0.0")]
    [InlineData("new Acme --database postgresql --use-aspire --include-ai-docs false --whatever")]
    [InlineData("dev new my-api --database mysql --ai false --docker false --check")]
    [InlineData("dev matrix --cells sqlite,bare --jex dotnet --jex-arg a.dll --jex-arg b")]
    [InlineData("dev install --from-package --version 2.1.0-rc.1")]
    public void Valid_command_lines_parse(string commandLine) =>
        s_root.Parse(commandLine.Split(' ')).Errors.ShouldBeEmpty();

    [Theory]
    [InlineData("test e2e")]
    [InlineData("scaffold usecase X Y --type event")]
    [InlineData("scaffold entity Order")]
    [InlineData("infra up --profile oracle")]
    [InlineData("check --fast")]
    [InlineData("db script a b c")]
    [InlineData("dev new Acme --database oracle")]
    [InlineData("new")]
    public void Invalid_command_lines_report_errors(string commandLine) =>
        s_root.Parse(commandLine.Split(' ')).Errors.ShouldNotBeEmpty();

    [Fact]
    public void Version_option_prints_the_cli_version_without_build_metadata()
    {
        var harness = new CliHarness(Path.GetTempPath()) { Version = Jex.Cli.Templates.SemVer.Parse("2.1.0-rc.1+abc123") };

        harness.Run("--version").ShouldBe(0);

        harness.Out.ToString().Trim().ShouldBe("2.1.0-rc.1");
    }

    [Fact]
    public void Global_tool_store_is_detected()
    {
        Commands.DevCommands.IsGlobalToolInstall("/home/u/.dotnet/tools/.store/jex.cli/2.0.0/jex.cli/2.0.0/tools/net10.0/any/").ShouldBeTrue();
        Commands.DevCommands.IsGlobalToolInstall(@"C:\Users\u\.dotnet\tools\.store\jex.cli\0.0.0-dev\jex.cli\0.0.0-dev\tools\net10.0\any\").ShouldBeTrue();
        Commands.DevCommands.IsGlobalToolInstall(@"D:\repo\src\Jex.Cli\bin\Debug\net10.0\").ShouldBeFalse();
    }

    [Fact]
    public void Install_cli_refuses_to_replace_the_running_global_tool()
    {
        using var repo = new TempDirectory();
        repo.Write("Jex.CleanArchitecture.Template.csproj", "<Project />");
        var harness = new CliHarness(repo.Path) { BaseDirectory = "/home/u/.dotnet/tools/.store/jex.cli/0.0.0-dev/jex.cli/0.0.0-dev/tools/net10.0/any/" };

        harness.Run("dev", "install-cli").ShouldBe(1);

        harness.Runner.Invocations.ShouldBeEmpty();
        harness.Error.ToString().ShouldContain("dotnet run --project src/Jex.Cli -- dev install-cli");
    }

    [Fact]
    public void Install_cli_clears_the_cached_dev_package_and_installs_from_the_local_source()
    {
        using var repo = new TempDirectory();
        repo.Write("Jex.CleanArchitecture.Template.csproj", "<Project />");
        using var nuget = new TempDirectory();
        var cached = nuget.Write("jex.cli/0.0.0-dev/jex.cli.nuspec", "<package />");
        var harness = new CliHarness(repo.Path) { NuGetPackagesDirectory = nuget.Path };

        harness.Run("dev", "install-cli").ShouldBe(0);

        File.Exists(cached).ShouldBeFalse();
        var artifacts = repo.Combine("artifacts");
        harness.Runner.Invocations.Select(i => i.Arguments).ShouldBe([
            ["pack", Path.Combine("src", "Jex.Cli", "Jex.Cli.csproj"), "-c", "Release", "-p:Version=0.0.0-dev", "-o", artifacts],
            ["tool", "uninstall", "-g", "Jex.Cli"],
            ["tool", "install", "-g", "Jex.Cli", "--source", artifacts, "--version", "0.0.0-dev"],
        ]);
    }
}
