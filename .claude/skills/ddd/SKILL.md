---
name: ddd
description: >
  Domain-Driven Design tactical patterns for .NET 8 applications. Covers aggregates,
  aggregate roots, value objects, domain events, domain services, strongly-typed IDs
  (Guid-based), multi-tenancy with ITenantEntity and EF Core Global Query Filter,
  and repository patterns for aggregate persistence.
  Load this skill when implementing DDD, working with aggregates, value objects,
  domain events, bounded contexts, multi-tenancy, ITenantEntity, or when the
  architecture-advisor recommends DDD + Clean Architecture. Pair with the clean-architecture skill.
---

# Domain-Driven Design (DDD)

## Core Principles

1. **Aggregates define consistency boundaries** — An aggregate is a cluster of entities and value objects treated as a single unit for data changes. All invariants within an aggregate are enforced in a single transaction. Cross-aggregate consistency is eventual.
2. **Value objects over primitives** — Replace primitive obsession with value objects. `Money`, `EmailAddress`, `OrderNumber` are not strings — they carry validation, equality, and behavior. Use C# records for immutable value objects.
3. **Strongly-typed IDs** — Every aggregate root gets its own ID type (e.g., `DocumentId`, `CustomerId`) wrapping a `Guid`. Prevents mixing up IDs across entity types. Use `readonly record struct` for zero-allocation, stack-allocated IDs.
4. **Domain events decouple side effects** — When something meaningful happens (OrderPlaced, DocumentPublished), raise a domain event. Side effects (send email, update read model, notify another aggregate) subscribe. The aggregate stays focused on its own rules.
5. **Aggregate root is the sole entry point** — External code accesses an aggregate only through its root entity. Child entities are never loaded or modified independently. The root enforces all invariants for the entire aggregate.
6. **Repositories persist aggregates, not individual entities** — One repository per aggregate root. The repository loads and saves the entire aggregate as a unit. No repository for child entities. For simple CRUD (Master Data, Settings, Lookup tables), DDD/Repository is over-engineering — use plain EF Core `DbSet<T>` directly.
7. **Multi-tenancy belongs to Infrastructure, not Domain** — `ITenantEntity` is a marker interface in Domain that exposes `TenantId`. EF Core's Global Query Filter (Infrastructure) enforces tenant isolation automatically. Domain logic must never read `TenantId` from a service — it is set on creation and never changed.

## Patterns

### Aggregate Root

```csharp
// Domain/Orders/Order.cs
public sealed class Order : AggregateRoot, ITenantEntity
{
    private readonly List<OrderLine> _lines = [];

    private Order() { }

    public OrderId Id { get; private set; }
    public Guid TenantId { get; private set; }        // set on creation, immutable
    public CustomerId CustomerId { get; private set; }
    public Money Total { get; private set; } = Money.Zero("USD");
    public OrderStatus Status { get; private set; }
    public DateTimeOffset PlacedAt { get; private set; }
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public static Order Place(CustomerId customerId, Guid tenantId, DateTimeOffset now)
    {
        var order = new Order
        {
            Id = OrderId.New(),
            TenantId = tenantId,
            CustomerId = customerId,
            Status = OrderStatus.Placed,
            PlacedAt = now
        };

        order.RaiseDomainEvent(new OrderPlaced(order.Id, customerId, tenantId, now));
        return order;
    }

    public Result AddLine(ProductId productId, int quantity, Money unitPrice)
    {
        if (Status is not OrderStatus.Placed)
            return Result.Failure("Cannot modify a confirmed or cancelled order.");

        if (quantity <= 0)
            return Result.Failure("Quantity must be positive.");

        var existing = _lines.FirstOrDefault(l => l.ProductId == productId);
        if (existing is not null)
            existing.IncreaseQuantity(quantity);
        else
            _lines.Add(new OrderLine(productId, quantity, unitPrice));

        RecalculateTotal();
        return Result.Success();
    }

    public Result Confirm()
    {
        if (Status is not OrderStatus.Placed)
            return Result.Failure("Only placed orders can be confirmed.");

        if (_lines.Count == 0)
            return Result.Failure("Cannot confirm an order with no lines.");

        Status = OrderStatus.Confirmed;
        RaiseDomainEvent(new OrderConfirmed(Id, TenantId));
        return Result.Success();
    }

    private void RecalculateTotal() =>
        Total = _lines.Aggregate(Money.Zero(Total.Currency), (sum, l) => sum + l.Subtotal);
}
```

