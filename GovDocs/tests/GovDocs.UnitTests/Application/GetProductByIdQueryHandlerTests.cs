using GovDocs.Application.Abstractions.Data;
using GovDocs.Application.Products.Queries.GetProductById;
using GovDocs.Domain.Products;
using GovDocs.Domain.Products.Errors;
using FluentAssertions;
using NSubstitute;

namespace GovDocs.UnitTests.Application;

public class GetProductByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenProductExists_ShouldReturnMappedResponse()
    {
        // Arrange: use real in-memory context via TestDbContextFactory
        await using var context = TestDbContextFactory.Create();
        var product = Product.Create("Widget", "desc", 9.99m).Value;
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var handler = new GetProductByIdQueryHandler(context);

        var result = await handler.Handle(
            new GetProductByIdQuery(product.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Widget");
        result.Value.Price.Should().Be(9.99m);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFoundError()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new GetProductByIdQueryHandler(context);

        var result = await handler.Handle(
            new GetProductByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFound);
    }
}
