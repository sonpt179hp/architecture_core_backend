using GovDocs.Domain.Common;
using Mediator;

namespace GovDocs.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
