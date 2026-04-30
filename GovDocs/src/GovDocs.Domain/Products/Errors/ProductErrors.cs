using GovDocs.Domain.Common;

namespace GovDocs.Domain.Products.Errors;

public static class ProductErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Product.NotFound",
        "The product with the specified identifier was not found.");

    public static readonly Error NameEmpty = Error.Validation(
        "Product.NameEmpty",
        "The product name cannot be empty.");

    public static readonly Error InvalidPrice = Error.Validation(
        "Product.InvalidPrice",
        "The product price must be greater than zero.");

    public static readonly Error AlreadyExists = Error.Conflict(
        "Product.AlreadyExists",
        "A product with the same identifier already exists.");
}
