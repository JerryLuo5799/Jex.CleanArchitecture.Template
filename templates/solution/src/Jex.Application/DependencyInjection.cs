using System.Globalization;
using Jex.Application.Common.Behaviours;
using Jex.Application.Common.Events;
using Jex.Application.Common.Interfaces;

namespace Jex.Application;

/// <summary>
/// Application layer registration: MediatR handlers, pipeline behaviours and FluentValidation validators.
/// Validators and handlers are discovered by assembly scanning; nothing needs manual registration.
/// Pipeline order (outer → inner): Logging → UnhandledException → Authorization → Validation → Performance → UnitOfWork → handler.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Validation messages are always English regardless of the server culture; translate at the edge if needed.
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.InvariantCulture;
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<PendingDomainEvents>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenRequestPreProcessor(typeof(LoggingBehaviour<>));
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddOpenBehavior(typeof(AuthorizationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
            cfg.AddOpenBehavior(typeof(UnitOfWorkBehaviour<,>));
        });

        return services;
    }
}
