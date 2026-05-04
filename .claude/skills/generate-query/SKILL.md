# Skill: Generate CQRS Query

## Purpose

Scaffold đầy đủ bộ file cho một read use case mới theo CQRS read-path convention.
Tuân thủ Rule:
- **Read đơn giản**: EF Core `AsNoTracking()`
- **Read phức tạp**: Dapper + **Stored Procedure**
- Tuân thủ Result pattern, Mediator, và FluentValidation.

## CQRS Decision Matrix

| Thao tác | Cách thực hiện |
|---|---|
| **Tạo mới (Create)** | EF Core |
| **Cập nhật (Update)** | EF Core |
| **Xóa (Delete)** | EF Core |
| **Đọc đơn giản** (1 entity, theo ID) | EF Core `AsNoTracking()` |
| **Đọc phức tạp** (nhiều bảng, JOIN, filter phức tạp) | Dapper + **Stored Procedure** |

## PagedResult Type

```csharp
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
```

## Convention Mapping

| Artifact | Convention | Pattern |
|---|---|---|
| **Mediator** | `Mediator` (Arch.Ext) | `ISender`, `IQuery`, `IQueryHandler` |
| **Namespace** | `{Namespace}.{Feature}.Queries.{QueryName}` | Feature-sliced |
| **Query** | `public sealed record {Name}Query(...)` | Immutable, input params only |
| **Response DTO** | `public sealed record {Name}Response(...)` | Flat, UI-facing |
| **Mapping** | Extension method `ToResponse()` | Entity → DTO |
| **EF Core read** | `.AsNoTracking()` | Read-only queries |
| **Dapper read** | `IDbConnection` + Stored Procedure | Complex JOINs, high perf |
| **Return type** | `Result<T>` | NotFound error nếu không tìm thấy |

## Project Structure

```
src/
├── Application/
│   ├── Common/
│   │   └── PagedResult.cs     # PagedResult<T> type
│   └── {Feature}/
│       └── Queries/
│           └── {QueryName}/
│               ├── {QueryName}Query.cs
│               ├── {QueryName}QueryHandler.cs
│               ├── {QueryName}Response.cs
│               └── {QueryName}Validator.cs (nếu cần)
│       └── Mappings/
│           └── {Entity}Mappings.cs
└── Api/
    └── Controllers/
        └── {Feature}Controller.cs
```

## Instructions

**Input cần từ user:** Tên query, bounded context, loại (simple/complex, single/list), filter parameters.

### Decision: EF Core vs Dapper + Stored Procedure

- **Dùng EF Core** (`IApplicationDbContext`): Query đơn giản, 1 entity, dùng `.AsNoTracking()`.
- **Dùng Dapper + Stored Procedure** (`IDbConnection`): JOIN > 2 bảng, filter phức tạp, dashboard/reports.

> **Quy tắc:** JOIN > 2 bảng hoặc filter phức tạp → dùng Dapper + Stored Procedure.
> **KHÔNG viết SQL thuần trong code C#** — dùng Stored Procedure.

### Path A: EF Core Read (Query đơn giản)

#### Create Response DTO

`src/{Solution}/Application/{Feature}/Queries/{QueryName}/{QueryName}Response.cs`:

```csharp
namespace {Namespace}.Application.{Feature}.Queries.{QueryName};

public sealed record {QueryName}Response(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

#### Create Query Record

`src/{Solution}/Application/{Feature}/Queries/{QueryName}/{QueryName}Query.cs`:

```csharp
using {Namespace}.Application.Abstractions.Messaging;

namespace {Namespace}.Application.{Feature}.Queries.{QueryName};

public sealed record {QueryName}Query(Guid {Entity}Id)
    : IQuery<Result<{QueryName}Response>>;
```

#### Create Mapping Extension

`src/{Solution}/Application/{Feature}/Mappings/{Entity}Mappings.cs`:

```csharp
using {Namespace}.Application.{Feature}.Queries.{QueryName};
using {Namespace}.Domain.{Feature};

