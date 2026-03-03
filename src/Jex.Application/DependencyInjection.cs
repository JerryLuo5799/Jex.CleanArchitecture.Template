using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Jex.Application.Common.Behaviors;
using Jex.Application.Features.Users.Commands.CreateUser;
using Jex.Application.Features.Users.Commands.UpdateUser;

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

        // Register Sannr validators
        CreateUserCommandValidator.Register();
        UpdateUserCommandValidator.Register();

        return services;
    }
}
