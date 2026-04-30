using GovDocs.Application.Abstractions.Data;
using GovDocs.Application.Abstractions.Messaging;
using GovDocs.Application.Products.Mappings;
using GovDocs.Domain.Common;
using GovDocs.Domain.Products;
using GovDocs.Domain.Products.Errors;
using Microsoft.EntityFrameworkCore;

namespace GovDocs.Application.Products.Queries.GetProductById;

internal sealed class GetProductByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetProductByIdQuery, GetProductByIdResponse>
{
    public async ValueTask<Result<GetProductByIdResponse>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(query.ProductId);
        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        return product.ToResponse();
    }
}
