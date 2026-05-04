---
name: error-handling
description: >
  Error handling strategy for this .NET 8 LTS Clean Architecture project. Uses an
  Exception hierarchy (DomainException→422, NotFoundException→404, ConflictException→409,
  InfrastructureException→502) mapped by GlobalExceptionHandler to ProblemDetails (RFC 9457).
  NOT the Result pattern.
  Load this skill when implementing error handling, designing API error contracts, or when
  the user mentions "error handling", "exception", "DomainException", "NotFoundException",
  "ConflictException", "GlobalExceptionHandler", "ProblemDetails", "validation",
  "FluentValidation", "error response", or "RFC 9457".
---

# Error Handling

## Core Principles

1. **Exception hierarchy, not Result pattern** — This stack uses typed exceptions to signal
   expected failures. `DomainException`, `NotFoundException`, `ConflictException`, and
   `InfrastructureException` propagate up the call stack and are caught once in
   `GlobalExceptionHandler`. No `Result<T>` wrappers. See ADR-002.
2. **GlobalExceptionHandler maps all exceptions to ProblemDetails** — One place in the
   middleware pipeline converts every exception to an RFC 9457 `ProblemDetails` response
   with the correct HTTP status code. Handlers and repositories never return error objects.
3. **Translate at layer boundaries** — Infrastructure exceptions (e.g. `PostgresException`)
   are caught at the repository level and re-thrown as domain exceptions. Domain logic never
   sees raw `SqlException` or `DbException`.
4. **Log once at the middleware** — `GlobalExceptionHandler` is the single logging site for
   unhandled exceptions. Do not log-and-rethrow at intermediate layers.
5. **Reserve exceptions for failure paths** — Do not use exceptions for control flow in the
   happy path. Throw when something is genuinely wrong; let it propagate.

## Patterns

### Exception Hierarchy

```csharp
// Domain/Exceptions/AppException.cs
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
    protected AppException(string message, Exception inner) : base(message, inner) { }
}

// Domain/Exceptions/DomainException.cs — 422 Unprocessable Entity
// Use when a business rule is violated (invalid state transition, invariant breach).
public sealed class DomainException : AppException
{
    public DomainException(string message) : base(message) { }
}

// Domain/Exceptions/NotFoundException.cs — 404 Not Found
// Use when an entity that must exist is absent.
public sealed class NotFoundException : AppException
{
    public NotFoundException(string entity, object id)
        : base($"{entity} with ID '{id}' was not found.") { }
}

// Domain/Exceptions/ConflictException.cs — 409 Conflict
// Use for optimistic concurrency failures or duplicate key violations.
public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}

// Domain/Exceptions/InfrastructureException.cs — 502 Bad Gateway
// Use when an external dependency (DB, message broker, external API) is unavailable.
public sealed class InfrastructureException : AppException
{
    public InfrastructureException(string message, Exception inner)
        : base(message, inner) { }
}
```

### GlobalExceptionHandler

```csharp
// Api/Middleware/GlobalExceptionHandler.cs
internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        // OperationCanceledException is not an error — client disconnected
        if (exception is OperationCanceledException)
            return false;

        var (statusCode, title) = exception switch
        {
            NotFoundException     => (StatusCodes.Status404NotFound,              "Resource not found"),
            DomainException       => (StatusCodes.Status422UnprocessableEntity,   "Business rule violation"),
            ConflictException     => (StatusCodes.Status409Conflict,              "Conflict"),
            InfrastructureException => (StatusCodes.Status502BadGateway,          "Upstream dependency failure"),
            _                     => (StatusCodes.Status500InternalServerError,   "An unexpected error occurred")
        };

        // Log 5xx as Error, 4xx as Warning — 502 is upstream fault, still Error
        if (statusCode >= 500)
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            logger.LogWarning(exception, "Expected exception: {Message}", exception.Message);

        context.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails =
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message,
                Extensions =
                {
                    ["traceId"] = context.TraceIdentifier
                }
            }
        });
    }
}

// Api/Program.cs — registration
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();
app.UseExceptionHandler();
```

### Infrastructure Boundary Translation

Catch raw database exceptions at the repository level and translate to domain exceptions so
the Application layer is never exposed to `PostgresException` or `DbUpdateException`.

```csharp
// Infrastructure/Repositories/EfDocumentRepository.cs
internal sealed class EfDocumentRepository(AppDbContext db) : IDocumentRepository
{
    public async Task AddAsync(Document document, CancellationToken ct = default)
    {
        db.Documents.Add(document);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
        {
            // Unique constraint violation
            if (pg.SqlState == PostgresErrorCodes.UniqueViolation)
                throw new ConflictException($"A document with reference '{document.Reference}' already exists.");

            // Other DB error — treat as infrastructure failure
            throw new InfrastructureException("Database write failed.", ex);
        }
    }

    public async Task<Document> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Documents.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(Document), id);
    }
}
```

