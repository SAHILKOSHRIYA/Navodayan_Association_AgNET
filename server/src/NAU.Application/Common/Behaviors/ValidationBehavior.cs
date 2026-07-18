using FluentValidation;
using MediatR;

namespace NAU.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior: every command/query with registered FluentValidation
/// validators is validated before its handler runs (Phase 2 §7 — input validation).
/// Throws <see cref="ValidationException"/>, mapped to HTTP 400 by the API middleware.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();
            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
