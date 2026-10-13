using Jex.Cli.Project;

namespace Jex.Cli.Tests;

public sealed class SandboxPathTests : IDisposable
{
    private readonly TempDirectory _repo = new();

    public SandboxPathTests() => _repo.Write(RepoContext.MarkerFile, "<Project />");

    public void Dispose() => _repo.Dispose();

    [Theory]
    [InlineData("Acme")]
    [InlineData("Acme.Shop")]
    [InlineData("my-api")]
    [InlineData("_internal")]
    public void Valid_names_resolve_inside_the_sandbox(string name)
    {
        var repo = RepoContext.Discover(_repo.Path);

        repo.ResolveSandbox(name).ShouldBe(Path.Combine(_repo.Path, "artifacts", "sandbox", name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("..")]
    [InlineData("..\\x")]
    [InlineData("../x")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("C:\\abs")]
    [InlineData("/abs")]
    [InlineData("1Acme")]
    [InlineData("Acme..Shop")]
    public void Unsafe_names_are_rejected(string name)
    {
        var repo = RepoContext.Discover(_repo.Path);

        Should.Throw<JexException>(() => repo.ResolveSandbox(name));
    }

    [Fact]
    public void Dev_new_rejects_traversal_before_deleting_anything()
    {
        var victim = _repo.Write("artifacts/keep.txt", "keep");
        var harness = new CliHarness(_repo.Path);

        harness.Run("dev", "new", "..\\artifacts").ShouldBe(1);

        File.Exists(victim).ShouldBeTrue();
        harness.Runner.Invocations.ShouldBeEmpty();
        harness.Error.ToString().ShouldContain("not a valid project name");
    }

    [Fact]
    public void Dev_commands_outside_the_repository_explain_where_to_run()
    {
        using var elsewhere = new TempDirectory();
        var harness = new CliHarness(elsewhere.Path);

        harness.Run("dev", "pack").ShouldBe(1);

        harness.Error.ToString().ShouldContain(RepoContext.MarkerFile);
    }
}
