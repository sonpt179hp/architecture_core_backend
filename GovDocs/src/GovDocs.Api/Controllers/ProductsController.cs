using GovDocs.Api.Controllers;
using GovDocs.Api.Extensions;
using GovDocs.Application.Products.Commands.CreateProduct;
using GovDocs.Application.Products.Queries.GetProductById;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace GovDocs.Api.Controllers;

/// <summary>
/// Manages product resources.
/// </summary>
public sealed class ProductsController(ISender sender) : ApiController(sender)
{
    /// <summary>
    /// Gets a product by its unique identifier.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The product details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetProductByIdQuery(id), ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="command">The create product request body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created with location header pointing to the new product.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand command,
        CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return result.Match(
            id => CreatedAtAction(nameof(GetById), new { id = id.Value }, null),
            error => error.ToActionResult());
    }
}
