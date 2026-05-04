---
name: clean-architecture
description: >
  Clean Architecture for .NET 8 applications. Covers the 4-layer layout (Domain,
  Application, Infrastructure, Api), CQRS with EF Core (write) + Dapper (read),
  Repository pattern, multi-tenancy with ITenantEntity and Global Query Filter,
  and Controllers with ApiController attribute.
  Load this skill when building Clean Architecture, discussing layered architecture,
  CQRS, repository pattern, multi-tenancy, or dependency inversion.
---

# Clean Architecture

## Core Principles

1. **Dependency inversion** — All dependencies point inward. Domain has zero project references. Application references only Domain. Infrastructure references Application and Domain. Api references all but depends on abstractions. The compiler enforces this via project references.
2. **CQRS: EF Core for writes, Dapper for reads** — Command handlers load aggregates via Repository, call domain methods, then persist. Change Tracking handles dirty detection. Query handlers bypass EF Core entirely and execute raw SQL via Dapper returning DTOs directly — no entity mapping, no `.Include()` chains.
3. **Repository pattern** — Interface defined in Application layer. EF Core implementation in Infrastructure. One repository per aggregate root. Enables unit-testable command handlers without a real database.
4. **Domain owns the rules** — Business logic lives in Domain as entity methods, domain services, or specifications. Domain has no knowledge of databases, HTTP, or any framework — only pure C# and .NET primitives. Never inject `ICurrentUser`, `ITenantContext`, or `IConfiguration` into Domain.
5. **Multi-tenancy via Global Query Filter** — Tenant-scoped entities implement `ITenantEntity`. EF Core applies a Global Query Filter on `TenantId` automatically. `ICurrentUser` / `ITenantContext` live in Application; Infrastructure reads TenantId from JWT and injects into the DbContext.
6. **Infrastructure is a plugin** — EF Core, Dapper, Redis, MassTransit, Serilog — all live in Infrastructure and implement interfaces from Application. Swap implementations without touching business logic.
7. **Controllers are thin** — `[ApiController]` + `[Route("api/v{version:apiVersion}/[controller]")]`. Endpoints map HTTP → MediatR command/query → HTTP response. No business logic in controllers.

## Patterns

### Project Layout

```
src/
  MyApp.Domain/
    Entities/           # Aggregates + entities with behavior
    ValueObjects/       # Immutable records
    Events/             # Domain events (IDomainEvent)
    Exceptions/         # DomainException, NotFoundException, ConflictException
    Interfaces/         # IOrderRepository (aggregate-level, declared in Domain if domain needs it)
    Common/             # Entity base, AggregateRoot, ITenantEntity

  MyApp.Application/
    Features/
      Orders/
        Commands/
          CreateOrder/
            CreateOrderCommand.cs
            CreateOrderHandler.cs
            CreateOrderValidator.cs
        Queries/
          GetOrder/
            GetOrderQuery.cs
            GetOrderHandler.cs
            OrderDto.cs
    Abstractions/
      IOrderRepository.cs      # Repository interface (Application defines the contract)
      ICurrentUser.cs          # TenantId + UserId from JWT
      IDbConnectionFactory.cs  # Dapper connection factory
    Behaviors/
      ValidationBehavior.cs    # MediatR pipeline

  MyApp.Infrastructure/
    Persistence/
      AppDbContext.cs           # Implements EF Core, applies Global Query Filter
      Repositories/
        OrderRepository.cs      # Implements IOrderRepository
      Configurations/
        OrderConfiguration.cs
      Migrations/
    Caching/                    # IDistributedCache (Redis) decorator implementations
    Messaging/                  # MassTransit consumers, outbox
    BackgroundJobs/
    DependencyInjection.cs      # AddInfrastructure extension

  MyApp.Api/
    Controllers/
      OrdersController.cs      # [ApiController] thin controller
    Middleware/
    Extensions/
    Program.cs
```

### CQRS — Command Handler (Write Path)

Load aggregate via Repository → call domain method → Unit of Work persists via SaveChangesAsync. Change Tracking detects all mutations automatically.

```csharp
// Application/Features/Orders/Commands/CreateOrder/CreateOrderCommand.cs
public record CreateOrderCommand(Guid CustomerId, List<OrderItemDto> Items) : IRequest<Guid>;
public record OrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice);

// Application/Features/Orders/Commands/CreateOrder/CreateOrderHandler.cs
internal sealed class CreateOrderHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var order = Order.Create(
            new CustomerId(request.CustomerId),
            new TenantId(currentUser.TenantId),
            request.Items.Select(i => new OrderLine(i.ProductId, i.Quantity, i.UnitPrice)),
            clock.GetUtcNow());

        await repository.AddAsync(order, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return order.Id.Value;
    }
}
```

### CQRS — Query Handler (Read Path)

Raw SQL via Dapper → DTO. No EF Core, no `.Include()`, no entity loading. Shaped for the consumer.

```csharp
// Application/Features/Orders/Queries/GetOrder/GetOrderQuery.cs
public record GetOrderQuery(Guid OrderId) : IRequest<OrderDto?>;

public record OrderDto(Guid Id, Guid CustomerId, decimal Total, string Status, DateTimeOffset CreatedAt);

// Application/Features/Orders/Queries/GetOrder/GetOrderHandler.cs
internal sealed class GetOrderHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser) : IRequestHandler<GetOrderQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken ct)
    {
        const string sql = """
            SELECT o.id, o.customer_id, o.total, o.status, o.created_at
            FROM orders o
            WHERE o.id = @OrderId
              AND o.tenant_id = @TenantId
            """;

        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<OrderDto>(
            sql, new { request.OrderId, TenantId = currentUser.TenantId });
    }
}
```

