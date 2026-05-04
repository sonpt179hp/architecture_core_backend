# 01 – Clean Architecture Rules

**Nguồn:** `design_pattern_architecture.md` §2.1 · `backend_core_technical_guidelines.md` §4.6

---

## DO

1. **Tuân thủ dependency rule tuyệt đối:**
   ```
   Presentation → Application → Domain ← Infrastructure
   ```
   `Domain` và `Application` không được import bất kỳ package nào từ Infrastructure (EntityFrameworkCore, Dapper, MassTransit, Serilog, Polly...).

2. **Đặt Aggregate Root, Value Objects, Domain Events** vào `Domain` layer cho các bounded context cốt lõi (Document, Office Management).

3. **Namespace Convention:** `{Namespace}.{Layer}.{Feature}`:
   ```
   {Namespace}.Domain.{Feature}           → Entities, Errors, Events, ValueObjects
   {Namespace}.Application.{Feature}.Commands.Create{Entity}
   {Namespace}.Application.{Feature}.Queries.Get{Entity}ById
   {Namespace}.Infrastructure.Persistence
   {Namespace}.Api.Controllers.{Feature}
   ```
   - Layer: `Domain`, `Application`, `Infrastructure`, `Api`
   - Feature: tên bounded context
   - File-scoped namespace, indent 4 spaces

4. **Dùng CRUD đơn giản** (không có Aggregate Root, không có Value Object) cho các entity phụ trợ như Setting, Master Data.

5. **Áp dụng Architecture Test** (NetArchTest / ArchUnitNET) để enforce dependency rule trong CI pipeline.

6. **Mỗi bounded context** (Document, OrgManagement, UserIdentity) là một module/project riêng, tự quản migration riêng.

7. **Repository Pattern là optional.** Nếu dùng:
   - Định nghĩa interface (ví dụ `IDocumentRepository`) trong `Application` layer.
   - Cài đặt cụ thể (`EfDocumentRepository`) đặt trong `Infrastructure` layer.
   - Nếu KHÔNG dùng Repository — dùng trực tiếp `IApplicationDbContext` trong handler.

## DON'T

1. **KHÔNG** để controller hoặc Infrastructure class nào kế thừa hoặc inject trực tiếp vào Domain entity.

2. **KHÔNG** để Domain layer tham chiếu `Microsoft.EntityFrameworkCore`, `Dapper`, `MassTransit` hoặc bất kỳ framework infrastructure nào.

3. **KHÔNG** dùng `[Table]`, `[Column]` EF annotation trực tiếp trên Domain entity — dùng Fluent Configuration trong `Infrastructure`.

4. **KHÔNG** tạo "God Service" chứa logic của nhiều Aggregate khác nhau.

5. **KHÔNG** áp dụng full DDD (Aggregate Root, Domain Events, Specification) cho Setting/Master Data — đây là over-engineering.

6. **KHÔNG** mix MediatR (Jimmy Bogard) với Mediator (Arch.Ext). Chỉ dùng Mediator (Arch.Ext).

## Ví dụ minh họa

```csharp
// ── Folder & Namespace Convention
// src/{Solution}.Domain/{Feature}/
namespace {Namespace}.Domain.{Feature};

// src/{Solution}.Application/{Feature}/Commands/Create{Entity}/
namespace {Namespace}.Application.{Feature}.Commands.Create{Entity};

// ── ❌ WRONG — EF annotation trong Domain
public class {Entity} : AggregateRoot
{
    [Column("doc_title")]
    public string Title { get; set; }
}

// ── ✅ CORRECT — Domain entity thuần
namespace {Namespace}.Domain.{Feature};

public class {Entity} : AggregateRoot<{Entity}Id>
{
    public {Entity}Name Name { get; private set; }
    public TenantId TenantId { get; private set; }
    private {Entity}() { }

    public static Result<{Entity}> Create({Entity}Name name, TenantId tenantId)
    {
        if (name.IsEmpty) return {Entity}Errors.NameEmpty;
        return new {Entity}({Entity}Id.New(), name, tenantId);
    }
}

// ── ✅ CORRECT — Fluent config trong Infrastructure
namespace {Namespace}.Infrastructure.Persistence.Configurations;

public class {Entity}Configuration : IEntityTypeConfiguration<{Entity}>
{
    public void Configure(EntityTypeBuilder<{Entity}> builder)
    {
        builder.ToTable("{entity_names}");

        builder.Property(e => e.Name)
              .HasConversion(v => v.Value, v => {Entity}Name.Create(v).Value)
              .HasColumnName("name")
              .HasMaxLength(500);

        builder.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
    }
}

// ── ✅ CORRECT — Repository Interface trong Application
namespace {Namespace}.Application.{Feature}.Abstractions;

public interface I{Entity}Repository
{
    Task<{Entity}?> GetByIdAsync({Entity}Id id, CancellationToken ct);
    Task AddAsync({Entity} entity, CancellationToken ct);
    Task<bool> ExistsAsync({Entity}Id id, CancellationToken ct);
}

// ── ✅ CORRECT — Repository Implementation trong Infrastructure
namespace {Namespace}.Infrastructure.Persistence.Repositories;

internal sealed class Ef{Entity}Repository : I{Entity}Repository
{
    private readonly AppDbContext _db;
    public Ef{Entity}Repository(AppDbContext db) { _db = db; }
    public async Task<{Entity}?> GetByIdAsync({Entity}Id id, CancellationToken ct) =>
        await _db.{Entities}.FirstOrDefaultAsync(e => e.Id == id, ct);
}
```

```csharp
// ── Architecture Test
[Fact]
public void Domain_Should_Not_Reference_Infrastructure()
{
    var result = Types.InAssembly(typeof({Entity}).Assembly)
        .ShouldNot()
        .HaveDependencyOn("Infrastructure")
        .GetResult();
    Assert.True(result.IsSuccessful);
}
```