namespace {Namespace}.Application.{Feature}.Mappings;

public static class {Entity}Mappings
{
    public static {QueryName}Response ToResponse(this {Entity} entity) =>
        new(
            entity.Id.Value,
            entity.Name,
            entity.Description,
            entity.Price,
            entity.CreatedAt,
            entity.UpdatedAt);
}
```

#### Create QueryHandler

`src/{Solution}/Application/{Feature}/Queries/{QueryName}/{QueryName}QueryHandler.cs`:

```csharp
using {Namespace}.Application.Abstractions.Data;
using {Namespace}.Application.Abstractions.Messaging;
using {Namespace}.Application.{Feature}.Mappings;
using {Namespace}.Domain.Common;
using {Namespace}.Domain.{Feature};
using {Namespace}.Domain.{Feature}.Errors;
using Microsoft.EntityFrameworkCore;

namespace {Namespace}.Application.{Feature}.Queries.{QueryName};

internal sealed class {QueryName}QueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<{QueryName}Query, {QueryName}Response>
{
    public async ValueTask<Result<{QueryName}Response>> Handle(
        {QueryName}Query query,
        CancellationToken cancellationToken)
    {
        var entityId = new {Entity}Id(query.{Entity}Id);

        var entity = await dbContext.{Entities}
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entityId, cancellationToken);

        if (entity is null)
            return {Entity}Errors.NotFound;

        return entity.ToResponse();
    }
}
```

### Path B: EF Core Read — Paginated List

`src/{Solution}/Application/{Feature}/Queries/{QueryName}/{QueryName}Query.cs`:

```csharp
using {Namespace}.Application.Abstractions.Messaging;

namespace {Namespace}.Application.{Feature}.Queries.{QueryName};

public sealed record {QueryName}Query(int Page = 1, int PageSize = 20, string? Keyword = null)
    : IQuery<Result<PagedResult<{QueryName}Response>>>;
```

`src/{Solution}/Application/{Feature}/Queries/{QueryName}/{QueryName}QueryHandler.cs`:

```csharp
using {Namespace}.Application.Abstractions.Data;
using {Namespace}.Application.Abstractions.Messaging;
using {Namespace}.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace {Namespace}.Application.{Feature}.Queries.{QueryName};

