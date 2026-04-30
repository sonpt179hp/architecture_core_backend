using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace GovDocs.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiController(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;
}
