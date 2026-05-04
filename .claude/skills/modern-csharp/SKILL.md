---
name: modern-csharp
description: >
  Modern C# language features for .NET 8 and C# 12. Covers primary constructors,
  collection expressions, records, pattern matching, required members, init-only
  setters, file-scoped namespaces, raw string literals, global usings, and nameof().
  Load this skill when writing any new C# code, reviewing existing code for
  modernization, using "modern C#", "C# 12", "primary constructor", "collection
  expression", "records", "pattern matching", "required members", or "init". 
  Always loaded as the baseline for all agents.
---

# Modern C# (C# 12 / .NET 8)

## Core Principles

1. **C# 12 is the target** — Use language-level constructs available in C# 12 / .NET 8 LTS. Do not use C# 13 or C# 14 features (`field` keyword, extension members) as they are not available in the target runtime.
2. **Readability over cleverness** — Pattern matching and expression-bodied members improve readability when used appropriately; deeply nested patterns do not.
3. **Value types where possible** — Prefer `record struct`, `Span<T>`, and stack allocation to reduce GC pressure.
4. **Immutability by default** — Use `record`, `readonly`, `init`, and `required` to make illegal states unrepresentable.

## Patterns

### Well-Known C# 12 Features Quick Reference

| Feature | Usage | Example |
|---------|-------|---------|
| Primary constructors | DI injection, eliminate field assignments | `public class OrderService(IOrderRepo repo, TimeProvider clock) { }` |
| Collection expressions | `[]` for all collection types + spread | `List<string> names = ["Alice", "Bob"];` / `int[] all = [..a, ..b, 99];` |
| Records | DTOs, value objects, immutable data | `public record CreateOrderRequest(string CustomerId, List<OrderItem> Items);` |
| `readonly record struct` | Small stack-allocated value types | `public readonly record struct Money(decimal Amount, string Currency);` |
| Pattern matching | Switch expressions, property patterns | `order switch { { Total: > 1000 } => "Premium", _ => "Standard" };` |
| List patterns | Deconstruct arrays/lists | `items switch { [] => "Empty", [var x] => $"One: {x}", [var f, .., var l] => $"{f}..{l}" };` |
| `Span<T>` | Zero-allocation slicing | `ReadOnlySpan<char> trimmed = input.Trim(); int.TryParse(trimmed[4..], out id);` |
| Raw string literals | Multi-line SQL, JSON, XML | `var sql = """ SELECT ... """;` / interpolated: `$$""" {"id": "{{id}}"} """;` |
| `required` members | Enforce initialization | `public required string ConnectionString { get; init; }` |
| `init`-only setters | Immutable after construction | `public string Name { get; init; }` |
| `is` pattern + extraction | Null/type/property check | `if (result is { IsSuccess: true, Value: var order }) { ... }` |
| File-scoped namespaces | Remove one level of indentation | `namespace MyApp.Orders;` |
| Global usings | Eliminate repetitive using directives | `global using MediatR;` in `GlobalUsings.cs` |
| `nameof()` | Refactor-safe string references | `nameof(DatabaseOptions.SectionName)` |

### Primary Constructors for DI

```csharp
// C# 12 — primary constructor eliminates field declarations
public class CreateOrderHandler(
    IDocumentRepository repo,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<CreateOrderHandler> logger)
    : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        logger.LogInformation("Creating order for {UserId}", currentUser.Id);
        var order = Order.Create(command.CustomerId, clock.GetUtcNow());
        repo.Add(order);
        return order.Id;
    }
}
```

### Records for DTOs and Value Objects

```csharp
// API request DTO — immutable, value equality
public record CreateOrderRequest(string CustomerId, List<OrderItem> Items);

// Strongly-typed ID — prevents mixing Guid types accidentally
public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

// Domain value object
public readonly record struct Money(decimal Amount, string Currency)
{
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return this with { Amount = Amount + other.Amount };
    }
}
```

### Required Members (C# 11/12)

Enforce that properties are set at object construction without a constructor parameter.

```csharp
// Options class — required enforces initialization in object initializer
public class DatabaseOptions
{
    public const string SectionName = "Database";

    public required string ConnectionString { get; init; }
    public int CommandTimeoutSeconds { get; init; } = 30;
    public int MaxRetryCount { get; init; } = 3;
}

// Test fixture — required + init forces explicit initialization
var options = new DatabaseOptions
{
    ConnectionString = "Host=localhost;Database=test" // required: must be set
};

// Domain entity with required properties
public class Address
{
    public required string Street { get; init; }
    public required string City { get; init; }
    public required string PostalCode { get; init; }
    public string? Country { get; init; } // optional
}
```

**Why `required` over constructor params for options**: Constructor params don't play well with `IOptions<T>` binding (needs parameterless constructor). `required` + `init` gives you compile-time safety without the constraint.

### nameof() for Refactor-Safe String References

