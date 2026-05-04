# Skill: Generate Domain Entity (Aggregate Root)

## Purpose

Scaffold đầy đủ mô hình DDD cho một Aggregate Root mới theo Clean Architecture.
Domain layer phải hoàn toàn không phụ thuộc framework (không EF annotations, không MassTransit imports).

Áp dụng cho core services có invariants, lifecycle, behaviors; **không áp dụng** cho CRUD master-data đơn giản.

## Convention Mapping

| Artifact | Convention | Pattern |
|---|---|---|
| **Entity base** | `Entity<TId> where TId : notnull` | Base class có `RaiseDomainEvent()` |
| **Aggregate base** | `AggregateRoot<TId>` | Kế thừa Entity, đánh dấu aggregate boundary |
| **ID** | `readonly record struct {Name}Id(Guid Value)` | Immutable, factory `New()` |
| **Value Objects** | `readonly record struct` hoặc `record` | Immutable, validation trong factory |
| **Domain Events** | Interface `IDomainEvent` (marker) | Raise qua `RaiseDomainEvent()` trong Entity |
| **Result Pattern** | `Result` + `Result<T>` + `Error` + `ErrorType` | Railway-oriented |
| **Errors** | Static class `{Entity}Errors` | Predefined `Error` constants |
| **Factory method** | `static Result<T> Create(...)` | Validate invariants, trả `Result<T>` |
| **EF Core config** | `IEntityTypeConfiguration<T>` | Fluent API, snake_case column names |

## Project Structure

```
src/
├── Domain/
│   ├── Primitives/
│   │   ├── Entity.cs              ← TId base class + domain events
│   │   ├── AggregateRoot.cs      ← Aggregate boundary marker
│   │   └── IDomainEvent.cs       ← Domain event marker
│   ├── Common/
│   │   ├── Result.cs             ← Result base class
│   │   ├── ResultT.cs            ← Result<T> generic
│   │   ├── Error.cs             ← Error record struct
│   │   └── ErrorType.cs          ← Failure|Validation|NotFound|Conflict|Unauthorized
│   └── {Feature}/
│       ├── {Entity}Id.cs         ← ID value type
│       ├── {Entity}.cs          ← Aggregate Root
│       ├── ValueObjects/         ← Value objects
│       ├── Events/               ← Domain events
│       └── Errors/               ← Predefined errors
└── Infrastructure/
    └── Persistence/
        └── Configurations/
            └── {Entity}Configuration.cs
```

## Instructions

**Input cần từ user:** Tên aggregate, bounded context, properties, behaviors nghiệp vụ.

### Step 1: Validate Entity Type

- Core business concept có invariants, lifecycle, behaviors → Aggregate Root.
- Master data/setting đơn giản → CRUD entity nhẹ hơn, không dùng skill này.

### Step 2: Create ID Value Type

`src/{Solution}/{Domain}/{Feature}/{Entity}Id.cs`:

```csharp
namespace {Namespace}.{Feature};

public readonly record struct {Entity}Id(Guid Value)
{
    public static {Entity}Id New() => new(Guid.NewGuid());
}
```

### Step 3: Create Domain Errors

`src/{Solution}/{Domain}/{Feature}/Errors/{Entity}Errors.cs`:

```csharp
using {Namespace}.Domain.Common;

namespace {Namespace}.{Feature}.Errors;

public static class {Entity}Errors
{
    public static readonly Error NotFound = Error.NotFound(
        "{Entity}.NotFound",
        "The {entity} with the specified identifier was not found.");

    public static readonly Error NameEmpty = Error.Validation(
        "{Entity}.NameEmpty",
        "The {entity} name cannot be empty.");

    public static readonly Error InvalidState = Error.Failure(
        "{Entity}.InvalidState",
        "The {entity} is in an invalid state for this operation.");
}
```

### Step 4: Create Aggregate Root

`src/{Solution}/{Domain}/{Feature}/{Entity}.cs`:

```csharp
using {Namespace}.Domain.Common;
using {Namespace}.Domain.Primitives;
using {Namespace}.{Feature}.Errors;

namespace {Namespace}.{Feature};

public sealed class {Entity} : AggregateRoot<{Entity}Id>
{
    private {Entity}(
        {Entity}Id id,
        string name,
        DateTime createdAt) : base(id)
    {
        Name = name;
        CreatedAt = createdAt;
    }

    // Required by EF Core
    private {Entity}()
    {
    }

    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static Result<{Entity}> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return {Entity}Errors.NameEmpty;
        }

        var entity = new {Entity}({Entity}Id.New(), name, DateTime.UtcNow);
        return Result<{Entity}>.Success(entity);
    }

    public Result Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return {Entity}Errors.NameEmpty;
        }

        Name = name;
        return Result.Success();
    }

    // Thêm domain behaviors (Publish, Approve, Archive...) theo yêu cầu
    // Mỗi behavior gọi RaiseDomainEvent(new {Entity}{Action}Event(...))
}
```

### Step 5: Create Domain Events (nếu có behaviors)

`src/{Solution}/{Domain}/{Feature}/Events/{Entity}{Action}Event.cs`:

```csharp
using {Namespace}.Domain.Primitives;

namespace {Namespace}.{Feature}.Events;

public sealed class {Entity}{Action}Event : IDomainEvent
{
    public {Entity}{Action}Event({Entity}Id {entity}Id, string name)
    {
        {Entity}Id = {entity}Id;
        Name = name;
        OccurredAt = DateTime.UtcNow;
    }

    public {Entity}Id {Entity}Id { get; }
    public string Name { get; }
    public DateTime OccurredAt { get; }
}
```

### Step 6: Create EF Core Configuration

`src/{Solution}/{Infrastructure}/Persistence/Configurations/{Entity}Configuration.cs`:

```csharp
using {Namespace}.{Feature};
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace {Namespace}.Infrastructure.Persistence.Configurations;

internal sealed class {Entity}Configuration : IEntityTypeConfiguration<{Entity}>
{
    public void Configure(EntityTypeBuilder<{Entity}> builder)
    {
        builder.ToTable("{entity_names}"); // snake_case: products, documents

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new {Entity}Id(value))
            .HasColumnName("id");

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
```

### Step 7: Add DbSet to ApplicationDbContext

```csharp
// IApplicationDbContext
DbSet<{Entity}> {Entities} { get; }

// ApplicationDbContext
public DbSet<{Entity}> {Entities} => Set<{Entity}>();
```

## Checklist

- [ ] Domain layer không import EF Core, Dapper, MassTransit
- [ ] Tất cả mutable operations đi qua method, không set property trực tiếp
- [ ] Factory method validate invariants, trả `Result<T>`
- [ ] Domain Events được raise qua `RaiseDomainEvent()`
- [ ] EF Core config dùng snake_case column names
- [ ] Unit tests cover happy path và error cases

## Edge Cases

- Aggregate có child entities: tạo trong cùng file với `private readonly List<ChildEntity> _children`.
- Không cần multi-tenancy: bỏ `ITenantEntity`, nêu rõ lý do.
- Master data/setting: dừng và giải thích đây là over-engineering.
- Value Objects phức tạp: tạo file riêng với `readonly record struct` và factory validation.

## References

- `ai-rules/01-clean-architecture.md` — Domain thuần, dependency rule
- `ai-rules/03-security-tenancy.md` — ITenantEntity, Global Query Filter boundaries
- `ai-rules/08-efcore.md` — Fluent Configuration, concurrency token
- `ai-rules/07-testing.md` — unit test expectations for Aggregate/Value Objects
