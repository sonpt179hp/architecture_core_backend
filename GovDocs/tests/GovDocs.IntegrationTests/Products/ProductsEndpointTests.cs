using System.Net;
using System.Net.Http.Json;
using GovDocs.Application.Products.Commands.CreateProduct;
using GovDocs.Application.Products.Queries.GetProductById;
using GovDocs.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace GovDocs.IntegrationTests.Products;

[Trait("Category", "Integration")]
public sealed class ProductsEndpointTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;

    public ProductsEndpointTests(WebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidProduct_ShouldReturn201Created()
    {
        var command = new CreateProductCommand("Integration Widget", "desc", 29.99m);

        var response = await _client.PostAsJsonAsync("/api/products", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_WithEmptyName_ShouldReturn400BadRequest()
    {
        var command = new CreateProductCommand(string.Empty, "desc", 9.99m);

        var response = await _client.PostAsJsonAsync("/api/products", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ExistingProduct_ShouldReturn200WithBody()
    {
        // Arrange: create a product first
        var createCommand = new CreateProductCommand("Fetchable Widget", "desc", 14.99m);
        var createResponse = await _client.PostAsJsonAsync("/api/products", createCommand);
        createResponse.EnsureSuccessStatusCode();

        var location = createResponse.Headers.Location!;

        // Act
        var getResponse = await _client.GetAsync(location);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadFromJsonAsync<GetProductByIdResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Fetchable Widget");
    }

    [Fact]
    public async Task Get_NonExistentProduct_ShouldReturn404()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
