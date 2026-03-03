using MediatR;

namespace Jex.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs Sannr validators before the handler.
/// Throws <see cref="Exceptions.ValidationException"/> when any rule fails.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!Sannr.AspNetCore.SannrValidatorRegistry.TryGetValidator(typeof(TRequest), out _))
            return await next();

        var result = await Sannr.AspNetCore.SannrValidatorRegistry.ValidateAsync(request);

        if (result is null || result.IsValid)
            return await next();

        throw new Exceptions.ValidationException(result.Errors);
    }
}
