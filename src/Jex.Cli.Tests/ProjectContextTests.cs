using Jex.Cli.Project;

namespace Jex.Cli.Tests;

public sealed class ProjectContextTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Discover_walks_up_from_a_nested_directory()
    {
        TestProject.Create(_dir, "Acme", TestProject.StandardProjects("Acme"));
        var nested = _dir.Combine("src", "Acme.Domain");

        var project = ProjectContext.Discover(nested);

        project.Root.ShouldBe(_dir.Path);
        project.SolutionFile.ShouldBe("Acme.slnx");
        project.StartDirectory.ShouldBe(nested);
    }

    [Fact]
    public void Discover_without_manifest_explains_init()
    {
        var ex = Should.Throw<JexException>(() => ProjectContext.Discover(_dir.Path));

        ex.Message.ShouldContain("jex init");
    }

    [Fact]
    public void Discover_requires_exactly_one_solution()
    {
        _dir.Write("jex.json", """{ "schemaVersion": 1, "template": "jex", "templateVersion": "0.0.0-dev" }""");
        Should.Throw<JexException>(() => ProjectContext.Discover(_dir.Path)).Message.ShouldContain("No *.slnx");

        _dir.Write("A.slnx", "<Solution />");
        _dir.Write("B.slnx", "<Solution />");
        Should.Throw<JexException>(() => ProjectContext.Discover(_dir.Path)).Message.ShouldContain("exactly one");
    }

    [Fact]
    public void ReadProjects_reads_references_inside_template_conditionals()
    {
        var slnx = _dir.Write("Jex.slnx", """
            <Solution>
              <Folder Name="/src/">
                <Project Path="src/Jex.WebAPI/Jex.WebAPI.csproj" />
                <!--#if (UseAspire) -->
                <Project Path="src\Jex.AppHost\Jex.AppHost.csproj" />
                <!--#endif -->
              </Folder>
            </Solution>
            """);

        ProjectContext.ReadProjects(slnx).ShouldBe(["src/Jex.WebAPI/Jex.WebAPI.csproj", "src/Jex.AppHost/Jex.AppHost.csproj"]);
    }

    [Fact]
    public void Renamed_solution_still_resolves_projects_and_scaffold_prefix()
    {
        TestProject.Create(_dir, "Backend", TestProject.StandardProjects("Acme"));

        var project = ProjectContext.Discover(_dir.Path);

        project.Name.ShouldBe("Backend");
        project.ResolveApiProject().ShouldBe("src/Acme.WebAPI/Acme.WebAPI.csproj");
        project.ResolveTestProject("unit").ShouldBe("tests/Acme.UnitTests/Acme.UnitTests.csproj");
        project.ResolveScaffoldPrefix().ShouldBe("Acme");
    }

    [Fact]
    public void Kebab_case_project_names_use_the_safe_prefix()
    {
        TestProject.Create(_dir, "my_api", TestProject.StandardProjects("my_api"));

        ProjectContext.Discover(_dir.Path).ResolveScaffoldPrefix().ShouldBe("my_api");
    }

    [Fact]
    public void Missing_domain_project_fails_only_the_prefix_resolution()
    {
        TestProject.Create(_dir, "Acme", TestProject.StandardProjects("Acme").Where(p => !p.Contains(".Domain", StringComparison.Ordinal)));

        var project = ProjectContext.Discover(_dir.Path);

        project.ResolveApiProject().ShouldBe("src/Acme.WebAPI/Acme.WebAPI.csproj");
        Should.Throw<JexException>(() => project.ResolveScaffoldPrefix()).Message.ShouldContain("--solution-name");
    }

    [Fact]
    public void Explicit_solution_name_wins_without_inference()
    {
        TestProject.Create(_dir, "Acme", ["src/Odd/Odd.csproj"]);

        ProjectContext.Discover(_dir.Path).ResolveScaffoldPrefix("Foo").ShouldBe("Foo");
    }

    [Fact]
    public void Zero_or_two_api_projects_fail_only_when_the_api_is_needed()
    {
        TestProject.Create(_dir, "Acme", ["src/A.WebAPI/A.WebAPI.csproj", "src/B.WebAPI/B.WebAPI.csproj"]);
        var project = ProjectContext.Discover(_dir.Path);
        Should.Throw<JexException>(() => project.ResolveApiProject()).Message.ShouldContain("--api-project");

        using var empty = new TempDirectory();
        TestProject.Create(empty, "Acme", ["src/Acme.Domain/Acme.Domain.csproj"]);
        var withoutApi = ProjectContext.Discover(empty.Path);
        Should.Throw<JexException>(() => withoutApi.ResolveApiProject()).Message.ShouldContain("--api-project");
        Should.Throw<JexException>(() => withoutApi.ResolveScaffoldPrefix()).Message.ShouldContain("--solution-name");
    }

    [Fact]
    public void Api_project_override_accepts_a_directory_or_a_project_file()
    {
        TestProject.Create(_dir, "Acme", ["src/Shop.Api/Shop.Api.csproj"]);
        var project = ProjectContext.Discover(_dir.Path);

        project.ResolveApiProject("src/Shop.Api").ShouldBe("src/Shop.Api/Shop.Api.csproj");
        project.ResolveApiProject(_dir.Combine("src", "Shop.Api", "Shop.Api.csproj")).ShouldBe("src/Shop.Api/Shop.Api.csproj");
        Should.Throw<JexException>(() => project.ResolveApiProject("src/Missing"));
    }

    [Fact]
    public void Discover_does_not_read_appsettings()
    {
        TestProject.Create(_dir, "Acme", TestProject.StandardProjects("Acme"));
        _dir.Write("src/Acme.WebAPI/appsettings.json", "{ this is not json");

        Should.NotThrow(() => ProjectContext.Discover(_dir.Path));
    }

    [Fact]
    public void AppHost_is_resolved_on_demand()
    {
        TestProject.Create(_dir, "Acme", TestProject.StandardProjects("Acme", aspire: true));
        ProjectContext.Discover(_dir.Path).ResolveAppHostProject().ShouldBe("src/Acme.AppHost/Acme.AppHost.csproj");

        using var plain = new TempDirectory();
        TestProject.Create(plain, "Acme", TestProject.StandardProjects("Acme"));
        Should.Throw<JexException>(() => ProjectContext.Discover(plain.Path).ResolveAppHostProject()).Message.ShouldContain("--use-aspire");
    }

    [Fact]
    public void Manifest_must_be_a_jex_manifest()
    {
        TestProject.Create(_dir, "Acme", TestProject.StandardProjects("Acme"));
        _dir.Write("jex.json", """{ "schemaVersion": 1, "template": "other" }""");

        Should.Throw<JexException>(() => ProjectContext.Discover(_dir.Path)).Message.ShouldContain("not a jex manifest");
    }
}
