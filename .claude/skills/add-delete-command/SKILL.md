# Skill: Add Delete Command

## Purpose

Scaffold Delete command cho một aggregate đã tồn tại. Soft delete (recommended) hoặc hard delete.

## Convention Mapping

| Artifact | Convention | Pattern |
|---|---|---|
| **Mediator** | `Mediator` (Arch.Ext) | |
| **Namespace** | `{Namespace}.{Feature}.Commands.Delete{Entity}` | |
| **Command** | `record Delete{Entity}Command(Guid Id)` | |
| **Soft Delete** | `IsDeleted` flag + Global Query Filter | |
| **Hard Delete** | `ExecuteDeleteAsync` | |

## Project Structure

```
src/
├── Domain/
│   └── {Feature}/
│       └── {Entity}.cs       ← Thêm Delete() method
└── Application/
    └── {Feature}/
        └── Commands/
            └── Delete{Entity}/
                ├── Delete{Entity}Command.cs
                ├── Delete{Entity}CommandHandler.cs
                └── Delete{Entity}CommandValidator.cs
```

## Instructions

**Input cần từ user:** Tên entity, bounded context, soft delete hay hard delete.

### Decision: Soft Delete vs Hard Delete

- **Soft Delete (Recommended):** Thêm `IsDeleted` flag + Global Query Filter. Không mất dữ liệu, có thể khôi phục.
- **Hard Delete:** Xóa trực tiếp khỏi database. Chỉ dùng cho master data rác.

### Step 1: Create Delete Command

`src/{Solution}/Application/{Feature}/Commands/Delete{Entity}/Delete{Entity}Command.cs`:

```csharp
using {Namespace}.Application.Abstractions.Messaging;
using {Namespace}.Domain.Common;

namespace {Namespace}.Application.{Feature}.Commands.Delete{Entity};

public sealed record Delete{Entity}Command(Guid Id)
    : ICommand;
```

### Step 2: Create Validator

`src/{Solution}/Application/{Feature}/Commands/Delete{Entity}/Delete{Entity}CommandValidator.cs`:

```csharp
using FluentValidation;

namespace {Namespace}.Application.{Feature}.Commands.Delete{Entity};

public sealed class Delete{Entity}CommandValidator : AbstractValidator<Delete{Entity}Command>
{
    public Delete{Entity}CommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Entity ID is required.");
    }
}
```

### Step 3: Create Handler

`src/{Solution}/Application/{Feature}/Commands/Delete{Entity}/Delete{Entity}CommandHandler.cs`:

```csharp
using {Namespace}.Application.Abstractions.Data;
using {Namespace}.Application.Abstractions.Messaging;
using {Namespace}.Domain.Common;
using {Namespace}.Domain.{Feature};
using {Namespace}.Domain.{Feature}.Errors;
using Microsoft.EntityFrameworkCore;

namespace {Namespace}.Application.{Feature}.Commands.Delete{Entity};

internal sealed class Delete{Entity}CommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<Delete{Entity}Command>
{
    public async ValueTask<Result> Handle(
        Delete{Entity}Command command,
        CancellationToken cancellationToken)
    {
        var {entity}Id = new {Entity}Id(command.Id);

        var {entity} = await dbContext.{Entities}
            .FirstOrDefaultAsync(e => e.Id == {entity}Id, cancellationToken);

        if ({entity} is null)
        {
            return {Entity}Errors.NotFound;
        }

        {entity}.Delete();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();

        // Option B: Hard Delete
        // var rowsDeleted = await dbContext.{Entities}
        //     .Where(e => e.Id == {entity}Id)
        //     .ExecuteDeleteAsync(cancellationToken);
        //
        // if (rowsDeleted == 0)
        //     return {Entity}Errors.NotFound;
        //
        // return Result.Success();
    }
}
```

### Step 4: Add Delete Method to Aggregate

`src/{Solution}/Domain/{Feature}/{Entity}.cs`:

```csharp
public sealed class {Entity} : AggregateRoot<{Entity}Id>
{
    // ... existing code ...

    public bool IsDeleted { get; private set; }

    public void Delete()
    {
        // Raise domain event
        RaiseDomainEvent(new {Entity}DeletedEvent(Id, Name));

        // Soft delete
        IsDeleted = true;
    }
}
```

### Step 5: Add Global Query Filter (Soft Delete)

`src/{Solution}/Infrastructure/Persistence/Configurations/{Entity}Configuration.cs`:

```csharp
builder.Property(e => e.IsDeleted)
    .HasColumnName("is_deleted");

builder.HasQueryFilter(e => !e.IsDeleted);
```

### Step 6: Add Controller Action

`src/{Solution}/Api/Controllers/{Feature}Controller.cs`:

```csharp
[HttpDelete("{id:guid}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
{
    var result = await Sender.Send(new Delete{Entity}Command(id), ct);

    return result.IsSuccess
        ? NoContent()
        : result.Error.ToActionResult();
}
```

### Step 7: Create Unit Test

`tests/{Solution}.UnitTests/Application/Delete{Entity}CommandHandlerTests.cs`:

```csharp
using {Namespace}.Application.{Feature}.Commands.Delete{Entity};
using {Namespace}.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace {Solution}.UnitTests.Application;

public class Delete{Entity}CommandHandlerTests
{
    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Handle_WithExistingId_ShouldDeleteEntity()
    {
        await using var context = CreateContext();
        var entity = {Entity}.Create("Test", 9.99m).Value;
        context.{Entities}.Add(entity);
        await context.SaveChangesAsync();

        var handler = new Delete{Entity}CommandHandler(context);
        var command = new Delete{Entity}Command(entity.Id.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.{Entities}.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldReturnNotFound()
    {
        await using var context = CreateContext();
        var handler = new Delete{Entity}CommandHandler(context);
        var command = new Delete{Entity}Command(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({Entity}Errors.NotFound);
    }
}
```

## Checklist

- [ ] Handler load entity trước khi delete
- [ ] Trả `{Entity}Errors.NotFound` nếu entity không tồn tại
- [ ] Gọi domain method `Delete()` để raise domain event
- [ ] Dùng soft delete cho business entities
- [ ] Dùng hard delete cho master data rác (`ExecuteDeleteAsync`)
- [ ] Trả `204 NoContent` khi delete thành công

## Edge Cases

- Entity có child entities: cascade delete hoặc prevent delete.
- Entity đã bị xóa: trả `{Entity}Errors.NotFound` (không expose rằng đã tồn tại).
- Restore: tạo `Restore{Entity}Command` riêng.

## References

- `{Solution}/Domain/{Feature}/{Entity}.cs`
- `{Solution}/Application/{Feature}/Commands/Create{Entity}/Create{Entity}CommandHandler.cs`