### Controller Error Handling (Clean path — exceptions propagate)

Controllers stay clean. No try/catch inside handlers or controllers — exceptions bubble to
`GlobalExceptionHandler`.

```csharp
// Application/Documents/Commands/CreateDocumentCommandHandler.cs
internal sealed class CreateDocumentCommandHandler(
    IDocumentRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateDocumentCommand, Guid>
{
    public async Task<Guid> Handle(CreateDocumentCommand command, CancellationToken ct)
    {
        // Domain validation — throws DomainException (→ 422) if rule violated
        var document = Document.Create(command.TenantId, command.Title, command.Content);

        await repository.AddAsync(document, ct);   // throws ConflictException (→ 409) on duplicate
        await unitOfWork.SaveChangesAsync(ct);      // throws InfrastructureException (→ 502) on DB failure

        return document.Id;
    }
}

// Api/Controllers/DocumentsController.cs
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class DocumentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateDocumentRequest request, CancellationToken ct)
    {
        var id = await sender.Send(request.ToCommand(), ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
        // Any exception propagates → GlobalExceptionHandler → ProblemDetails
    }
}
```

### ProblemDetails Response Shape

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Business rule violation",
  "status": 422,
  "detail": "Document cannot transition from 'Draft' to 'Archived' without approval.",
  "traceId": "00-4f3e2a1b9c8d7e6f-a1b2c3d4e5f6g7h8-00"
}
```

## Anti-patterns

### Don't Throw Generic `Exception`

```csharp
// BAD — no semantic meaning, maps to 500
throw new Exception("Document not found");

// GOOD — typed exception maps to correct HTTP status
throw new NotFoundException(nameof(Document), id);
```

### Don't Expose Stack Traces in Production

```csharp
// BAD — GlobalExceptionHandler leaks internals
problem.Detail = exception.StackTrace; // never do this

// GOOD — detail contains only the message; traceId allows correlation
problem.Detail = exception.Message;
problem.Extensions["traceId"] = context.TraceIdentifier;
```

### Don't Use Exceptions for Happy-Path Control Flow

```csharp
// BAD — throwing to signal "nothing to do" in normal flow
public async Task Handle(ArchiveDocumentCommand cmd, CancellationToken ct)
{
    if (document.Status == DocumentStatus.Archived)
        throw new DomainException("Already archived"); // this IS a real violation — OK
                                                        // but don't throw just to exit early
}

// GOOD — guard at domain level for real invariants; use early return for trivial cases
if (document.Status == DocumentStatus.Archived) return; // idempotent, no exception needed
```

### Don't Swallow Exceptions

```csharp
// BAD — silent failure, debugging nightmare
try { await repository.AddAsync(document, ct); }
catch (Exception) { /* ignore */ }

// GOOD — let it propagate; GlobalExceptionHandler logs and returns ProblemDetails
await repository.AddAsync(document, ct);
```

### Don't Treat `OperationCanceledException` as an Error

```csharp
// BAD — logging a 499 (client disconnect) as an application error
catch (OperationCanceledException ex)
{
    logger.LogError(ex, "Request cancelled"); // noisy, misleading
    return Result.Failure("Cancelled");
}

// GOOD — return false in GlobalExceptionHandler to skip it; ASP.NET handles 499 automatically
if (exception is OperationCanceledException) return false;
```

### Don't Log-and-Rethrow at Multiple Layers

```csharp
// BAD — same exception logged 3 times with duplicated noise
catch (PostgresException ex)
{
    logger.LogError(ex, "DB error in repository"); // logged here...
    throw new InfrastructureException("DB failed", ex);
}
// ... then GlobalExceptionHandler logs it again

// GOOD — translate at boundary, log once in GlobalExceptionHandler
throw new InfrastructureException("Database write failed.", ex); // no local log
```

## Decision Guide

| Scenario | Exception to Throw | HTTP Status |
|----------|--------------------|-------------|
| Entity not found | `NotFoundException` | 404 |
| Business rule / invariant violation | `DomainException` | 422 |
| Duplicate key / optimistic concurrency | `ConflictException` | 409 |
| External dependency unavailable | `InfrastructureException` | 502 |
| Input validation failure (FluentValidation) | `ValidationException` (auto-mapped) | 400 |
| Client cancelled request | `OperationCanceledException` | 499 (skip) |
| Bug / unexpected error | Let unhandled `Exception` propagate | 500 |
