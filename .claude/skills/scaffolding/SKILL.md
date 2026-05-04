---
name: scaffolding
description: >
  Code scaffolding patterns for .NET 8 Clean Architecture features, entities, and tests.
  Default architecture is Clean Architecture with Controllers, MediatR, and Dapper reads.
  Generates complete feature slices: Command+Handler+Validator, Dapper query handler,
  EF entity configuration, Controller action, Integration test, and Unit test.
  Load when: "scaffold", "create feature", "add feature", "new endpoint", "generate",
  "add entity", "new entity", "scaffold test", "add module", "create command",
  "create query", "create handler", "generate use case".
---

# Scaffolding (.NET 8 Clean Architecture)

## Core Principles

1. **Read the folder structure first** — Before generating any file, scan existing paths with Glob to confirm the bounded context name, folder conventions, and existing base classes. Never assume.
2. **Complete feature slices** — A command or query is not done until the Command/Query record, Handler, Validator, Controller action, and at least one test exist. Scaffold all at once.
3. **Write path = EF Core, read path = Dapper** — Commands use `IUnitOfWork` + domain repositories. Queries use `IDbConnection` with raw SQL and map directly to DTOs. No EF Core on read path.
4. **Records over classes** — Commands, Queries, DTOs, and Domain Events are `record` types. Aggregates and Entities are `class` types with `private set`.
5. **Ask before overwriting** — If any target file already exists, list it and ask the user to confirm overwrite before touching it.

## Patterns

### Scaffold a CQRS Command (Write Operation)

Use for: create, update, delete, publish, approve, archive — any state-changing operation.

**File layout** (`src/{BoundedContext}/Application/Features/{UseCase}/`):

```csharp
// {UseCase}Command.cs
public sealed record PublishDocumentCommand(Guid DocumentId, Guid TenantId)
    : IRequest<Unit>;

// {UseCase}CommandValidator.cs
public sealed class PublishDocumentCommandValidator : AbstractValidator<PublishDocumentCommand>
{
    public PublishDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        // Format/required validation only — NO business rules here
    }
}

// {UseCase}CommandHandler.cs
internal sealed class PublishDocumentCommandHandler(
    IDocumentRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<PublishDocumentCommand, Unit>
{
    public async Task<Unit> Handle(PublishDocumentCommand command, CancellationToken ct)
    {
        var document = await repository.GetByIdAsync(command.DocumentId, ct)
            ?? throw new NotFoundException(nameof(Document), command.DocumentId);

        document.Publish(currentUser.UserId); // domain method raises DomainEvent + validates invariants

        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
```

**Controller action** (add to `Api/Controllers/{BoundedContext}Controller.cs`):

```csharp
[HttpPost("{id:guid}/publish")]
[Authorize(Policy = "documents:publish")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
{
    await sender.Send(new PublishDocumentCommand(id, currentUser.TenantId), ct);
    return NoContent();
}

// For POST that creates a new resource — include Idempotency-Key
[HttpPost]
[Authorize(Policy = "documents:create")]
[ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
public async Task<IActionResult> Create(
    CreateDocumentRequest request,
    [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
    CancellationToken ct)
{
    var id = await sender.Send(request.ToCommand(idempotencyKey), ct);
    return CreatedAtAction(nameof(GetById), new { id }, id);
}
```

### Scaffold a CQRS Query (Read Operation)

Use for: get by ID, list with pagination, search — any read-only operation.

**File layout** (`src/{BoundedContext}/Application/Features/{QueryName}/`):