### Repository Interface (Application Layer)

Defines the contract. Application depends on this abstraction, not on EF Core.

```csharp
// Application/Abstractions/IOrderRepository.cs
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
}

// Application/Abstractions/IUnitOfWork.cs
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

### EF Core Repository Implementation (Infrastructure Layer)

Global Query Filter for multi-tenancy is applied at the DbContext level — not in every repository.

```csharp
// Infrastructure/Persistence/AppDbContext.cs
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUser currentUser) : DbContext(options), IUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global Query Filter: all ITenantEntity queries are automatically scoped to current tenant
        foreach (var entityType in builder.Model.GetEntityTypes()
            .Where(e => typeof(ITenantEntity).IsAssignableFrom(e.ClrType)))
        {
            builder.Entity(entityType.ClrType)
                .HasQueryFilter(
                    e => EF.Property<Guid>(e, nameof(ITenantEntity.TenantId)) == currentUser.TenantId);
        }
    }
}

// Infrastructure/Persistence/Repositories/OrderRepository.cs
internal sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct) =>
        db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(Order order, CancellationToken ct)
    {
        await db.Orders.AddAsync(order, ct);
    }
}
```

### Multi-Tenancy (ITenantEntity)

```csharp
// Domain/Common/ITenantEntity.cs
public interface ITenantEntity
{
    Guid TenantId { get; }
}

// Domain/Entities/Order.cs — implements ITenantEntity
public sealed class Order : AggregateRoot, ITenantEntity
{
    private Order() { }

    public OrderId Id { get; private set; }
    public Guid TenantId { get; private set; }     // set on creation, never changed
    public CustomerId CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }

    public static Order Create(CustomerId customerId, TenantId tenantId, ...)
    {
        return new Order
        {
            Id = OrderId.New(),
            TenantId = tenantId.Value,
            CustomerId = customerId,
            Status = OrderStatus.Pending
        };
    }
}

// Application/Abstractions/ICurrentUser.cs — Infrastructure reads from JWT, Application uses the interface
public interface ICurrentUser
{
    Guid UserId { get; }
    Guid TenantId { get; }
}
```

### Controller (API Layer)

```csharp
// Api/Controllers/OrdersController.cs
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        CreateOrderCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetOrderQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
```

### Module DI Registration

Each bounded context registers its own services. Program.cs stays clean.

```csharp
// Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}

// Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddSingleton<IDbConnectionFactory>(
            new NpgsqlConnectionFactory(config.GetConnectionString("Default")!));

        return services;
    }
}

// Program.cs
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

## Anti-patterns

### EF Include Chain in Query Handler

```csharp
// DON'T — EF Include chains in query handlers load entity graphs, not read-optimized DTOs
var order = await db.Orders
    .Include(o => o.Lines).ThenInclude(l => l.Product)
    .Include(o => o.Customer)
    .FirstOrDefaultAsync(o => o.Id == id, ct);

// DO — Dapper with raw SQL returns exactly the shape the consumer needs
const string sql = "SELECT o.id, o.total, l.product_name, l.qty FROM orders o JOIN order_lines l ...";
```

### ICurrentUser / ITenantContext Injected into Domain

```csharp
// DON'T — Domain depends on Application/Infrastructure concerns
public class Order(ICurrentUser user)  // Domain cannot reference Application interfaces
{
    public static Order Create() => new() { TenantId = user.TenantId };
}

// DO — Pass TenantId as a plain value during construction
var order = Order.Create(new CustomerId(request.CustomerId), new TenantId(currentUser.TenantId), ...);
```

### IConfiguration in Domain

```csharp
// DON'T — Domain reads from configuration
public class ShippingCalculator(IConfiguration config)  // Domain depends on Infrastructure concern
{
    var maxWeight = config.GetValue<int>("Shipping:MaxWeightKg");
}

// DO — Pass configuration-derived values through Application-defined interfaces or value objects
public interface IShippingPolicy { decimal CalculateRate(Weight weight); }
```

### Anemic Domain Model

```csharp
// DON'T — entity is a data bag, all logic in handler
order.Status = OrderStatus.Cancelled;  // no invariant check, no event raised

// DO — entity encapsulates its own rules
var result = order.Cancel();           // checks status, raises OrderCancelledEvent
if (result.IsFailure) throw new DomainException(result.Error);
```

### Repository for Every Entity (Including Read Models)

```csharp
// DON'T — repository wrapping DbSet for simple lookups
public interface IProductRepository { Task<Product?> GetByIdAsync(Guid id); }  // just use Dapper for reads

// DO — repository only for aggregate write path; read path uses Dapper directly in query handler
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Write path (mutate aggregate) | Repository + UnitOfWork + EF Change Tracking |
| Read path (data for API response) | Dapper + raw SQL → DTO directly |
| When to use EF `.Include()` | Only in command handlers loading full aggregate for mutation |
| Simple CRUD / settings / master data | Plain EF Core via DbSet — no repository, no CQRS overhead |
| Multi-tenant entity | Implement `ITenantEntity`, set `TenantId` on creation, rely on Global Query Filter |
| When NOT to use CA | Simple APIs with < 5 entities, no complex business rules — use VSA instead |
| IAppDbContext vs IRepository | Repository for aggregate roots; IAppDbContext/Dapper for reads |
