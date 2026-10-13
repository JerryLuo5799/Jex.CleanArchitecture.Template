using Jex.Cli.Templates;

namespace Jex.Cli.Tests;

public sealed class TemplateInstallerTests
{
    private const string Sample = """
        Currently installed items:
           Microsoft.DotNet.Common.ProjectTemplates.10.0
              Version: 10.0.100
              Details:
                 Author: Microsoft
              Templates:
                 Class Library (classlib) C#,F#,VB
                 Aspire Starter App (ASP.NET Core/Blazor) (aspire-starter) C#
              Uninstall Command:
                 dotnet new uninstall Microsoft.DotNet.Common.ProjectTemplates.10.0

           Jex.CleanArchitecture.Template
              Version: 2.1.0-rc.1
              Details:
                 Author: JerryLuo
                 NuGetSource: https://api.nuget.org/v3/index.json
              Templates:
                 Jex Endpoint Group (jex-endpoint) C#
                 Jex Clean Architecture Solution (jex) C#
              Uninstall Command:
                 dotnet new uninstall Jex.CleanArchitecture.Template

           C:\src\Jex.CleanArchitecture.Template\templates
              Templates:
                 Jex Clean Architecture Solution (jex) C#
              Uninstall Command:
                 dotnet new uninstall C:\src\Jex.CleanArchitecture.Template\templates
        """;

    private static readonly InstalledTemplate s_directory = new(@"C:\repo\templates", null, IsDirectory: true);

    [Fact]
    public void Parser_reads_packages_versions_and_short_names()
    {
        var installs = TemplateInstallListParser.Parse(Sample.ReplaceLineEndings("\r\n"));

        installs.Count.ShouldBe(3);
        installs[0].ShortNames.ShouldBe(["classlib", "ASP.NET Core/Blazor", "aspire-starter"]);
        installs[1].Source.ShouldBe("Jex.CleanArchitecture.Template");
        installs[1].Version.ShouldBe("2.1.0-rc.1");
        installs[1].ShortNames.ShouldContain("jex");
        installs[2].IsDirectory.ShouldBeTrue();
        installs[2].ShortNames.ShouldBe(["jex"]);
    }

    [Fact]
    public void Detect_prefers_the_directory_install_and_warns_about_duplicates()
    {
        var harness = new CliHarness(Path.GetTempPath());
        harness.Runner.InstalledTemplates = Sample;

        var installed = new TemplateInstaller(harness.Context).Detect();

        installed.ShouldNotBeNull();
        installed.IsDirectory.ShouldBeTrue();
        harness.Error.ToString().ShouldContain("more than once");
    }

    [Fact]
    public void Detect_returns_null_when_only_other_templates_are_installed()
    {
        var harness = new CliHarness(Path.GetTempPath());
        harness.Runner.InstalledTemplates = Sample.ReplaceLineEndings("\n").Split("\n\n")[0];

        new TemplateInstaller(harness.Context).Detect().ShouldBeNull();
    }

    // ── jex new ─────────────────────────────────────────────────────────────────

    [Fact]
    public void New_installs_the_cli_version_when_nothing_is_installed()
    {
        var decision = TemplatePolicy.ForNew(V("2.1.0"), null, requested: null, noInstall: false);

        decision.Action.ShouldBe(NewTemplateAction.Install);
        decision.Version.ShouldBe(V("2.1.0"));
    }

    [Fact]
    public void New_uses_matching_templates()
    {
        TemplatePolicy.ForNew(V("2.1.0"), Package("2.1.0"), null, false).Action.ShouldBe(NewTemplateAction.Use);
    }

    [Fact]
    public void New_switches_to_the_cli_version_and_warns_when_templates_are_newer()
    {
        var decision = TemplatePolicy.ForNew(V("2.1.0"), Package("2.2.0"), null, false);

        decision.Action.ShouldBe(NewTemplateAction.Install);
        decision.Version.ShouldBe(V("2.1.0"));
        decision.Messages.ShouldContain(m => m.Contains("dotnet tool update -g Jex.Cli", StringComparison.Ordinal));
    }

    [Fact]
    public void New_with_explicit_version_installs_it_even_as_a_downgrade()
    {
        var decision = TemplatePolicy.ForNew(V("2.1.0"), Package("2.1.0"), V("2.0.0"), false);

        decision.Action.ShouldBe(NewTemplateAction.Install);
        decision.Version.ShouldBe(V("2.0.0"));
    }

    [Fact]
    public void New_no_install_fails_instead_of_installing()
    {
        Should.Throw<JexException>(() => TemplatePolicy.ForNew(V("2.1.0"), null, null, noInstall: true));
        Should.Throw<JexException>(() => TemplatePolicy.ForNew(V("2.1.0"), Package("2.0.0"), null, noInstall: true));
    }

    [Fact]
    public void New_dev_cli_uses_dev_templates_and_never_installs()
    {
        TemplatePolicy.ForNew(SemVer.Dev, s_directory, null, false).Action.ShouldBe(NewTemplateAction.Use);
        TemplatePolicy.ForNew(SemVer.Dev, Package("0.0.0-ci.7"), null, false).Action.ShouldBe(NewTemplateAction.Use);
        Should.Throw<JexException>(() => TemplatePolicy.ForNew(SemVer.Dev, null, null, false)).Message.ShouldContain("jex dev install");
        Should.Throw<JexException>(() => TemplatePolicy.ForNew(SemVer.Dev, Package("2.1.0"), null, false)).Message.ShouldContain("jex dev install");
    }

