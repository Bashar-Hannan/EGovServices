using EGovServices.Application.Common;
using FluentValidation;
using MediatR;

namespace EGovServices.Application.Common.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior that runs FluentValidation
/// before every Command/Query Handler.
///
/// Flow:
/// Request → ValidationBehavior → Handler → Response
///
/// If validation fails → returns Result.Failure with error message
/// and the Handler is never called.
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
        // If no validators registered for this request → skip
        if (!validators.Any())
            return await next();

        // Run all validators
        var context = new ValidationContext<TRequest>(request);

        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        // Build Arabic error message from first failure
        var errorMessage = failures.First().ErrorMessage;

        // Return Result.Failure using reflection (works with Result<T>)
        var responseType = typeof(TResponse);

        if (responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var innerType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(Result<>)
                .MakeGenericType(innerType)
                .GetMethod("Failure", new[] { typeof(string) })!;

            return (TResponse)failureMethod.Invoke(null, [errorMessage])!;
        }

        // Fallback for non-Result responses
        throw new ValidationException(failures);
    }
}
