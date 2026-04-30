using GovDocs.Domain.Common;
using FluentValidation;
using Mediator;

namespace GovDocs.Application.Abstractions.Behaviors;

public sealed class ValidationBehavior<TMessage, TResponse>(
    IEnumerable<IValidator<TMessage>> validators) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        CancellationToken cancellationToken,
        MessageHandlerDelegate<TMessage, TResponse> next)
    {
        if (!validators.Any())
        {
            return await next(message, cancellationToken);
        }

        var context = new ValidationContext<TMessage>(message);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
        {
            var errorDescriptions = string.Join("; ", failures.Select(f => f.ErrorMessage));
            var error = Error.Validation("Validation.Failed", errorDescriptions);

            // Attempt to construct a failure result of the expected response type
            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(error);
            }

            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(TResponse);
                var failureMethod = resultType.GetMethod(
                    "Failure",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    new[] { typeof(Error) });

                if (failureMethod is not null)
                {
                    return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
                }
            }
        }

        return await next(message, cancellationToken);
    }
}
