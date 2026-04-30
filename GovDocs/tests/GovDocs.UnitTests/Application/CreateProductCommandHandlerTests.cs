using GovDocs.Application.Products.Commands.CreateProduct;
using FluentAssertions;

namespace GovDocs.UnitTests.Application;

public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnProductId()
    {
        await using var context = TestDbContextFactory.Create();

        var handler = new CreateProductCommandHandler(context);
        var command = new CreateProductCommand("Widget", "A widget", 9.99m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().NotBeEmpty();
        context.Products.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithInvalidPrice_ShouldReturnFailure()
    {
        await using var context = TestDbContextFactory.Create();

        var handler = new CreateProductCommandHandler(context);
        var command = new CreateProductCommand("Widget", "A widget", -1m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        context.Products.Should().BeEmpty();
    }
}
