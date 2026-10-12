using System.Reflection;
using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;
using Jex.Domain.Common;

namespace Jex.ArchitectureTests;

/// <summary>
/// Conventions from AGENTS.md §5/§6. Every failure message says how to fix it.
/// </summary>
public class ApplicationConventionTests
{
    private static readonly Assembly s_application = typeof(IUserRepository).Assembly;
    private static readonly Assembly s_domain = typeof(BaseEntity).Assembly;

    private static IEnumerable<Type> ApplicationTypes => s_application.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false } && !t.Name.Contains('<'));

    private static IEnumerable<Type> Commands => ApplicationTypes.Where(Implements(typeof(ICommand), typeof(ICommand<>)));

    private static IEnumerable<Type> Queries => ApplicationTypes.Where(Implements(typeof(IQuery<>)));

    private static IEnumerable<Type> Handlers => ApplicationTypes.Where(Implements(typeof(IRequestHandler<,>), typeof(IRequestHandler<>)));

    private static IEnumerable<Type> Validators => ApplicationTypes.Where(Implements(typeof(IValidator<>)));

    [Fact]
    public void Requests_ImplementCommandOrQuery_NotRawIRequest()
    {
        var raw = ApplicationTypes
            .Where(Implements(typeof(IRequest), typeof(IRequest<>)))
            .Where(t => !Implements(typeof(ICommand), typeof(ICommand<>), typeof(IQuery<>))(t))
            .ToList();

        raw.ShouldBeEmpty(
            $"Requests must implement ICommand/ICommand<T> (writes) or IQuery<T> (reads), not IRequest directly: {Names(raw)}. See AGENTS.md §5.");
    }

    [Fact]
    public void Commands_EndWithCommand_AndAreSealed()
    {
        var bad = Commands.Where(t => !t.Name.EndsWith("Command", StringComparison.Ordinal) || !t.IsSealed).ToList();

        bad.ShouldBeEmpty($"Commands must be sealed records named {{Verb}}{{Entity}}Command: {Names(bad)}.");
    }

    [Fact]
    public void Queries_EndWithQuery_AndAreSealed()
    {
        var bad = Queries.Where(t => !t.Name.EndsWith("Query", StringComparison.Ordinal) || !t.IsSealed).ToList();

        bad.ShouldBeEmpty($"Queries must be sealed records named Get{{...}}Query: {Names(bad)}.");
    }

    [Fact]
    public void EveryCommand_HasExactlyOneValidator()
    {
        var validatedTypes = Validators
            .SelectMany(v => v.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)).Select(i => i.GetGenericArguments()[0]))
            .GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count());

        var missing = Commands.Where(c => !validatedTypes.ContainsKey(c)).ToList();
        var duplicated = Commands.Where(c => validatedTypes.TryGetValue(c, out var n) && n > 1).ToList();

        missing.ShouldBeEmpty(
            $"Every command needs a validator, even an empty one: add sealed class {{Name}}Validator : AbstractValidator<{{Name}}> next to {Names(missing)}. See AGENTS.md §4 step 4.");
        duplicated.ShouldBeEmpty($"Commands must have exactly one validator: {Names(duplicated)}.");
    }

    [Fact]
    public void Handlers_AreSealed_AndEndWithHandler()
    {
        var bad = Handlers.Where(t => !t.IsSealed || !t.Name.EndsWith("Handler", StringComparison.Ordinal)).ToList();

        bad.ShouldBeEmpty($"Handlers must be sealed and named {{Request}}Handler: {Names(bad)}.");
    }

    [Fact]
    public void Validators_AreSealed_AndEndWithValidator()
    {
        var bad = Validators.Where(t => !t.IsSealed || !t.Name.EndsWith("Validator", StringComparison.Ordinal)).ToList();

        bad.ShouldBeEmpty($"Validators must be sealed and named {{Request}}Validator: {Names(bad)}.");
    }

    [Fact]
    public void Handlers_DoNotInjectUnitOfWorkOrFreeSql()
    {
        var bad = Handlers
            .Where(h => h.GetConstructors().SelectMany(c => c.GetParameters())
                .Any(p => p.ParameterType == typeof(IUnitOfWork) || p.ParameterType.Namespace?.StartsWith("FreeSql", StringComparison.Ordinal) == true))
            .ToList();

        bad.ShouldBeEmpty(
            $"Handlers talk to repositories only; transactions are owned by UnitOfWorkBehaviour and FreeSql stays in Infrastructure: {Names(bad)}. See AGENTS.md §6.");
    }

    [Fact]
    public void DomainEvents_DeriveFromBaseEvent_AndEndWithEvent()
    {
        var bad = s_domain.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(INotification).IsAssignableFrom(t))
            .Where(t => !typeof(BaseEvent).IsAssignableFrom(t) || !t.Name.EndsWith("Event", StringComparison.Ordinal))
            .ToList();

        bad.ShouldBeEmpty($"Domain events derive from BaseEvent and are named {{Entity}}{{PastTenseVerb}}Event: {Names(bad)}.");
    }

    [Fact]
    public void DomainEventHandlers_HaveNoExternalSideEffectDependencies()
    {
        var forbidden = new[] { "System.Net.Http.HttpClient", "System.Net.Http.IHttpClientFactory" };

        var bad = ApplicationTypes
            .Where(Implements(typeof(INotificationHandler<>)))
            .Where(h => h.GetConstructors().SelectMany(c => c.GetParameters()).Any(p => forbidden.Contains(p.ParameterType.FullName)))
            .ToList();

        bad.ShouldBeEmpty(
            $"Domain event handlers run inside the command transaction and may only do database work (ADR-0005). " +
            $"Move external calls to an outbox/background job: {Names(bad)}.");
    }

    private static Func<Type, bool> Implements(params Type[] openInterfaces) =>
        type => type.GetInterfaces().Any(i =>
            openInterfaces.Contains(i) || (i.IsGenericType && openInterfaces.Contains(i.GetGenericTypeDefinition())));

    private static string Names(IEnumerable<Type> types) => string.Join(", ", types.Select(t => t.Name));
}
