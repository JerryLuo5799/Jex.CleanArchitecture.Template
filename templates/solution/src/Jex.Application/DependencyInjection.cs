using FluentValidation;
using Jex.Application.Common.Behaviours;
using Microsoft.Extensions.DependencyInjection;

namespace Jex.Application;

/// <summary>
/// Application layer service registration: MediatR handlers, pipeline behaviours and FluentValidation validators.
/// Validators and handlers are discovered by assembly scanning; nothing needs manual registration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        return services;
    }
}
