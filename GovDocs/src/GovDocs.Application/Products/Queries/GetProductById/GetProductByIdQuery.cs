using GovDocs.Application.Abstractions.Messaging;
using GovDocs.Application.Products.Queries.GetProductById;

namespace GovDocs.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<GetProductByIdResponse>;
