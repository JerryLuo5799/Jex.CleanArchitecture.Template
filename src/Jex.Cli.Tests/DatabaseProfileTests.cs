using Jex.Cli.Project;

namespace Jex.Cli.Tests;

public sealed class DatabaseProfileTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Theory]
    [InlineData("Sqlite", "none")]
    [InlineData("PostgreSQL", "postgresql")]
    [InlineData("SqlServer", "sqlserver")]
    [InlineData("MySql", "mysql")]
    public void Provider_maps_to_compose_profile(string provider, string profile)
    {
        _dir.Write("src/Acme.WebAPI/appsettings.json", $$"""{ "Database": { "Provider": "{{provider}}" } }""");

        DatabaseProfile.Resolve(_dir.Path, "src/Acme.WebAPI/Acme.WebAPI.csproj").ShouldBe(profile);
    }

    [Fact]
    public void Template_conditional_comments_and_trailing_commas_are_tolerated()
    {
        _dir.Write("src/Acme.WebAPI/appsettings.json", """
            {
              "Database": {
                //#if (TemplateSourceOnly)
                "Provider": "PostgreSQL",
                //#endif
                "AutoSyncStructure": true,
              },
            }
            """);

        DatabaseProfile.Resolve(_dir.Path, "src/Acme.WebAPI/Acme.WebAPI.csproj").ShouldBe("postgresql");
    }

    [Fact]
    public void Template_placeholder_asks_for_an_explicit_profile()
    {
        _dir.Write("src/Jex.WebAPI/appsettings.json", """{ "Database": { "Provider": "FREESQL_PROVIDER" } }""");

        var ex = Should.Throw<JexException>(() => DatabaseProfile.Resolve(_dir.Path, "src/Jex.WebAPI/Jex.WebAPI.csproj"));

        ex.Message.ShouldContain("FREESQL_PROVIDER");
        ex.Message.ShouldContain("--profile");
    }

    [Fact]
    public void Missing_provider_asks_for_an_explicit_profile()
    {
        _dir.Write("src/Acme.WebAPI/appsettings.json", "{}");

        Should.Throw<JexException>(() => DatabaseProfile.Resolve(_dir.Path, "src/Acme.WebAPI/Acme.WebAPI.csproj")).Message.ShouldContain("--profile");
    }
}
