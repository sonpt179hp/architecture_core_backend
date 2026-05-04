# 09 – Error Handling & Exception Strategy Rules

**Nguồn:** `backend_core_technical_guidelines.md` §4.1 (Cross-Cutting Pipeline)

---

## Error Strategy: Result Pattern First

Dự án dùng **Result Pattern** làm primary error handling mechanism. Exception chỉ dùng cho
unhandled/system errors.

```
✅ Command Handler → return Result<T>.Failure(error)
✅ Query Handler → return Error.NotFound(...)
✅ Validator (FluentValidation) → ValidationBehavior trả Error.Validation(...)
✅ Domain Entity Factory → return Result<T>.Failure(domainError)
❌ KHÔNG dùng exception cho business rule violations
```

**GlobalExceptionHandler chỉ catch:**
- Unhandled exceptions (bugs, null refs)
- External service failures (DB down, Redis down)
- KHÔNG catch Result failures — chúng được handle ở Controller qua `result.ToActionResult()`

---

## Result Pattern Types

```
Domain/Common/
├── Result.cs          # Base class: IsSuccess, IsFailure, Error
├── ResultT.cs         # Result<T>: Value property
├── Error.cs           # Error record struct: Code, Description, Type
└── ErrorType.cs      # enum: Failure | Validation | NotFound | Conflict | Unauthorized
```

---

## DO

1. **Dùng Result Pattern** cho mọi handler:
   ```csharp
   public async ValueTask<Result> Handle(DeleteDocumentCommand cmd, CancellationToken ct)
   {
       if (result.IsFailure)
           return Result.Failure(DocumentErrors.NotFound);
       // ...
       return Result.Success();
   }
   ```

2. **Định nghĩa predefined errors** trong Domain:
   ```csharp
   public static class DocumentErrors
   {
       public static readonly Error NotFound = Error.NotFound(
           "Document.NotFound",
           "The document with the specified identifier was not found.");

       public static readonly Error AlreadyPublished = Error.Validation(
           "Document.AlreadyPublished",
           "The document has already been published and cannot be modified.");

       public static readonly Error ConcurrencyConflict = Error.Conflict(
           "Document.ConcurrencyConflict",
           "The document was modified by another user. Please refresh and retry.");
   }
   ```

3. **Map Error.Type sang HTTP status** trong `ResultExtensions`:
   | ErrorType | HTTP Status | Mô tả |
   |---|---|---|
   | `NotFound` | 404 | Resource không tồn tại |
   | `Validation` | 422 | Business validation thất bại (Result pattern) |
   | `Conflict` | 409 | Concurrency conflict hoặc trùng dữ liệu |
   | `Unauthorized` | 401 | Chưa xác thực |
   | `Forbidden` | 403 | Không có quyền truy cập tài nguyên |
   | `Failure` | 500 | Lỗi hệ thống không xử lý được |
   | `ServiceUnavailable` | 503 | Dependency không available |

4. **GlobalExceptionHandler** chỉ catch thật sự exceptions:
   ```csharp
   // Chỉ log và trả ProblemDetails cho unhandled exceptions
   public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
   {
       _logger.LogError(ex, "Unhandled exception for {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
       // Trả 500 Internal Server Error
   }
   ```

5. **Validation failures (FluentValidation)** trả 400 Bad Request cho input validation.
   **Business validation (Result pattern)** trả 422 Unprocessable Entity.

## DON'T

1. **KHÔNG** dùng exception cho business rule violations:
   ```csharp
   // ❌ WRONG — dùng exception cho business rule
   if (!doc.CanPublish()) throw new DocumentAlreadyPublishedException(doc.Id);
   // ✅ CORRECT — dùng Result pattern
   var result = doc.Publish(publishedBy);
   if (result.IsFailure) return result;
   ```

2. **KHÔNG** throw generic `Exception` hay `ApplicationException`:
   ```csharp
   // ❌ WRONG
   throw new Exception("Document not found");
   // ✅ CORRECT
   return DocumentErrors.NotFound;
   ```

3. **KHÔNG** log exception nhiều lần trong cùng call stack (log once tại middleware).

4. **KHÔNG** expose stack trace, inner exception details ra response body ở production.

5. **KHÔNG** dùng exception để điều khiển control flow:
   ```csharp
   // ❌ WRONG
   try { var doc = _repo.GetById(id); }
   catch (NotFoundException) { return false; }
   // ✅ CORRECT
   var result = await _queryHandler.Handle(new GetDocumentByIdQuery(id), ct);
   ```

6. **KHÔNG** catch `OperationCanceledException` và log như Error — đây là hành vi bình thường khi client disconnect.

7. **KHÔNG** dùng Exception hierarchy (DomainException, NotFoundException) cho business errors — dùng `Error` static constants.

