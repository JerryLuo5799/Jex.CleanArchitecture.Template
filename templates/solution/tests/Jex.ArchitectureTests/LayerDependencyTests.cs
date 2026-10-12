using System.Reflection;
using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;
using Jex.Infrastructure.Persistence.Repositories;
using NetArchTest.Rules;

namespace Jex.ArchitectureTests;

/// <summary>
/// Clean Architecture dependency rule. Identity entity base types (Microsoft.AspNetCore.Identity.IdentityUser etc.)
/// are an accepted exception recorded in ADR-0007; HTTP/MVC/hosting namespaces are not.
///
///   Domain ← Application ← Infrastructure
///                ↑               ↑
///              WebAPI ────────────
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly s_domainAssembly = typeof(BaseEntity).Assembly;
    private static readonly Assembly s_applicationAssembly = typeof(IUserRepository).Assembly;
    private static readonly Assembly s_infrastructureAssembly = typeof(UserRepository).Assembly;

    [Theory]
    [InlineData("Jex.Application")]
    [InlineData("Jex.Infrastructure")]
    [InlineData("Jex.WebAPI")]
    [InlineData("FreeSql")]
    [InlineData("Microsoft.AspNetCore.Http")]
    [InlineData("Microsoft.AspNetCore.Mvc")]
    [InlineData("Microsoft.AspNetCore.Builder")]
    public void Domain_ShouldNotDependOn(string forbiddenNamespace)
    {
        var result = Types.InAssembly(s_domainAssembly)
            .ShouldNot().HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Explain("Domain", forbiddenNamespace, result));
    }

    [Theory]
    [InlineData("Jex.Infrastructure")]
    [InlineData("Jex.WebAPI")]
    [InlineData("FreeSql")]
    [InlineData("Microsoft.AspNetCore.Http")]
    [InlineData("Microsoft.AspNetCore.Mvc")]
    [InlineData("Microsoft.AspNetCore.Builder")]
    [InlineData("Microsoft.AspNetCore.Authorization")]
    public void Application_ShouldNotDependOn(string forbiddenNamespace)
    {
        var result = Types.InAssembly(s_applicationAssembly)
            .ShouldNot().HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Explain("Application", forbiddenNamespace, result));
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_WebApi()
    {
        var result = Types.InAssembly(s_infrastructureAssembly)
            .ShouldNot().HaveDependencyOn("Jex.WebAPI")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Explain("Infrastructure", "Jex.WebAPI", result));
    }

    private static string Explain(string layer, string forbidden, NetArchTest.Rules.TestResult result) =>
        $"{layer} must not reference '{forbidden}'. Offending types: {string.Join(", ", result.FailingTypeNames ?? [])}. " +
        "Move the dependency behind an interface in Jex.Application/Common/Interfaces (see AGENTS.md §2).";
}