### Multi-Tenancy: ITenantEntity + Global Query Filter

```csharp
// Domain/Common/ITenantEntity.cs — marker interface, lives in Domain
public interface ITenantEntity
{
    Guid TenantId { get; }
}

// Any entity that is tenant-scoped implements ITenantEntity:
// public sealed class Document : AggregateRoot, ITenantEntity { ... }
// public sealed class Product : Entity, ITenantEntity { ... }

// Infrastructure/Persistence/AppDbContext.cs — Global Query Filter applied automatically
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUser currentUser) : DbContext(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply tenant filter to every entity that implements ITenantEntity
        // Why: eliminates the risk of forgetting a WHERE tenant_id = ... clause
        foreach (var entityType in builder.Model.GetEntityTypes()
            .Where(e => typeof(ITenantEntity).IsAssignableFrom(e.ClrType)))
        {
            builder.Entity(entityType.ClrType)
                .HasQueryFilter(
                    e => EF.Property<Guid>(e, nameof(ITenantEntity.TenantId)) == currentUser.TenantId);
        }
    }
}
```

### Strongly-Typed IDs

Prevent mixing up GUIDs from different entities. Use `readonly record struct` for zero-allocation.

```csharp
// Domain/Common/StronglyTypedIds.cs
public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct CustomerId(Guid Value)
{
    public static CustomerId New() => new(Guid.NewGuid());
}

public readonly record struct ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.NewGuid());
}

public readonly record struct TenantId(Guid Value)
{
    public static TenantId New() => new(Guid.NewGuid());
}

// EF Core value converter (in IEntityTypeConfiguration<T>)
builder.Property(o => o.Id)
    .HasConversion(id => id.Value, value => new OrderId(value));

builder.Property(o => o.CustomerId)
    .HasConversion(id => id.Value, value => new CustomerId(value));
```

Why strongly-typed IDs? `void Ship(Guid orderId, Guid customerId)` compiles with swapped args. `void Ship(OrderId orderId, CustomerId customerId)` does not.

### Value Objects as Records

```csharp
// Domain/Common/Money.cs
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0, currency);

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot add {left.Currency} and {right.Currency}.");
        return new Money(left.Amount + right.Amount, left.Currency);
    }
}
```

### Domain Event Dispatching

Raise events in the aggregate; dispatch via MassTransit Outbox in SaveChangesAsync for guaranteed delivery.

```csharp
// Domain/Common/AggregateRoot.cs
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
}

// Domain/Orders/Events/OrderPlaced.cs
public sealed record OrderPlaced(
    OrderId OrderId,
    CustomerId CustomerId,
    Guid TenantId,
    DateTimeOffset PlacedAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => PlacedAt;
}

// Infrastructure/Persistence/AppDbContext.cs — dispatch after successful persist
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var aggregates = ChangeTracker.Entries<AggregateRoot>()
        .Where(e => e.Entity.DomainEvents.Count > 0)
        .Select(e => e.Entity)
        .ToList();

    var result = await base.SaveChangesAsync(ct);

    foreach (var @event in aggregates.SelectMany(a => a.DomainEvents))
        await _publisher.Publish(@event, ct);   // MediatR IPublisher; MassTransit Outbox handles integration events

    foreach (var aggregate in aggregates)
        aggregate.ClearDomainEvents();

    return result;
}
```

### Domain Services

For logic that does not belong to a single aggregate and requires coordination:

