using GovDocs.Domain.Common;
using Mediator;

namespace GovDocs.Application.Abstractions.Messaging;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
