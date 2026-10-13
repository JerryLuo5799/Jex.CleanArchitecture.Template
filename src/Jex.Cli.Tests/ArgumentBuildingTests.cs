namespace Jex.Cli.Tests;

public sealed class ArgumentBuildingTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly CliHarness _cli;

    public ArgumentBuildingTests()
    {
        TestProject.Create(_dir, "Acme", TestProject.StandardProjects("Acme"));
        _cli = new CliHarness(_dir.Combine("src", "Acme.Application"));
    }

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Commands_run_from_the_solution_root()
    {
        _cli.Run("build").ShouldBe(0);

        _cli.Runner.Invocations.Single().WorkingDirectory.ShouldBe(_dir.Path);
        _cli.Runner.CommandLines.ShouldBe(["dotnet build Acme.slnx"]);
    }

    [Fact]
    public void Usecase_passes_exact_item_template_arguments()
    {
        _cli.Run("scaffold", "usecase", "GetOrderById", "Orders", "--type", "query", "--return", "OrderDto", "--with-id").ShouldBe(0);

        _cli.Runner.CommandLines.ShouldBe([
            "dotnet new jex-usecase -n GetOrderById --feature Orders --usecase-type query --return-type OrderDto --with-id --solution-name Acme -o .",
        ]);
    }

    [Fact]
    public void Entity_and_endpoint_options_map_to_item_template_parameters()
    {
        _cli.Run("scaffold", "entity", "Order", "Orders", "--no-test").ShouldBe(0);
        _cli.Run("scaffold", "endpoint", "Reports", "--solution-name", "Other").ShouldBe(0);

        _cli.Runner.CommandLines.ShouldBe([
            "dotnet new jex-entity -n Order --plural Orders --solution-name Acme -o . --include-test false",
            "dotnet new jex-endpoint -n Reports --solution-name Other -o .",
        ]);
    }

    [Fact]
    public void Feature_scaffolds_entity_usecases_and_endpoint_in_order()
    {
        _cli.Run("scaffold", "feature", "Order", "Orders").ShouldBe(0);

        _cli.Runner.CommandLines.ShouldBe([
            "dotnet new jex-entity -n Order --plural Orders --solution-name Acme -o .",
            "dotnet new jex-usecase -n CreateOrder --feature Orders --usecase-type command --return-type long --solution-name Acme -o .",
            "dotnet new jex-usecase -n GetOrderById --feature Orders --usecase-type query --return-type OrderDto --with-id --solution-name Acme -o .",
            "dotnet new jex-endpoint -n Orders --entity Order --solution-name Acme -o .",
        ]);
        _cli.Runner.Captures.Count.ShouldBe(1, "the template version is checked once");
    }

    [Fact]
    public void Check_ci_runs_the_strict_gate_in_order()
    {
        _cli.Run("check", "--ci").ShouldBe(0);

        _cli.Runner.CommandLines.ShouldBe([
            "dotnet format Acme.slnx --verify-no-changes",
            "dotnet build Acme.slnx -c Release -p:ContinuousIntegrationBuild=true",
            "dotnet test --solution Acme.slnx -c Release --no-build",
        ]);
    }

    [Fact]
    public void Check_applies_formatting_then_builds_and_tests()
    {
        _cli.Run("check").ShouldBe(0);

        _cli.Runner.CommandLines.ShouldBe(["dotnet format Acme.slnx", "dotnet build Acme.slnx", "dotnet test --solution Acme.slnx"]);
    }

    [Fact]
    public void Db_migrate_builds_then_runs_without_rebuilding()
    {
        _cli.Run("db", "migrate", "--dry-run").ShouldBe(0);

        _cli.Runner.CommandLines.ShouldBe([
            "dotnet build src/Acme.WebAPI/Acme.WebAPI.csproj",
            "dotnet run --project src/Acme.WebAPI/Acme.WebAPI.csproj --no-build --no-launch-profile -- migrate --dry-run",
        ]);
    }

    [Fact]
    public void Db_script_defaults_to_update_and_accepts_an_output_directory()
    {
        _cli.Run("db", "script").ShouldBe(0);
        _cli.Run("db", "script", "add-orders", "db/other").ShouldBe(0);

        _cli.Runner.CommandLines.ShouldBe([
            "dotnet run --project src/Acme.WebAPI/Acme.WebAPI.csproj --no-launch-profile -- db-script update",
            "dotnet run --project src/Acme.WebAPI/Acme.WebAPI.csproj --no-launch-profile -- db-script add-orders db/other",
        ]);
    }

    [Fact]
    public void Db_reset_deletes_only_sqlite_files_next_to_the_api()
    {
        var db = _dir.Write("src/Acme.WebAPI/dev.db", "x");
        var wal = _dir.Write("src/Acme.WebAPI/dev.db-wal", "x");
        var settings = _dir.Write("src/Acme.WebAPI/appsettings.json", "{}");

        _cli.Run("db", "reset").ShouldBe(0);

        File.Exists(db).ShouldBeFalse();
        File.Exists(wal).ShouldBeFalse();
        File.Exists(settings).ShouldBeTrue();
        _cli.Runner.Invocations.ShouldBeEmpty();
    }

    [Fact]
    public void Test_suite_and_run_resolve_projects_from_the_solution()
    {
        _cli.Run("test", "integration").ShouldBe(0);
        _cli.Run("run").ShouldBe(0);
        _cli.Run("watch").ShouldBe(0);

        _cli.Runner.CommandLines.ShouldBe([
            "dotnet test --project tests/Acme.IntegrationTests/Acme.IntegrationTests.csproj",
            "dotnet run --project src/Acme.WebAPI/Acme.WebAPI.csproj",
            "dotnet watch --project src/Acme.WebAPI/Acme.WebAPI.csproj",
        ]);
    }

    [Fact]
    public void Build_format_and_test_do_not_need_an_api_project()
    {
        using var library = new TempDirectory();
        TestProject.Create(library, "Lib", ["src/Lib/Lib.csproj"]);
        var cli = new CliHarness(library.Path);

        cli.Run("build").ShouldBe(0);
        cli.Run("format", "--check").ShouldBe(0);
        cli.Run("test").ShouldBe(0);

        cli.Runner.CommandLines.ShouldBe(["dotnet build Lib.slnx", "dotnet format Lib.slnx --verify-no-changes", "dotnet test --solution Lib.slnx"]);
    }

    [Fact]
    public void Infra_skips_sqlite_and_uses_the_provider_profile()
    {
        var settings = _dir.Write("src/Acme.WebAPI/appsettings.json", """{ "Database": { "Provider": "Sqlite" } }""");
        _dir.Write("docker-compose.yml", "services: {}");

        _cli.Run("infra", "up").ShouldBe(0);
        _cli.Runner.Invocations.ShouldBeEmpty();
        _cli.Out.ToString().ShouldContain("SQLite needs no infrastructure");

        File.WriteAllText(settings, """{ "Database": { "Provider": "PostgreSQL" } }""");
        _cli.Run("infra", "up").ShouldBe(0);
        _cli.Run("infra", "down", "--profile", "mysql").ShouldBe(0);

        _cli.Runner.CommandLines.ShouldBe([
            "docker compose --profile postgresql up -d --wait",
            "docker compose --profile mysql down -v",
        ]);
    }

    [Fact]
    public void Infra_without_compose_file_fails()
    {
        _dir.Write("src/Acme.WebAPI/appsettings.json", """{ "Database": { "Provider": "PostgreSQL" } }""");

        _cli.Run("infra", "up").ShouldBe(1);

        _cli.Error.ToString().ShouldContain("docker-compose.yml");
    }

    [Fact]
    public void New_forwards_known_and_unknown_options_to_dotnet_new()
    {
        _cli.Run("new", "Acme.Shop", "-o", "out dir", "--use-aspire", "--include-docker", "false", "--http-port", "5100", "--custom", "x").ShouldBe(0);

        _cli.Runner.Invocations.Single().Arguments.ShouldBe([
            "new", "jex", "-n", "Acme.Shop", "-o", "out dir", "--use-aspire", "true", "--include-docker", "false", "--http-port", "5100", "--custom", "x",
        ]);
        _cli.Out.ToString().ShouldContain("jex check");
    }

    // ── Failure propagation ─────────────────────────────────────────────────────

    [Fact]
    public void Check_stops_at_the_failing_build()
    {
        _cli.Runner.ThenExit(0, 1);

        _cli.Run("check").ShouldBe(1);

        _cli.Runner.CommandLines.ShouldBe(["dotnet format Acme.slnx", "dotnet build Acme.slnx"]);
        _cli.Error.ToString().ShouldContain("exited with code 1");
    }

    [Fact]
    public void Db_migrate_does_not_run_after_a_failed_build()
    {
        _cli.Runner.ThenExit(1);

        _cli.Run("db", "migrate").ShouldBe(1);

        _cli.Runner.Invocations.Count.ShouldBe(1);
    }

    [Fact]
    public void Scaffold_feature_stops_at_the_first_failing_template_and_keeps_its_exit_code()
    {
        _cli.Runner.ThenExit(0, 73);

        _cli.Run("scaffold", "feature", "Order", "Orders").ShouldBe(73);

        _cli.Runner.Invocations.Count.ShouldBe(2);
    }

    [Fact]
    public void Scaffold_checks_the_template_version_before_generating()
    {
        TestProject.Create(_dir, "Acme", TestProject.StandardProjects("Acme"), templateVersion: "1.0.0");
        _cli.Runner.InstalledTemplates = """
               Jex.CleanArchitecture.Template
                  Version: 2.0.0
                  Templates:
                     Jex Clean Architecture Solution (jex) C#
            """;

        _cli.Run("scaffold", "entity", "Foo", "Foos").ShouldBe(1);
        _cli.Runner.Invocations.ShouldBeEmpty();

        _cli.Run("scaffold", "entity", "Foo", "Foos", "--force").ShouldBe(0);
        _cli.Runner.Invocations.Count.ShouldBe(1);
    }

    [Fact]
    public void Init_writes_a_manifest_without_a_template_version()
    {
        using var old = new TempDirectory();
        old.Write("Old.slnx", "<Solution />");
        var cli = new CliHarness(old.Path);

        cli.Run("init").ShouldBe(0);
        cli.Run("init").ShouldBe(1);

        var manifest = Jex.Cli.Project.JexManifest.Load(old.Combine("jex.json"));
        manifest.TemplateVersion.ShouldBeNull();
        manifest.Template.ShouldBe("jex");
    }
}
