using GovDocs.Application.Abstractions.Messaging;
using GovDocs.Domain.Products;

namespace GovDocs.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(string Name, string Description, decimal Price)
    : ICommand<ProductId>;
