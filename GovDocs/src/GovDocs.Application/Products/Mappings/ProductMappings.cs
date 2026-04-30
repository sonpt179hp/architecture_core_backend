using GovDocs.Application.Products.Queries.GetProductById;
using GovDocs.Domain.Products;

namespace GovDocs.Application.Products.Mappings;

public static class ProductMappings
{
    public static GetProductByIdResponse ToResponse(this Product product) =>
        new(
            product.Id.Value,
            product.Name,
            product.Description,
            product.Price,
            product.CreatedAt,
            product.UpdatedAt);
}
