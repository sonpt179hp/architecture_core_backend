# Skill: Setup Background Job Infrastructure

## Purpose

Dựng Outbox Pattern để đảm bảo domain events không bị mất khi broker down.
Domain Events được raise từ Aggregate Roots, lưu vào Outbox table cùng transaction,
sau đó `OutboxProcessor` BackgroundService đọc và publish lên message broker.

## Architecture Flow

```
Command Handler
    │
    ├─► Aggregate.Create() → sinh Domain Events
    │       └─► entity.RaiseDomainEvent(new {Entity}{Action}Event(...))
    │
    ├─► dbContext.SaveChangesAsync()
    │       ├─► Domain Events được lưu vào _domainEvents list
    │       └─► OutboxMessage được persist cùng transaction
    │
    └─► OutboxProcessor (BackgroundService)
            ├─► Poll: SELECT * FROM outbox_messages WHERE ProcessedAt IS NULL
            ├─► Deserialize event payload
            ├─► Publish lên MassTransit/IBus
            └─► Update ProcessedAt = now
```

## Convention Mapping

| Artifact | Convention | Pattern |
|---|---|---|
| **IDomainEvent** | Empty marker interface | |
| **Domain Event** | Implement `IDomainEvent`, chỉ chứa snapshot data | |
| **OutboxMessage** | Entity với Id, Type, Payload, CreatedAt, ProcessedAt, Error, RetryCount | |
| **OutboxProcessor** | `BackgroundService` polling `OutboxMessages` table | |
| **Dispatcher** | `SaveChangesInterceptor` chuyển Domain Events → Outbox | |

## Project Structure

```
src/
├── Domain/
│   └── Primitives/
│       ├── Entity.cs          ← RaiseDomainEvent(), ClearDomainEvents()
│       └── IDomainEvent.cs    ← Marker interface
└── Infrastructure/
    ├── Outbox/
    │   ├── OutboxMessage.cs
    │   └── OutboxMessageConfiguration.cs
    ├── Persistence/
    │   └── Interceptors/
    │       └── DomainEventsDispatcherInterceptor.cs
    └── BackgroundJobs/
        └── OutboxProcessor.cs
```

## Instructions

**Input:** Tên bounded context, loại background jobs cần.

### Step 1: Create OutboxMessage Entity

`src/{Solution}/Infrastructure/Outbox/OutboxMessage.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace {Namespace}.Infrastructure.Outbox;

public class OutboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Type { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(IDomainEvent domainEvent)
    {
        var type = domainEvent.GetType().AssemblyQualifiedName!;
        return new OutboxMessage
        {
            Type = type,
            Payload = System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
        };
    }
}

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(x => x.Error)
            .HasColumnName("error")
            .HasMaxLength(2000);

        builder.Property(x => x.RetryCount)
            .HasColumnName("retry_count");

        builder.HasIndex(x => x.ProcessedAt)
            .HasFilter("processed_at IS NULL")
            .HasDatabaseName("ix_outbox_unprocessed");
    }
}
```

### Step 2: Create Domain Events Dispatcher Interceptor

`src/{Solution}/Infrastructure/Persistence/Interceptors/DomainEventsDispatcherInterceptor.cs`:

```csharp
using {Namespace}.Domain.Primitives;
using {Namespace}.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace {Namespace}.Infrastructure.Persistence.Interceptors;

public sealed class DomainEventsDispatcherInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            DispatchDomainEvents(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void DispatchDomainEvents(DbContext context)
    {
        var entitiesWithEvents = context.ChangeTracker
            .Entries<Entity<Guid>>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count != 0)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            var domainEvents = entity.DomainEvents.ToList();
            entity.ClearDomainEvents();

            foreach (var domainEvent in domainEvents)
            {
                var outboxMessage = OutboxMessage.Create(domainEvent);
                context.Set<OutboxMessage>().Add(outboxMessage);
            }
        }
    }
}
```

### Step 3: Add Outbox DbSet to ApplicationDbContext

```csharp
public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<{Entity}> {Entities} => Set<{Entity}>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

### Step 4: Register Interceptor in DI

```csharp
services.AddSingleton<DomainEventsDispatcherInterceptor>();

services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var dispatcherInterceptor = sp.GetRequiredService<DomainEventsDispatcherInterceptor>();

    options
        .UseNpgsql(connectionString)
        .AddInterceptors(dispatcherInterceptor);
});
```

### Step 5: Create OutboxProcessor BackgroundService

`src/{Solution}/Infrastructure/BackgroundJobs/OutboxProcessor.cs`:

```csharp
using {Namespace}.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace {Namespace}.Infrastructure.BackgroundJobs;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 100;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type);
                if (eventType is not null)
                {
                    var domainEvent = System.Text.Json.JsonSerializer.Deserialize(
                        message.Payload, eventType);

                    _logger.LogInformation(
                        "Publishing outbox message {MessageId} of type {MessageType}",
                        message.Id, message.Type);

                    // TODO: await _bus.Publish(domainEvent, ct);
                }

                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                _logger.LogWarning(ex,
                    "Failed to process outbox message {MessageId}, retry {RetryCount}",
                    message.Id, message.RetryCount);

                if (message.RetryCount >= 5)
                {
                    message.ProcessedAt = DateTime.UtcNow;
                    _logger.LogError(ex,
                        "Outbox message {MessageId} exceeded max retries",
                        message.Id);
                }
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
```

### Step 6: Register BackgroundService

```csharp
services.AddHostedService<OutboxProcessor>();
```

### Step 7: Create Migration

```bash
dotnet ef migrations add AddOutboxMessages --project src/{Solution}.Infrastructure --output-dir Persistence/Migrations
```

## Checklist

- [ ] `OutboxMessage` entity có cấu hình Fluent API với `HasFilter` index
- [ ] `DomainEventsDispatcherInterceptor` convert Domain Events → OutboxMessages trong cùng transaction
- [ ] `OutboxProcessor` dùng `IServiceScopeFactory`, không inject DbContext trực tiếp
- [ ] Retry có giới hạn (MaxRetry = 5)
- [ ] SKIP LOCKED hoặc batch locking để tránh nhiều instance xử lý cùng message

## Edge Cases

- MassTransit Outbox built-in: dùng `cfg.UseMessageData()` và `cfg.AddEntityFrameworkOutbox()` thay vì tạo thủ công.
- Event schema thay đổi: dùng `AssemblyQualifiedName` để deserialize.
- Kafka broker: thay `IBus.Publish` bằng `ITopicProducer<TMessage>`.
- Job volume cao: tăng batch size, giảm polling interval.

## References

- `{Solution}/Domain/Primitives/Entity.cs` — RaiseDomainEvent() và ClearDomainEvents()
- `{Solution}/Domain/Primitives/IDomainEvent.cs` — Domain event marker
