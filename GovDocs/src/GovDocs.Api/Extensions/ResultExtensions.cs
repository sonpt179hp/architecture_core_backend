using GovDocs.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace GovDocs.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return MapErrorToActionResult(result.Error);
    }

    public static IActionResult ToActionResult(this Error error) =>
        MapErrorToActionResult(error);

    private static IActionResult MapErrorToActionResult(Error error)
    {
        return error.Type switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(
                CreateProblemDetails(error, StatusCodes.Status404NotFound, "Not Found")),

            ErrorType.Validation => new BadRequestObjectResult(
                CreateValidationProblemDetails(error)),

            ErrorType.Conflict => new ConflictObjectResult(
                CreateProblemDetails(error, StatusCodes.Status409Conflict, "Conflict")),

            ErrorType.Unauthorized => new UnauthorizedObjectResult(
                CreateProblemDetails(error, StatusCodes.Status401Unauthorized, "Unauthorized")),

            _ => new ObjectResult(
                CreateProblemDetails(error, StatusCodes.Status500InternalServerError, "Internal Server Error"))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
    }

    private static ProblemDetails CreateProblemDetails(Error error, int status, string title) =>
        new()
        {
            Status = status,
            Title = title,
            Type = $"https://httpstatuses.com/{status}",
            Extensions = { ["errorCode"] = error.Code, ["errorDescription"] = error.Description }
        };

    private static ValidationProblemDetails CreateValidationProblemDetails(Error error) =>
        new(new Dictionary<string, string[]>
        {
            { error.Code, new[] { error.Description } }
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Type = "https://httpstatuses.com/400"
        };
}
