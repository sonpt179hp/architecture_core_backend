# Skill: Setup Error Handling Infrastructure

## Purpose

Dựng hạ tầng xử lý lỗi nhất quán: Error pattern (Result/Error/ErrorType),
GlobalExceptionHandler (IExceptionHandler), ProblemDetails factory, và mapping Error → HTTP status.

## Architecture: Error-First, Không Exception-Driven

Dự án dùng **Result pattern** làm primary error handling mechanism:

```
✅ Command Handler → return Result<T>.Failure(error)
✅ Query Handler → return Error.NotFound(...)
✅ Validator (FluentValidation) → ValidationBehavior trả Error.Validation(...)
✅ Domain Entity Factory → return Result<T>.Failure(domainError)
```

**GlobalExceptionHandler chỉ catch:**
- Unhandled exceptions (bugs, null refs)
- External service failures (DB down, Redis down)
- **KHÔNG** catch Result failures — chúng được handle ở Controller qua `result.ToActionResult()`

## Convention Mapping

| Artifact | Convention | Pattern |
|---|---|---|
| **Error Pattern** | `Result` + `Result<T>` + `Error` + `ErrorType` | Railway-oriented |
| **Error Type** | `enum ErrorType { Failure, Validation, NotFound, Conflict, Unauthorized }` | |
| **Error Factory** | `Error.{Type}(code, description)` | Static factory methods |
| **GlobalExceptionHandler** | Implement `IExceptionHandler` (.NET 8+) | |
| **Result Extensions** | `Result<T>.ToActionResult()` + `Error.ToActionResult()` | |
| **HTTP Mapping** | NotFound→404, Validation→400, Conflict→409, Unauthorized→401, Failure→500 | |

## Project Structure

```
src/
├── Domain/
│   └── Common/
│       ├── Result.cs       ← Result base class
│       ├── ResultT.cs      ← Result<T>
│       ├── Error.cs        ← Error record struct
│       └── ErrorType.cs    ← ErrorType enum
└── Api/
    ├── Middleware/
    │   └── GlobalExceptionHandler.cs
    ├── Extensions/
    │   └── ResultExtensions.cs
    └── Controllers/
        └── ApiController.cs
```

## Instructions

**Input:** Tên bounded context.

### Step 1: Review Existing Error Pattern

Kiểm tra các file đã tồn tại:

```
- Domain/Common/Result.cs       ← Result base class
- Domain/Common/ResultT.cs      ← Result<T>
- Domain/Common/Error.cs        ← Error record struct
- Domain/Common/ErrorType.cs   ← ErrorType enum
```

### Step 2: Create GlobalExceptionHandler

`src/{Solution}/Api/Middleware/GlobalExceptionHandler.cs`:

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace {Namespace}.Api.Middleware;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred");

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Type = "https://httpstatuses.com/500"
        };

        if (environment.IsDevelopment())
        {
            problemDetails.Detail = exception.Message;
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
```

### Step 3: Create ResultExtensions

`src/{Solution}/Api/Extensions/ResultExtensions.cs`:

```csharp
using {Namespace}.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace {Namespace}.Api.Extensions;

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
```

### Step 4: Create ApiController Base

`src/{Solution}/Api/Controllers/ApiController.cs`:

```csharp
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace {Namespace}.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiController(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;
}
```

### Step 5: Register in Program.cs

```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

app.UseExceptionHandler(); // Đặt ĐẦU TIÊN trong middleware pipeline
```

## HTTP Status Code Mapping

| ErrorType | HTTP Status | ProblemDetails Type |
|---|---|---|
| `NotFound` | 404 | Not Found |
| `Validation` | 400 | Bad Request (ValidationProblemDetails) |
| `Conflict` | 409 | Conflict |
| `Unauthorized` | 401 | Unauthorized |
| `Failure` | 500 | Internal Server Error |

## Checklist

- [ ] `GlobalExceptionHandler` implement `IExceptionHandler`, log exception, trả ProblemDetails
- [ ] `ResultExtensions` map `Error.Type` → đúng HTTP status code
- [ ] Controller dùng `result.ToActionResult()` hoặc `result.Match(onSuccess, error => error.ToActionResult())`
- [ ] Validation failures trả 400 Bad Request với field-level errors
- [ ] Chỉ expose exception message ở Development environment

## Edge Cases

- Custom exception mapping (Stripe, Twilio...): thêm case trong GlobalExceptionHandler.
- .NET 7 (không có IExceptionHandler): dùng `UseMiddleware<GlobalExceptionMiddleware>()`.
- Correlation ID: thêm vào ProblemDetails Extensions.

## References

- `ai-rules/09-error-handling.md`
- `ai-rules/04-api-contract.md`
- `ai-rules/06-observability.md`
- `ai-rules/10-dependency-injection.md`
