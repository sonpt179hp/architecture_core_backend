---
name: ef-core
description: >
  Entity Framework Core 8 patterns for .NET 8 Clean Architecture. Covers write-path
  vs read-path (EF Core + Dapper), DbContext configuration, optimistic concurrency
  with PostgreSQL xmin, bulk operations, interceptors, migrations, and query optimization.
  Load this skill when working with databases, writing queries, managing schema
  changes, or when the user mentions "EF Core", "Entity Framework", "DbContext",
  "migration", "LINQ query", "database", "SQL", "N+1", "Include", "Dapper",
  "xmin", "concurrency", "ExecuteUpdateAsync", "ExecuteDeleteAsync",
  "value converter", "interceptor", or "compiled query".
---

# EF Core (.NET 8)

## Core Principles

1. **Write path vs Read path** — EF Core with Change Tracking for writes (commands). Dapper + raw SQL → DTO for reads (queries). Do NOT use EF `Include` chains on read queries; use Dapper for anything requiring joins or complex projections.
2. **Optimistic Concurrency for business-critical entities** — Use PostgreSQL `xmin` (system column updated on every row write) as the concurrency token. No application-managed `RowVersion` column needed.
3. **Bulk operations** — Use `ExecuteUpdateAsync`/`ExecuteDeleteAsync` for bulk changes. Never load entities into memory just to delete or update them in a loop.
4. **DbContext is a unit of work** — Don't wrap it in another UoW abstraction. EF Core already implements Unit of Work internally. Register as Scoped; never share across threads.
5. **Fluent configuration only** — Configure entities exclusively via `IEntityTypeConfiguration<T>`. Never place EF Data Annotations (`[Column]`, `[MaxLength]`, `[Required]`) on Domain entities.
6. **Migrations are code** — Treat them like source code. Review them, test them, and never call `MigrateAsync()` from web app startup in production.

## Patterns

### DbContext Configuration

```csharp
// Infrastructure/Persistence/AppDbContext.cs
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

### Registration with CommandTimeout and EnableRetryOnFailure

```csharp
// In AddInfrastructure()
services.AddDbContext<AppDbContext>((sp, options) =>
    options
        .UseNpgsql(
            configuration.GetConnectionString("Default"),
            npgsql => npgsql
                .CommandTimeout(30)
                .EnableRetryOnFailure(
                    maxRetryCount: 2,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null))
        .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
```

### Fluent Entity Configuration (No Data Annotations on Domain)

```csharp
// Infrastructure/Persistence/Configurations/DocumentConfiguration.cs
public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Optimistic concurrency via PostgreSQL xmin
        builder.UseXminAsConcurrencyToken();

        builder.HasIndex(d => d.TenantId);
        builder.HasIndex(d => d.CreatedAt);
    }
}
```

### Optimistic Concurrency with PostgreSQL xmin

PostgreSQL updates `xmin` automatically on every row write — no application-side column needed.

```csharp
// Entity — no RowVersion property required
public class Document
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public uint Version { get; private set; } // mapped to xmin by EF Core
}

// Configuration
builder.UseXminAsConcurrencyToken();
// EF Core maps Version (uint) → xmin column automatically with Npgsql provider

// Handler — catch concurrency conflict and return HTTP 409
public async Task<IActionResult> Handle(UpdateDocumentCommand command, CancellationToken ct)
{
    var document = await _db.Documents.FindAsync([command.Id], ct)
        ?? throw new NotFoundException(nameof(Document), command.Id);

    document.UpdateTitle(command.Title);

    try
    {
        await _db.SaveChangesAsync(ct);
        return Ok();
    }
    catch (DbUpdateConcurrencyException ex)
    {
        await ex.Entries.Single().ReloadAsync(ct); // reload latest state
        return Conflict(new { error = "Document was modified by another user. Please retry." });
    }
}
```

**Why xmin**: Eliminates the need for an application-managed `RowVersion`/`Timestamp` column. PostgreSQL guarantees uniqueness across the entire table, not just the row lifecycle.

### Write Path — Repository Pattern

Application layer defines the interface; Infrastructure layer implements it with EF Core.

```csharp
// Application/Repositories/IDocumentRepository.cs
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct);
    void Add(Document document);
    void Remove(Document document);
}

// Infrastructure/Repositories/EfDocumentRepository.cs
public class EfDocumentRepository(AppDbContext db) : IDocumentRepository
{
    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await db.Documents.FindAsync([id], ct);

    public void Add(Document document) => db.Documents.Add(document);

    public void Remove(Document document) => db.Documents.Remove(document);
}
// SaveChangesAsync is called by the Unit of Work (DbContext) in the handler or pipeline behavior
```

### Read Path — Dapper for Queries

Use Dapper + raw SQL to project directly into DTOs. No EF Change Tracking overhead.

```csharp
// Application/Queries/GetDocumentListQuery.cs
public record DocumentSummaryDto(Guid Id, string Title, string Status, DateTimeOffset CreatedAt);

