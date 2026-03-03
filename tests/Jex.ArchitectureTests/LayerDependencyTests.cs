using NetArchTest.Rules;
using Jex.Domain.Entities;
using Jex.Application.Common;
using Jex.Infrastructure.Persistence.Repositories;

namespace Jex.ArchitectureTests;

/// <summary>
/// Validates Clean Architecture dependency rules using NetArchTest.
/// The dependency rule: outer layers may depend on inner layers but never the reverse.
///
///   Domain ← Application ← Infrastructure
///                ↑               ↑
///              WebAPI ────────────
/// </summary>
public class LayerDependencyTests
{
    private static readonly System.Reflection.Assembly DomainAssembly      = typeof(BaseEntity).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(IUserRepository).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(UserRepository).Assembly;
    private static readonly System.Reflection.Assembly WebApiAssembly      = typeof(Program).Assembly;

    // ── Domain ───────────────────────────────────────────────────────────────

    [Fact]
    public void Domain_ShouldNotDependOn_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot().HaveDependencyOn("Jex.Application")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain references Application: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot().HaveDependencyOn("Jex.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain references Infrastructure: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_WebApi()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot().HaveDependencyOn("Jex.WebAPI")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain references WebAPI: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // ── Application ──────────────────────────────────────────────────────────

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot().HaveDependencyOn("Jex.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application references Infrastructure: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_ShouldNotDependOn_WebApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot().HaveDependencyOn("Jex.WebAPI")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application references WebAPI: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // ── Infrastructure ───────────────────────────────────────────────────────

    [Fact]
    public void Infrastructure_ShouldNotDependOn_WebApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot().HaveDependencyOn("Jex.WebAPI")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Infrastructure references WebAPI: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