internal sealed class {QueryName}QueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<{QueryName}Query, PagedResult<{QueryName}Response>>
{
    public async ValueTask<Result<PagedResult<{QueryName}Response>>> Handle(
        {QueryName}Query query,
        CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.{Entities}.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            baseQuery = baseQuery.Where(e => e.Name.Contains(query.Keyword));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderByDescending(e => e.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new {QueryName}Response(
                e.Id.Value, e.Name, e.Description,
                e.Price, e.CreatedAt, e.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<{QueryName}Response>(items, totalCount, query.Page, query.PageSize);
    }
}
```

### Path C: Dapper + Stored Procedure (Query phức tạp)

> **Lưu ý:** Trước tiên cần tạo Stored Procedure trong database.

#### Tạo Stored Procedure

```sql
-- Database/StoredProcedures/sp_{Entity}_GetById.sql
CREATE OR REPLACE FUNCTION sp_{Entity}_GetById(p_id UUID, p_tenant_id UUID)
RETURNS TABLE (...) AS $$
BEGIN
    RETURN QUERY SELECT ... FROM {entities} WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$ LANGUAGE plpgsql;
```

#### Create QueryHandler với Dapper + Stored Procedure

`src/{Solution}/Application/{Feature}/Queries/{QueryName}/{QueryName}QueryHandler.cs`:

```csharp
using System.Data;
using Dapper;
using {Namespace}.Application.Abstractions.Data;
using {Namespace}.Application.Abstractions.Messaging;
using {Namespace}.Application.{Feature}.Queries.{QueryName};
using {Namespace}.Domain.Common;
using {Namespace}.Domain.{Feature}.Errors;

namespace {Namespace}.Application.{Feature}.Queries.{QueryName};

internal sealed class {QueryName}QueryHandler(
    IDbConnection dbConnection,
    ITenantContext tenantContext)
    : IQueryHandler<{QueryName}Query, {QueryName}Response>
{
    public async ValueTask<Result<{QueryName}Response>> Handle(
        {QueryName}Query query,
        CancellationToken cancellationToken)
    {
        var parameters = new { Id = query.{Entity}Id, tenantContext.TenantId };

        var result = await dbConnection.QueryFirstOrDefaultAsync<{QueryName}Response>(
            "sp_{Entity}_GetById",
            parameters,
            commandType: CommandType.StoredProcedure);

        if (result is null)
            return {Entity}Errors.NotFound;

        return result;
    }
}
```

### Update Controller

`src/{Solution}/Api/Controllers/{Feature}Controller.cs`:

```csharp
[HttpGet("{id:guid}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
{
    var result = await Sender.Send(new {QueryName}Query(id), ct);
    return result.ToActionResult();
}

[HttpGet]
[ProducesResponseType(StatusCodes.Status200OK)]
public async Task<IActionResult> GetList(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? keyword = null,
    CancellationToken ct = default)
{
    var result = await Sender.Send(new {QueryName}Query(page, pageSize, keyword), ct);
    return result.ToActionResult();
}
```

### Create Unit Test

`tests/{Solution}.UnitTests/Application/{QueryName}QueryHandlerTests.cs`:

```csharp
using {Namespace}.Application.{Feature}.Queries.{QueryName};
using {Namespace}.Domain.{Feature};
using {Namespace}.Domain.{Feature}.Errors;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace {Solution}.UnitTests.Application;

public class {QueryName}QueryHandlerTests
{
    private static DbContextOptions CreateInMemoryOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task Handle_WhenEntityExists_ShouldReturnMappedResponse()
    {
        await using var context = new ApplicationDbContext(CreateInMemoryOptions());
        var entity = {Entity}.Create("Widget", "desc", 9.99m).Value;
        context.{Entities}.Add(entity);
        await context.SaveChangesAsync();

        var handler = new {QueryName}QueryHandler(context);
        var result = await handler.Handle(new {QueryName}Query(entity.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Widget");
    }

    [Fact]
    public async Task Handle_WhenEntityNotFound_ShouldReturnNotFoundError()
    {
        await using var context = new ApplicationDbContext(CreateInMemoryOptions());
        var handler = new {QueryName}QueryHandler(context);
        var result = await handler.Handle(new {QueryName}Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({Entity}Errors.NotFound);
    }
}
```

## Checklist

- [ ] Read đơn giản: dùng EF Core với `.AsNoTracking()`
- [ ] Read phức tạp: dùng Dapper + Stored Procedure (KHÔNG viết SQL thuần trong code C#)
- [ ] Trả `{Entity}Errors.NotFound` khi entity không tìm thấy
- [ ] Dùng `.ToResponse()` extension method cho mapping (EF Core path)
- [ ] CancellationToken được truyền qua toàn bộ async call chain
- [ ] Handler không chứa business logic
- [ ] Stored Procedure đặt trong thư mục `Database/StoredProcedures/` và có migration tương ứng

## Edge Cases

- Phân trang: dùng EF Core `.Skip()`/`.Take()` hoặc Stored Procedure với `LIMIT`/`OFFSET`.
- Full-text search: dùng PostgreSQL `tsvector` hoặc `ILIKE`.
- Export CSV/Excel: dùng background job thay vì sync response.
- JOIN > 2 bảng: tạo Database View trước, gọi qua Stored Procedure.

## References

- `ai-rules/02-cqrs-pattern.md`
- `ai-rules/03-security-tenancy.md`
- `ai-rules/04-api-contract.md`
- `ai-rules/06-observability.md`