```csharp
// {QueryName}Query.cs
public sealed record GetDocumentListQuery(
    Guid TenantId,
    string? Keyword,
    int Page,
    int PageSize) : IRequest<PagedResult<DocumentSummaryDto>>;

// {QueryName}Dto.cs
public sealed record DocumentSummaryDto(
    Guid Id,
    string Title,
    string Status,
    DateTimeOffset CreatedAt);

// {QueryName}QueryValidator.cs
public sealed class GetDocumentListQueryValidator : AbstractValidator<GetDocumentListQuery>
{
    public GetDocumentListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

// {QueryName}QueryHandler.cs — Dapper only, no EF Core
internal sealed class GetDocumentListQueryHandler(
    IDbConnection db,
    ITenantContext tenant) : IRequestHandler<GetDocumentListQuery, PagedResult<DocumentSummaryDto>>
{
    public async Task<PagedResult<DocumentSummaryDto>> Handle(
        GetDocumentListQuery query, CancellationToken ct)
    {
        const string sql = """
            SELECT
                id,
                title,
                status,
                created_at,
                COUNT(*) OVER() AS total_count
            FROM documents
            WHERE tenant_id = @TenantId          -- REQUIRED: always scope to tenant
              AND (@Keyword IS NULL OR title ILIKE '%' || @Keyword || '%')
            ORDER BY created_at DESC
            LIMIT @PageSize
            OFFSET @Offset
            """;

        var rows = await db.QueryAsync<DocumentSummaryDapperRow>(sql, new
        {
            tenant.TenantId,
            query.Keyword,
            query.PageSize,
            Offset = (query.Page - 1) * query.PageSize
        });

        var items = rows.ToList();
        var total = items.Count > 0 ? items[0].TotalCount : 0;

        return new PagedResult<DocumentSummaryDto>(
            items.Select(r => new DocumentSummaryDto(r.Id, r.Title, r.Status, r.CreatedAt)).ToList(),
            total, query.Page, query.PageSize);
    }

    private sealed record DocumentSummaryDapperRow(
        Guid Id, string Title, string Status, DateTimeOffset CreatedAt, int TotalCount);
}
```

**Controller action** (GET):

```csharp
[HttpGet]
[ProducesResponseType(typeof(PagedResult<DocumentSummaryDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetList(
    [FromQuery] GetDocumentListRequest request, CancellationToken ct)
{
    var result = await sender.Send(request.ToQuery(currentUser.TenantId), ct);
    return Ok(result);
}

[HttpGet("{id:guid}")]
[ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
{
    var result = await sender.Send(new GetDocumentByIdQuery(id, currentUser.TenantId), ct);
    return result is null ? NotFound() : Ok(result);
}
```

### Scaffold a Domain Aggregate Root

Use for: new core business concepts with lifecycle and invariants. **Not for simple CRUD master data.**

```csharp
// Domain/Aggregates/Document.cs
public sealed class Document : AggregateRoot, ITenantEntity
{
    private Document() { }  // EF Core

    public Guid TenantId { get; private set; }
    public DocumentTitle Title { get; private set; } = null!;
    public DocumentStatus Status { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Document Create(Guid tenantId, string title, Guid createdBy)
    {
        var document = new Document
        {
            TenantId   = tenantId,
            Title      = DocumentTitle.Create(title), // Value Object validates
            Status     = DocumentStatus.Draft,
            CreatedBy  = createdBy,
            CreatedAt  = DateTimeOffset.UtcNow
        };
        document.AddDomainEvent(new DocumentCreatedEvent(document.Id, tenantId));
        return document;
    }

    public void Publish(Guid publishedBy)
    {
        if (Status != DocumentStatus.Draft)
            throw new DomainException($"Document can only be published from Draft status. Current: {Status}");

        Status = DocumentStatus.Published;
        AddDomainEvent(new DocumentPublishedEvent(Id, TenantId, publishedBy));
    }
}

// Domain/ValueObjects/DocumentTitle.cs
public sealed class DocumentTitle : ValueObject
{
    public string Value { get; }

    private DocumentTitle(string value) => Value = value;

    public static DocumentTitle Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Document title cannot be empty.");
        if (value.Length > 500)
            throw new DomainException("Document title cannot exceed 500 characters.");
        return new DocumentTitle(value.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

// Domain/Events/DocumentPublishedEvent.cs
public sealed record DocumentPublishedEvent(
    Guid DocumentId,
    Guid TenantId,
    Guid PublishedBy) : IDomainEvent;

// Application/Interfaces/IDocumentRepository.cs
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Document document, CancellationToken ct = default);
    Task InvalidateAsync(Guid id, CancellationToken ct = default);
}

// Infrastructure/Persistence/Configurations/DocumentConfiguration.cs
internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.TenantId).IsRequired();
        builder.HasIndex(d => d.TenantId);

        builder.Property(d => d.Title)
            .HasConversion(t => t.Value, v => DocumentTitle.Create(v))
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Global Query Filter — enforces tenant isolation at EF Core level
        builder.HasQueryFilter(d => d.TenantId == EF.Property<Guid>(d, "_tenantId"));

        // Optimistic concurrency — PostgreSQL xmin system column
        builder.UseXminAsConcurrencyToken();
    }
}
```

### Scaffold a MassTransit Consumer