    [Fact]
    public void New_dev_cli_with_explicit_published_version_follows_the_release_rules()
    {
        var decision = TemplatePolicy.ForNew(SemVer.Dev, Package("2.1.0-rc.1"), V("2.1.0-rc.1"), false);
        decision.Action.ShouldBe(NewTemplateAction.Use);

        var install = TemplatePolicy.ForNew(SemVer.Dev, s_directory, V("2.1.0"), false);
        install.Action.ShouldBe(NewTemplateAction.Install);
        install.UninstallSource.ShouldBe(s_directory.Source);
    }

    [Fact]
    public void New_release_cli_uses_a_directory_install_when_no_version_is_requested()
    {
        TemplatePolicy.ForNew(V("2.1.0"), s_directory, null, false).Action.ShouldBe(NewTemplateAction.Use);
    }

    [Fact]
    public void New_install_runs_dotnet_new_install_with_the_exact_version()
    {
        using var dir = new TempDirectory();
        var harness = new CliHarness(dir.Path) { Version = V("2.1.0") };
        harness.Runner.InstalledTemplates = "Currently installed items:\n";

        harness.Run("new", "Acme", "--database", "postgresql").ShouldBe(0);

        harness.Runner.CommandLines.ShouldBe([
            "dotnet new install Jex.CleanArchitecture.Template@2.1.0 --force",
            "dotnet new jex -n Acme --database postgresql",
        ]);
    }

    // ── jex scaffold ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("2.1.0", "2.1.0", null, null)]
    [InlineData("2.1.0-rc.1", "2.1.0-rc.2", null, "differ")]
    [InlineData("2.1.0-rc.2", "2.1.0", null, "differ")]
    [InlineData("2.1.0", "2.0.0", null, "differ")]
    [InlineData("2.0.0", "2.3.1", null, "differ")]
    [InlineData("2.1.0", "1.0.0", "2.1.0", null)]
    public void Scaffold_same_major_proceeds(string installed, string project, string? requested, string? warning)
    {
        var decision = TemplatePolicy.ForScaffold(project, Package(installed), requested is null ? null : V(requested), force: false);

        if (warning is null)
        {
            decision.Warning.ShouldBeNull();
        }
        else
        {
            decision.Warning.ShouldNotBeNull().ShouldContain(warning);
        }
    }

    [Theory]
    [InlineData("2.1.0", "1.0.0", null)]
    [InlineData("2.1.0-rc.1", "3.0.0-rc.1", null)]
    [InlineData("3.0.0", "0.0.0-dev", "2.0.0")]
    [InlineData("2.1.0", "2.1.0", "3.0.0")]
    public void Scaffold_different_major_fails_with_both_ways_out(string installed, string project, string? requested)
    {
        var ex = Should.Throw<JexException>(() => TemplatePolicy.ForScaffold(project, Package(installed), requested is null ? null : V(requested), force: false));

        ex.Message.ShouldContain("dotnet new install Jex.CleanArchitecture.Template@");
        ex.Message.ShouldContain("--force");
    }

    [Fact]
    public void Scaffold_dev_rules_apply_only_without_an_explicit_version()
    {
        TemplatePolicy.ForScaffold("0.0.0-dev", s_directory, null, false).Warning.ShouldBeNull();
        TemplatePolicy.ForScaffold("0.0.0-dev", Package("3.0.0"), null, false).Warning.ShouldNotBeNull().ShouldContain("development build");
        TemplatePolicy.ForScaffold("2.0.0", s_directory, null, false).Warning.ShouldNotBeNull().ShouldContain("development build");

        Should.Throw<JexException>(() => TemplatePolicy.ForScaffold("0.0.0-dev", Package("3.0.0"), V("2.0.0"), false));
        Should.Throw<JexException>(() => TemplatePolicy.ForScaffold("0.0.0-dev", s_directory, V("2.0.0"), false));
    }

    [Fact]
    public void Scaffold_unknown_project_version_needs_an_explicit_version_or_force()
    {
        Should.Throw<JexException>(() => TemplatePolicy.ForScaffold(null, Package("2.0.0"), null, false)).Message.ShouldContain("--template-version");
        TemplatePolicy.ForScaffold(null, Package("2.0.0"), V("2.0.0"), false).Warning.ShouldBeNull();
        TemplatePolicy.ForScaffold("garbage", Package("2.0.0"), null, force: true).Warning.ShouldNotBeNull();
    }

    [Fact]
    public void Scaffold_without_templates_never_installs()
    {
        Should.Throw<JexException>(() => TemplatePolicy.ForScaffold("2.0.0", null, null, false)).Message.ShouldContain("@2.0.0");
    }

    private static SemVer V(string text) => SemVer.Parse(text);

    private static InstalledTemplate Package(string version) => new(TemplatePolicy.PackageId, V(version), IsDirectory: false);
}
