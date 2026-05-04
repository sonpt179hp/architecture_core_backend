# Skill: Add Update Command

## Purpose

Scaffold Update command cho một aggregate đã tồn tại. Load entity trước, gọi domain method, rồi persist.
Follow the same pattern as `CreateCommand`.

## Convention Mapping

| Artifact | Convention | Pattern |
|---|---|---|
| **Mediator** | `Mediator` (Arch.Ext) | |
| **Namespace** | `{Namespace}.{Feature}.Commands.Update{Entity}` | |
| **Command** | `record Update{Entity}Command(Guid Id, ...)` | |
| **Handler** | Load → domain method → SaveChangesAsync → Result | |
| **Return type** | `Result` | No content on success |

## Project Structure

```
src/
└── Application/
    └── {Feature}/
        └── Commands/
            └── Update{Entity}/
                ├── Update{Entity}Command.cs
                ├── Update{Entity}CommandHandler.cs
                └── Update{Entity}CommandValidator.cs
```

## Instructions

**Input cần từ user:** Tên entity, bounded context, các fields cần update.

### Step 1: Create Update Command

`src/{Solution}/Application/{Feature}/Commands/Update{Entity}/Update{Entity}Command.cs`:

```csharp
using {Namespace}.Application.Abstractions.Messaging;
using {Namespace}.Domain.Common;

namespace {Namespace}.Application.{Feature}.Commands.Update{Entity};

public sealed record Update{Entity}Command(
    Guid Id,
    string Name,
    string Description,
    decimal Price)
    : ICommand;
```

### Step 2: Create Validator

`src/{Solution}/Application/{Feature}/Commands/Update{Entity}/Update{Entity}CommandValidator.cs`:

```csharp
using FluentValidation;

namespace {Namespace}.Application.{Feature}.Commands.Update{Entity};

public sealed class Update{Entity}CommandValidator : AbstractValidator<Update{Entity}Command>
{
    public Update{Entity}CommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Entity ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0);
    }
}
```

### Step 3: Create Handler

`src/{Solution}/Application/{Feature}/Commands/Update{Entity}/Update{Entity}CommandHandler.cs`:

```csharp
using {Namespace}.Application.Abstractions.Data;
using {Namespace}.Application.Abstractions.Messaging;
using {Namespace}.Domain.Common;
using {Namespace}.Domain.{Feature};
using {Namespace}.Domain.{Feature}.Errors;
using Microsoft.EntityFrameworkCore;

namespace {Namespace}.Application.{Feature}.Commands.Update{Entity};

internal sealed class Update{Entity}CommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<Update{Entity}Command>
{
    public async ValueTask<Result> Handle(
        Update{Entity}Command command,
        CancellationToken cancellationToken)
    {
        var {entity}Id = new {Entity}Id(command.Id);
        var {entity} = await dbContext.{Entities}
            .FirstOrDefaultAsync(e => e.Id == {entity}Id, cancellationToken);

        if ({entity} is null)
        {
            return {Entity}Errors.NotFound;
        }

        var result = {entity}.Update(command.Name, command.Description, command.Price);

        if (result.IsFailure)
        {
            return result;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

**Handler pattern bắt buộc:**

```
1. Load entity bằng FirstOrDefaultAsync
2. Nếu null → return {Entity}Errors.NotFound
3. Gọi domain method → trả Result
4. Nếu IsFailure → return result
5. SaveChangesAsync → EF Core tự track changes
6. return Result.Success()
```

### Step 4: Add Controller Action

`src/{Solution}/Api/Controllers/{Feature}Controller.cs`:

```csharp
[HttpPut("{id:guid}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Update(
    Guid id,
    [FromBody] Update{Entity}Command command,
    CancellationToken ct)
{
    if (id != command.Id)
    {
        return BadRequest("Route ID must match command ID.");
    }

    var result = await Sender.Send(command, ct);

    return result.IsSuccess
        ? NoContent()
        : result.Error.ToActionResult();
}
```

### Step 5: Create Unit Test

`tests/{Solution}.UnitTests/Application/Update{Entity}CommandHandlerTests.cs`:

```csharp
using {Namespace}.Application.{Feature}.Commands.Update{Entity};
using {Namespace}.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace {Solution}.UnitTests.Application;

public class Update{Entity}CommandHandlerTests
{
    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateEntity()
    {
        await using var context = CreateContext();
        var entity = {Entity}.Create("Original", 9.99m).Value;
        context.{Entities}.Add(entity);
        await context.SaveChangesAsync();

        var handler = new Update{Entity}CommandHandler(context);
        var command = new Update{Entity}Command(entity.Id.Value, "Updated", "desc", 19.99m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        entity.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldReturnNotFound()
    {
        await using var context = CreateContext();
        var handler = new Update{Entity}CommandHandler(context);
        var command = new Update{Entity}Command(Guid.NewGuid(), "Updated", "desc", 9.99m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({Entity}Errors.NotFound);
    }
}
```

## Checklist

- [ ] Handler load entity trước khi update
- [ ] Trả `{Entity}Errors.NotFound` nếu entity không tồn tại
- [ ] Gọi domain method cho business validation
- [ ] Validator kiểm tra `Id` không empty
- [ ] Controller validate route ID match command ID
- [ ] Trả `204 NoContent` khi update thành công

## Edge Cases

- Optimistic concurrency: thêm concurrency token check, trả `{Entity}Errors.ConcurrencyConflict`.
- Partial update: command với optional fields (`string? Name = null`).
- Nhiều behaviors: tạo riêng command cho từng behavior.

## References

- `{Solution}/Application/{Feature}/Commands/Create{Entity}/Create{Entity}CommandHandler.cs`
- `{Solution}/Domain/{Feature}/{Entity}.cs`
