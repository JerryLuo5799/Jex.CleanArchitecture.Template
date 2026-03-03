using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Jex.Application.Common.Behaviors;

namespace Jex.Application;

/// <summary>
/// Application layer service registration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
