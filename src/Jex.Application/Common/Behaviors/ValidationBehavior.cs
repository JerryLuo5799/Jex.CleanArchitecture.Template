using FluentValidation;
using MediatR;

namespace Jex.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs FluentValidation validators before the handler.
/// Throws <see cref="Exceptions.ValidationException"/> when any rule fails.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new Exceptions.ValidationException(failures);

        return await next();
    }
}