// Infrastructure/Queries/DocumentQueryService.cs
public class DocumentQueryService(IDbConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<DocumentSummaryDto>> GetByTenantAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct)
    {
        using var conn = connectionFactory.Create();
        var sql = """
            SELECT d.id, d.title, d.status, d.created_at
            FROM documents d
            WHERE d.tenant_id = @TenantId
              AND d.is_deleted = false
            ORDER BY d.created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        return (await conn.QueryAsync<DocumentSummaryDto>(
            sql,
            new { TenantId = tenantId, PageSize = pageSize, Offset = (page - 1) * pageSize }))
            .AsList();
    }
}
```

**Why Dapper for reads**: EF Core's `AsNoTracking()` + projection is good for simple queries, but Dapper is simpler and faster for complex joins, reporting queries, or when you want full SQL control.

### Bulk Operations — ExecuteUpdateAsync / ExecuteDeleteAsync

```csharp
// Bulk delete without loading entities (bypasses Change Tracking)
await db.Documents
    .Where(d => d.TenantId == tenantId
             && d.Status == DocumentStatus.Archived
             && d.CreatedAt < archiveCutoff)
    .ExecuteDeleteAsync(ct);

// Bulk update without loading entities
await db.Documents
    .Where(d => d.Status == DocumentStatus.Pending && d.CreatedAt < cutoff)
    .ExecuteUpdateAsync(s => s
        .SetProperty(d => d.Status, DocumentStatus.Expired)
        .SetProperty(d => d.UpdatedAt, clock.GetUtcNow()),
        ct);
```

**Why**: Loading 10,000 rows to delete them wastes memory, triggers change tracking overhead, and executes N round-trips. `ExecuteDeleteAsync` compiles to a single `DELETE FROM ... WHERE ...`.

### Interceptors for Audit Trails

```csharp
public class AuditInterceptor(TimeProvider clock, ICurrentUser currentUser)
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var context = eventData.Context;
        if (context is null) return ValueTask.FromResult(result);

        var now = clock.GetUtcNow();
        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = currentUser.Id;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUser.Id;
                    break;
            }
        }

        return ValueTask.FromResult(result);
    }
}
```

### Migrations Workflow

```bash
# Create migration
dotnet ef migrations add AddDocumentIndex \
  --project src/MyApp.Infrastructure \
  --startup-project src/MyApp.Api

# Review the generated migration — ALWAYS review before applying
# Check for data loss, index strategy, nullable columns

# Apply to development database
dotnet ef database update \
  --project src/MyApp.Infrastructure \
  --startup-project src/MyApp.Api

# Generate idempotent SQL script for production deployment
dotnet ef migrations script --idempotent \
  --output deploy/migrations.sql
```

### Global Query Filters

```csharp
// Soft delete filter
builder.HasQueryFilter(d => !d.IsDeleted);

// Multi-tenant filter
builder.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId);

// Bypass when needed (admin queries)
var allDocuments = await db.Documents.IgnoreQueryFilters().ToListAsync(ct);
```

## Anti-patterns

### DON'T Call MigrateAsync() from Web App Startup in Production

```csharp
// BAD — runs migrations on every pod startup; race condition with multiple instances
// and no ability to review or roll back
var app = builder.Build();
using var scope = app.Services.CreateScope();
await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();

// GOOD — run migrations as a separate deployment step (init container, CI job)
// Generate idempotent SQL script and apply via DBA-reviewed pipeline:
// dotnet ef migrations script --idempotent --output deploy/migrations.sql
```

### DON'T Use Include Chains > 2 Levels on Read Queries

```csharp
// BAD — complex Include chain for a read query; hard to optimize, loads full entities
var docs = await db.Documents
    .Include(d => d.Owner)
        .ThenInclude(u => u.Department)
            .ThenInclude(dept => dept.Region)
    .Where(d => d.TenantId == tenantId)
    .ToListAsync(ct);

// GOOD — use Dapper for complex joins; return DTO directly
var docs = await _queryService.GetDocumentsByTenantAsync(tenantId, page, pageSize, ct);
```

### DON'T Ignore DbUpdateConcurrencyException

```csharp
// BAD — swallows conflict; last writer wins silently
await db.SaveChangesAsync(ct);

// GOOD — detect conflict, reload state, return 409
try
{
    await db.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException ex)
{
    await ex.Entries.Single().ReloadAsync(ct);
    return Conflict(new { error = "Concurrent modification detected. Please retry." });
}
```

### DON'T Share DbContext Between Threads

```csharp
// BAD — DbContext is not thread-safe; resolving from singleton scope
public class BadSingleton(AppDbContext db) // DbContext is Scoped, not Singleton!
{
    public async Task DoWork() => await db.Documents.CountAsync();
}

// GOOD — create a scope per unit of work in background services
public class GoodBackgroundService(IServiceScopeFactory factory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await using var scope = factory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Documents.CountAsync(ct);
    }
}
```

### DON'T Use EF Data Annotations on Domain Entities

```csharp
// BAD — Domain entity coupled to EF Core infrastructure concern
public class Document
{
    [Key]
    [Column("document_id")]
    public Guid Id { get; set; }

    [MaxLength(500)]
    [Required]
    public string Title { get; set; } = string.Empty;
}

// GOOD — Domain entity is clean; EF config lives in Infrastructure
public class Document
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
}
// Configuration is in DocumentConfiguration : IEntityTypeConfiguration<Document>
```

### DON'T Use Lazy Loading

```csharp
// BAD — hides N+1 queries; makes performance unpredictable
options.UseLazyLoadingProxies();

// GOOD — explicit loading via Include (write path) or Dapper join (read path)
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Write command (create/update/delete) | EF Core with Change Tracking + repository |
| Read query / list view | Dapper + raw SQL → DTO |
| Simple single-entity read | EF Core `FindAsync` + `AsNoTracking` projection |
| Bulk update (100+ rows) | `ExecuteUpdateAsync` |
| Bulk delete (100+ rows) | `ExecuteDeleteAsync` |
| Optimistic concurrency (PostgreSQL) | `UseXminAsConcurrencyToken()` → catch `DbUpdateConcurrencyException` → 409 |
| Audit trails | `SaveChangesInterceptor` |
| Multi-tenancy / soft delete | Global query filter |
| Strongly-typed IDs | Value converter |
| Production migration | Idempotent SQL script from `dotnet ef migrations script` |
| Complex joins / reporting | Dapper |
