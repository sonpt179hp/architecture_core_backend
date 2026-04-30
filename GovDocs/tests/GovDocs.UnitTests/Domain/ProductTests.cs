using GovDocs.Domain.Products;
using GovDocs.Domain.Products.Errors;
using FluentAssertions;

namespace GovDocs.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var result = Product.Create("Widget", "A test widget", 9.99m);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Widget");
        result.Value.Price.Should().Be(9.99m);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldReturnNameEmptyError()
    {
        var result = Product.Create(string.Empty, "desc", 9.99m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NameEmpty);
    }

    [Fact]
    public void Create_WithZeroPrice_ShouldReturnInvalidPriceError()
    {
        var result = Product.Create("Widget", "desc", 0m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.InvalidPrice);
    }

    [Fact]
    public void Create_WithNegativePrice_ShouldReturnInvalidPriceError()
    {
        var result = Product.Create("Widget", "desc", -5m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.InvalidPrice);
    }

    [Fact]
    public void Update_WithValidData_ShouldSucceed()
    {
        var product = Product.Create("Widget", "desc", 9.99m).Value;

        var result = product.Update("Updated Widget", "new desc", 19.99m);

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("Updated Widget");
        product.Price.Should().Be(19.99m);
    }
}