```csharp
// Configuration section names — refactor-safe
public class JwtOptions
{
    public const string SectionName = nameof(JwtOptions); // "JwtOptions"
    public required string Key { get; init; }
}

// Validation messages — property name stays accurate after rename
throw new ArgumentException(
    "Value must be positive",
    nameof(amount)); // "amount" — updated automatically on rename

// LogContext property names
using (LogContext.PushProperty(nameof(TraceId), traceId))
{
    await next(context);
}

// Route constraints and claim names
[HttpGet("{" + nameof(id) + "}")]
public IActionResult GetById(Guid id) { }
```

### Collection Expressions (C# 12)

```csharp
// All collection types support [] syntax
int[] numbers = [1, 2, 3, 4, 5];
List<string> names = ["Alice", "Bob", "Charlie"];
ImmutableArray<int> immutable = [10, 20, 30];

// Spread operator — combine without LINQ
int[] first = [1, 2, 3];
int[] second = [4, 5, 6];
int[] combined = [..first, ..second, 99]; // [1, 2, 3, 4, 5, 6, 99]

// Empty collection
List<Order> orders = [];
```

### Pattern Matching

```csharp
// Switch expression with property patterns
string GetOrderPriority(Order order) => order switch
{
    { Total: > 10_000 } => "Enterprise",
    { Total: > 1_000 }  => "Premium",
    { IsRush: true }     => "Rush",
    _                    => "Standard"
};

// is pattern with extraction — avoids null checks + cast
if (result is { IsSuccess: true, Value: var order })
{
    logger.LogInformation("Order {OrderId} created", order.Id);
}

// List patterns
string Describe(int[] values) => values switch
{
    []           => "empty",
    [var x]      => $"single: {x}",
    [var f, var l] => $"two: {f}, {l}",
    [var f, .., var l] => $"many: {f}..{l}"
};
```

### Raw String Literals for SQL and JSON

```csharp
// Multi-line SQL — no escaping, IDE highlights syntax
var sql = """
    SELECT d.id, d.title, d.status, d.created_at
    FROM documents d
    WHERE d.tenant_id = @TenantId
      AND d.is_deleted = false
    ORDER BY d.created_at DESC
    LIMIT @PageSize OFFSET @Offset
    """;

// Interpolated raw string — double $ for interpolation, no escaping needed
var json = $$"""
    {
        "orderId": "{{orderId}}",
        "tenantId": "{{tenantId}}"
    }
    """;
```

### File-Scoped Namespaces and Global Usings

```csharp
// Every file — eliminates one level of indentation
namespace MyApp.Orders.Application.Commands;

// GlobalUsings.cs — project-wide imports
global using MediatR;
global using FluentValidation;
global using Microsoft.Extensions.Logging;
global using MyApp.Common.Results;
```

## Anti-patterns

### Don't Use Obsolete Patterns When Modern Alternatives Exist

```csharp
// BAD — old-style collection initialization
var list = new List<int>() { 1, 2, 3 };
// GOOD
List<int> list = [1, 2, 3];

// BAD — manual backing field when init property works
private string _name;
public string Name { get => _name; set => _name = value ?? throw new ArgumentNullException(); }
// GOOD — validate in constructor or use records
public record Product(string Name, decimal Price);

// BAD — Tuple instead of record for domain types
(string Name, decimal Price) product = ("Widget", 9.99m);
// GOOD
public record Product(string Name, decimal Price);
```

### Don't Use C# 13/14 Features (Not Available in .NET 8)

```csharp
// BAD — `field` keyword is C# 14 only
public string Name
{
    get => field;
    set => field = value?.Trim() ?? throw new ArgumentNullException();
}

// BAD — extension members are C# 14 only
public extension OrderExtensions for Order
{
    public bool IsHighValue => Total > 1000m;
}

// GOOD — use explicit backing field (C# 12 compatible)
private string _name = string.Empty;
public string Name
{
    get => _name;
    set => _name = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
}

// GOOD — use static extension methods (all C# versions)
public static class OrderExtensions
{
    public static bool IsHighValue(this Order order) => order.Total > 1000m;
}
```

### Don't Over-Pattern-Match

```csharp
// BAD — deeply nested pattern that's hard to read
if (order is { Customer: { Address: { Country: { Code: "US" } } } })

// GOOD — extract to a clear method or use sequential checks
if (order.Customer.Address.Country.Code == "US")
```

### Don't Use `var` When the Type Is Not Obvious

```csharp
// BAD — what type is this?
var result = Process(order);

// GOOD — explicit type when not obvious
Result<Order> result = Process(order);

// Also GOOD — var is fine when type is apparent from the right-hand side
var orders = new List<Order>();
var orderId = Guid.NewGuid();
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| DTO / API contract | `record` (reference type) |
| Small value object (2-3 fields) | `readonly record struct` |
| Service with DI | Primary constructor |
| Collection creation | Collection expression `[]` |
| Property with mandatory initialization | `required` + `init` |
| Multi-line SQL / JSON / XML | Raw string literal `"""` |
| Slicing strings/arrays | `Span<T>` or `ReadOnlySpan<T>` |
| Type checking + extraction | Pattern matching with `is` / `switch` |
| Refactor-safe string | `nameof()` |
| Project-wide imports | `global using` in `GlobalUsings.cs` |
| Adding methods to external types | Static extension methods |
