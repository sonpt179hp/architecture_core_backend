using GovDocs.Domain.Common;
using GovDocs.Domain.Primitives;
using GovDocs.Domain.Products.Errors;

namespace GovDocs.Domain.Products;

public sealed class Product : AggregateRoot<ProductId>
{
    private Product(
        ProductId id,
        string name,
        string description,
        decimal price,
        DateTime createdAt) : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        CreatedAt = createdAt;
    }

    // Required by EF Core
    private Product()
    {
    }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static Result<Product> Create(string name, string description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ProductErrors.NameEmpty;
        }

        if (price <= 0)
        {
            return ProductErrors.InvalidPrice;
        }

        var product = new Product(ProductId.New(), name, description, price, DateTime.UtcNow);
        return Result<Product>.Success(product);
    }

    public Result Update(string name, string description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ProductErrors.NameEmpty;
        }

        if (price <= 0)
        {
            return ProductErrors.InvalidPrice;
        }

        Name = name;
        Description = description;
        Price = price;
        return Result.Success();
    }

}