For full patterns (Inbox check, retry, DLQ), load the **messaging** skill. Use this as the structural template:

```csharp
// Infrastructure/Messaging/Consumers/DocumentPublishedConsumer.cs
internal sealed class DocumentPublishedConsumer(
    AppDbContext db,
    ILogger<DocumentPublishedConsumer> logger) : IConsumer<DocumentPublished>
{
    public async Task Consume(ConsumeContext<DocumentPublished> context)
    {
        var messageId = context.MessageId?.ToString()
            ?? throw new InvalidOperationException("MessageId is required.");

        // Inbox check — idempotency guard
        if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == messageId))
        {
            logger.LogDebug("Message {MessageId} already processed, skipping", messageId);
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync();

        // Side-effects + inbox record in same transaction
        db.ProcessedMessages.Add(new ProcessedMessage(messageId, DateTimeOffset.UtcNow));
        // TODO: side-effects here (e.g. update read model, send notification)
        await db.SaveChangesAsync();

        await tx.CommitAsync();
        logger.LogInformation("Message {MessageId} processed successfully", messageId);
    }
}

// Infrastructure/Messaging/Consumers/DocumentPublishedConsumerDefinition.cs
internal sealed class DocumentPublishedConsumerDefinition
    : ConsumerDefinition<DocumentPublishedConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<DocumentPublishedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2));
            r.Ignore<DomainException>();      // business errors — no point retrying
            r.Ignore<ValidationException>();  // validation errors — no point retrying
        });
        endpointConfigurator.UseDeadLetterQueueDeadLetterTransport();
    }
}
```

## Anti-patterns

### Don't Put Business Logic in Validators

```csharp
// BAD — validator loads from DB, knows about business rules
public class PublishDocumentCommandValidator : AbstractValidator<PublishDocumentCommand>
{
    public PublishDocumentCommandValidator(AppDbContext db)
    {
        RuleFor(x => x.DocumentId).MustAsync(async (id, ct) =>
            (await db.Documents.FindAsync(id))?.Status == DocumentStatus.Draft); // WRONG
    }
}

// GOOD — validator checks format/required only; domain method enforces invariants
public class PublishDocumentCommandValidator : AbstractValidator<PublishDocumentCommand>
{
    public PublishDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
    }
}
```

### Don't Use EF Core on the Read Path

```csharp
// BAD — query uses EF Core + domain entity + AutoMapper
var entities = await db.Documents.Where(d => d.TenantId == tenantId).ToListAsync(ct);
return mapper.Map<List<DocumentSummaryDto>>(entities);

// GOOD — raw SQL with Dapper, map directly to DTO
var rows = await db.QueryAsync<DocumentSummaryDto>(sql, new { TenantId = tenantId });
```

### Don't Forget `WHERE tenant_id` in Query Handlers

```csharp
// BAD — returns data from ALL tenants
const string sql = "SELECT * FROM documents WHERE status = 'Published'";

// GOOD — always filter by tenant
const string sql = """
    SELECT * FROM documents
    WHERE tenant_id = @TenantId AND status = 'Published'
    """;
```

### Don't Expose `IQueryable` in Repository Interfaces

```csharp
// BAD — leaks persistence concern to application layer
public interface IDocumentRepository
{
    IQueryable<Document> GetAll(); // application can bypass query filters
}

// GOOD — narrow interface, specific methods only
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Document document, CancellationToken ct = default);
}
```

## Decision Guide

| What to scaffold | Recipe |
|---|---|
| New write use case | Command record + Validator + Handler + Controller [HttpPost/Put/Delete] |
| New read use case | Query record + Validator + Dapper Handler + DTO + Controller [HttpGet] |
| New core domain concept | Aggregate + Value Objects + Domain Events + EF Config + Repository interface + EF implementation |
| Simple CRUD master data | Single EF entity + controller — skip Aggregate Root, skip Value Objects |
| Subscribe to external event | MassTransit Consumer + ConsumerDefinition + load `messaging` skill for Inbox pattern |
| Background outbox setup | Load `messaging` skill — Outbox entity + OutboxProcessor BackgroundService |
| POST that must be idempotent | Add `[FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey` to controller action |
| Aggregate with children | `private readonly List<ChildEntity> _children` in Aggregate — no separate repository |
| List query with pagination | `COUNT(*) OVER()` in Dapper + `PagedResult<T>` return type + Validator max PageSize = 100 |
