using Jex.Cli.Templates;

namespace Jex.Cli.Tests;

public sealed class SemVerTests
{
    [Theory]
    [InlineData("2.0.0", 2, 0, 0, "")]
    [InlineData("2.1.0-rc.1", 2, 1, 0, "rc.1")]
    [InlineData("0.0.0-dev", 0, 0, 0, "dev")]
    [InlineData("1.2.3+abcdef", 1, 2, 3, "")]
    [InlineData("1.2.3-beta.1+sha.5", 1, 2, 3, "beta.1")]
    public void Parses(string text, int major, int minor, int patch, string prerelease)
    {
        var version = SemVer.Parse(text);

        version.Major.ShouldBe(major);
        version.Minor.ShouldBe(minor);
        version.Patch.ShouldBe(patch);
        version.Prerelease.ShouldBe(prerelease);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.2")]
    [InlineData("v1.2.3")]
    [InlineData("01.2.3")]
    [InlineData("1.2.3-")]
    [InlineData("1.2.3-rc..1")]
    [InlineData("latest")]
    public void Rejects_invalid_versions(string text)
    {
        SemVer.TryParse(text, out _).ShouldBeFalse();
        Should.Throw<JexException>(() => SemVer.Parse(text));
    }

    [Theory]
    [InlineData("2.0.0-rc.2", "2.0.0-rc.10")]
    [InlineData("2.0.0-rc.10", "2.0.0")]
    [InlineData("2.0.0-rc.2", "2.0.0")]
    [InlineData("1.9.9", "1.10.0")]
    [InlineData("1.0.0-alpha", "1.0.0-alpha.1")]
    [InlineData("1.0.0-alpha.1", "1.0.0-alpha.beta")]
    [InlineData("1.0.0-beta.11", "1.0.0-rc.1")]
    [InlineData("0.0.0-dev", "0.0.1")]
    public void Orders_by_semver_precedence(string lower, string higher)
    {
        var low = SemVer.Parse(lower);
        var high = SemVer.Parse(higher);

        (low < high).ShouldBeTrue();
        (high > low).ShouldBeTrue();
        low.CompareTo(high).ShouldBeLessThan(0);
        (low == high).ShouldBeFalse();
    }

    [Fact]
    public void Build_metadata_does_not_affect_equality()
    {
        (SemVer.Parse("1.2.3+a") == SemVer.Parse("1.2.3+b")).ShouldBeTrue();
        SemVer.Parse("1.2.3+a").ToString().ShouldBe("1.2.3");
    }

    [Theory]
    [InlineData("0.0.0-dev", true)]
    [InlineData("0.0.0-ci.42", true)]
    [InlineData("0.0.1", false)]
    [InlineData("2.0.0", false)]
    public void Zero_versions_are_development_builds(string text, bool isDev) => SemVer.Parse(text).IsDev.ShouldBe(isDev);
}
