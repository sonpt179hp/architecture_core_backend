# 09 – Error Handling & Exception Strategy Rules

**Nguồn:** `backend_core_technical_guidelines.md` §4.1 (Cross-Cutting Pipeline)

---

## Exception Hierarchy

```
Exception
└── AppException (base, abstract)
    ├── DomainException              ← Business rule violations (422)
    │   ├── DocumentAlreadyPublishedException
    │   └── InsufficientPermissionException
    ├── NotFoundException            ← Resource not found (404)
    ├── ConflictException            ← Concurrency / duplicate (409)
    └── InfrastructureException      ← External dependency failure (502/503)
```

---

## DO

1. **Định nghĩa exception hierarchy** rõ ràng theo layer:
   - `DomainException` — vi phạm business rule, đặt trong `Domain/Exceptions/`
   - `NotFoundException` — resource không tồn tại, đặt trong `Application/Exceptions/`
   - `ConflictException` — concurrency hoặc duplicate, đặt trong `Application/Exceptions/`
   - `InfrastructureException` — lỗi external (DB, broker, HTTP), đặt trong `Infrastructure/Exceptions/`

2. **Đăng ký Global Exception Middleware** trước tất cả middleware khác:
   ```csharp
   app.UseExceptionHandler(); // .NET 8 built-in IExceptionHandler
   // hoặc
   app.UseMiddleware<GlobalExceptionMiddleware>();
   ```

3. **Map từng exception type sang HTTP status code nhất quán:**
   | Exception | HTTP Status |
   |---|---|
   | `ValidationException` (FluentValidation) | 422 |
   | `DomainException` | 422 |
   | `NotFoundException` | 404 |
   | `ConflictException` / `DbUpdateConcurrencyException` | 409 |
   | `UnauthorizedAccessException` | 403 |
   | `InfrastructureException` | 502 |
   | `Exception` (unhandled) | 500 |

4. **Log exception với đầy đủ context** tại middleware level:
   ```csharp
   _logger.LogError(ex, "Unhandled exception for {Method} {Path} CorrelationId={CorrelationId}",
       context.Request.Method, context.Request.Path, context.Items["TraceId"]);
   ```

5. **Translate exception ở layer boundary** — Infrastructure exception không được bubble up thô vào Application:
   ```csharp
   catch (PostgresException ex) when (ex.SqlState == "23505")
   {
       throw new ConflictException("Record already exists", ex);
   }
   ```

6. **Dùng typed, named exceptions** — mỗi business rule violation có exception riêng để dễ xử lý có chủ đích.

## DON'T

1. **KHÔNG** throw `Exception` hay `ApplicationException` generic:
   ```csharp
   // ❌ WRONG
   throw new Exception("Document not found");
   // ✅ CORRECT
   throw new NotFoundException(nameof(Document), id);
   ```

2. **KHÔNG** log exception nhiều lần trong cùng call stack (log once tại middleware).

3. **KHÔNG** expose stack trace, inner exception details, hoặc connection string ra response body ở production:
   ```csharp
   // ❌ WRONG
   return Problem(detail: ex.ToString()); // lộ stack trace
   // ✅ CORRECT
   return Problem(detail: "An internal error occurred. TraceId: " + traceId);
   ```

4. **KHÔNG** dùng exception để điều khiển control flow thông thường:
   ```csharp
   // ❌ WRONG — dùng exception để check tồn tại
   try { var doc = _repo.GetById(id); }
   catch (NotFoundException) { return false; }
   // ✅ CORRECT
   var exists = await _repo.ExistsAsync(id, ct);
   ```

5. **KHÔNG** swallow exception mà không log:
   ```csharp
   // ❌ WRONG
   catch (Exception) { return null; }
   // ✅ CORRECT
   catch (Exception ex) { _logger.LogWarning(ex, "..."); return null; }
   ```

6. **KHÔNG** catch `OperationCanceledException` và log như Error — đây là hành vi bình thường khi client disconnect.

## Ví dụ minh họa

```csharp
// ── Domain/Exceptions/DomainException.cs
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception inner) : base(message, inner) { }
}

public class DocumentAlreadyPublishedException : DomainException
{
    public Guid DocumentId { get; }
    public DocumentAlreadyPublishedException(Guid documentId)
        : base($"Document '{documentId}' is already published and cannot be modified.")
        => DocumentId = documentId;
}

// ── Application/Exceptions/NotFoundException.cs
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object id)
        : base($"{entityName} with id '{id}' was not found.") { }
}

// ── Api/Middleware/GlobalExceptionHandler.cs (.NET 8 IExceptionHandler)
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var traceId = context.Items["TraceId"]?.ToString() ?? Activity.Current?.TraceId.ToString();

        var (statusCode, title) = exception switch
        {
            OperationCanceledException    => (0, null),          // ignore, client disconnected
            ValidationException vex       => (422, "Validation Failed"),
            DomainException               => (422, "Business Rule Violation"),
            NotFoundException             => (404, "Not Found"),
            ConflictException             => (409, "Conflict"),
            UnauthorizedAccessException   => (403, "Forbidden"),
            InfrastructureException       => (502, "Upstream Error"),
            _                            => (500, "Internal Server Error"),
        };

        if (statusCode == 0) return false; // let ASP.NET handle cancellation

        _logger.LogError(exception,
            "Exception {ExceptionType} for {Method} {Path} TraceId={TraceId}",
            exception.GetType().Name, context.Request.Method, context.Request.Path, traceId);

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = statusCode == 500 ? $"TraceId: {traceId}" : exception.Message,
            Extensions = { ["traceId"] = traceId }
        }, ct);

        return true;
    }
}

// ── Program.cs
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
app.UseExceptionHandler();
```