## Ví dụ minh họa

```csharp
// ── Domain/Common/Error.cs
namespace {Namespace}.Domain.Common;

public enum ErrorType { Failure, Validation, NotFound, Conflict, Unauthorized, Forbidden, ServiceUnavailable }

public readonly struct Error
{
    public ErrorType Type { get; }
    public string Code { get; }
    public string Description { get; }

    private Error(ErrorType type, string code, string description)
    {
        Type = type;
        Code = code;
        Description = description;
    }

    public static Error NotFound(string code, string description) =>
        new(ErrorType.NotFound, code, description);

    public static Error Validation(string code, string description) =>
        new(ErrorType.Validation, code, description);

    public static Error Conflict(string code, string description) =>
        new(ErrorType.Conflict, code, description);

    public static Error Failure(string code, string description) =>
        new(ErrorType.Failure, code, description);

    public static Error Forbidden(string code, string description) =>
        new(ErrorType.Forbidden, code, description);

    public static Error ServiceUnavailable(string code, string description) =>
        new(ErrorType.ServiceUnavailable, code, description);
}

// ── Domain/Common/Result.cs
namespace {Namespace}.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, default);
    public static Result Failure(Error error) => new(false, error);
}

public class Result<T> : Result
{
    public T Value { get; }

    private Result(bool isSuccess, Error error, T value)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, default, value);
    public static new Result<T> Failure(Error error) => new(false, error, default!);
}
```

```csharp
// ── Domain entity dùng Result
public class Document : AggregateRoot<DocumentId>
{
    public Result Publish(UserId publishedBy)
    {
        if (Status == DocumentStatus.Published)
            return DocumentErrors.AlreadyPublished;

        Status = DocumentStatus.Published;
        RaiseDomainEvent(new DocumentPublishedEvent(Id, TenantId, publishedBy));
        return Result.Success();
    }
}
```

```csharp
// ── Application/Extensions/ResultExtensions.cs
namespace {Namespace}.Application.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);
        return MapErrorToActionResult(result.Error);
    }

    public static IActionResult ToActionResult(this Error error) =>
        MapErrorToActionResult(error);

    private static IActionResult MapErrorToActionResult(Error error) =>
        error.Type switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(CreateProblemDetails(error, 404, "Not Found")),
            ErrorType.Validation => new UnprocessableEntityObjectResult(CreateValidationProblemDetails(error)),
            ErrorType.Conflict => new ConflictObjectResult(CreateProblemDetails(error, 409, "Conflict")),
            ErrorType.Unauthorized => new UnauthorizedObjectResult(CreateProblemDetails(error, 401, "Unauthorized")),
            ErrorType.Forbidden => new ForbidResult(CreateProblemDetails(error, 403, "Forbidden")),
            ErrorType.ServiceUnavailable => new ObjectResult(CreateProblemDetails(error, 503, "Service Unavailable")) { StatusCode = 503 },
            _ => new ObjectResult(CreateProblemDetails(error, 500, "Internal Server Error")) { StatusCode = 500 }
        };

    private static ProblemDetails CreateProblemDetails(Error error, int status, string title) =>
        new()
        {
            Status = status,
            Title = title,
            Type = $"https://httpstatuses.com/{status}",
            Extensions = { ["errorCode"] = error.Code, ["errorDescription"] = error.Description }
        };

    private static ValidationProblemDetails CreateValidationProblemDetails(Error error) =>
        new(new Dictionary<string, string[]> { { error.Code, new[] { error.Description } } })
        {
            Status = 400,
            Title = "Validation Failed",
            Type = "https://httpstatuses.com/400"
        };
}
```

```csharp
// ── Handler dùng Result
internal sealed class DeleteDocumentCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<DeleteDocumentCommand>
{
    public async ValueTask<Result> Handle(DeleteDocumentCommand cmd, CancellationToken ct)
    {
        var doc = await dbContext.Documents
            .FirstOrDefaultAsync(d => d.Id == new DocumentId(cmd.Id), ct);

        if (doc is null)
            return DocumentErrors.NotFound;

        var result = doc.Delete();
        if (result.IsFailure)
            return result;

        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

```csharp
// ── GlobalExceptionHandler — chỉ cho unhandled exceptions
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (ex is OperationCanceledException) return false;

        _logger.LogError(ex, "Unhandled exception for {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
        var traceId = Activity.Current?.TraceId.ToString();

        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 500,
            Title = "Internal Server Error",
            Detail = Environment.IsDevelopment() ? ex.Message : "An error occurred.",
            Type = "https://httpstatuses.com/500",
            Extensions = { ["traceId"] = traceId ?? "" }
        }, ct);

        return true;
    }
}
```