```csharp
// Domain/Orders/Services/PricingService.cs
// Takes domain interfaces as parameters; returns domain value objects
public sealed class PricingService(IDiscountPolicy discountPolicy)
{
    public Money CalculatePrice(ProductId productId, int quantity, Money unitPrice, CustomerId customerId)
    {
        var subtotal = new Money(unitPrice.Amount * quantity, unitPrice.Currency);
        var discount = discountPolicy.GetDiscount(customerId, productId, quantity);
        return new Money(subtotal.Amount * (1 - discount), subtotal.Currency);
    }
}
```

## Anti-patterns

### Oversized Aggregates

```csharp
// DON'T — Customer aggregate owns everything the customer touches (bloated, locks too much)
public class Customer : AggregateRoot
{
    public List<Order> Orders { get; } = [];      // separate aggregate
    public List<Payment> Payments { get; } = [];  // separate aggregate
    public ShoppingCart Cart { get; set; }        // separate aggregate
}

// DO — small, focused aggregates linked by ID
public class Customer : AggregateRoot, ITenantEntity
{
    public CustomerId Id { get; private set; }
    public Guid TenantId { get; private set; }
    public CustomerName Name { get; private set; }
    public EmailAddress Email { get; private set; }
    // Orders, Payments, Cart are separate aggregates referencing CustomerId
}
```

### Anemic Aggregates

```csharp
// DON'T — aggregate is a data bag; service does all the work
order.Status = OrderStatus.Confirmed;  // public setter, no invariant check, no event raised
order.Lines.Add(newLine);             // no validation

// DO — aggregate encapsulates rules
var result = order.Confirm();         // validates status, raises OrderConfirmed event
order.AddLine(productId, qty, price); // validates, recalculates total
```

### TenantId Set from Outside Domain

```csharp
// DON'T — TenantId set directly from an untrusted request
var order = new Order { TenantId = request.TenantId };  // spoofable

// DO — TenantId flows from ICurrentUser in Application layer, passed as value to Domain factory
var order = Order.Place(customerId, currentUser.TenantId, clock.GetUtcNow());
```

### Value Objects with Identity

```csharp
// DON'T — value object with an Id (it's an entity)
public record Address { public Guid Id { get; init; } public string Street { get; init; } }

// DO — value objects defined by their attributes, no Id
public record Address(string Street, string City, string PostalCode, string Country);
```

### Domain Events for Intra-Aggregate Logic

```csharp
// DON'T — events for logic within the same aggregate (unnecessary indirection)
order.RaiseDomainEvent(new OrderLineAdded(line));
// Then a handler recalculates the total... but you're still in the same aggregate

// DO — call the private method directly
_lines.Add(line);
RecalculateTotal();
```

### DDD for Simple CRUD

```csharp
// DON'T — applying DDD/Aggregate/Repository to a simple lookup table
public class CountryAggregate : AggregateRoot { ... }
public interface ICountryRepository { Task<Country?> GetByCodeAsync(string code); }

// DO — use plain EF Core directly in the handler; no aggregate, no repository
var country = await db.Countries.FirstOrDefaultAsync(c => c.Code == request.Code, ct);
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| When to use DDD | Complex domain with business rules, invariants, cross-entity constraints |
| When NOT to use DDD | Simple CRUD, settings, lookup tables, audit logs, read models — use plain EF Core |
| Aggregate size | Keep small — 1 root entity + 0-3 child entities. Load the whole aggregate every time |
| Strongly-typed IDs | Always for aggregate root IDs that cross boundaries. Optional for internal child entity IDs |
| Multi-tenant entity | Implement `ITenantEntity`, set `TenantId` on factory method, rely on Global Query Filter |
| Domain events vs integration events | Domain events: within bounded context, same transaction (via MediatR). Integration events: cross-context, via MassTransit Outbox |
| Value objects | Any concept with validation rules or equality based on attributes, not identity |
| Domain services | Only when logic requires multiple aggregates or external data the aggregate must not know about |
| Repository vs DbSet | Repository per aggregate root for write path. Dapper for read path. DbSet directly for simple master data |
