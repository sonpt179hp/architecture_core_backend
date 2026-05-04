# Skill: Add Event Handler (MassTransit Consumer)

## Purpose

Scaffold MassTransit consumer cho một integration event với Idempotent Consumer pattern.
Đảm bảo không xử lý trùng message, có dead-letter queue fallback, và có tracing/logging.

## Convention Mapping

| Artifact | Convention | Pattern |
|---|---|---|
| **Consumer** | `IConsumer<TMessage>` interface | |
| **Integration Event** | `record` với Guid, DateTime, payload fields | |
| **Consumer Definition** | `ConsumerDefinition<T>` | Retry policy, DLQ |
| **Namespace** | `Infrastructure.Messaging.Consumers` | |
| **ProcessedMessages** | `(MessageId, ConsumerName)` composite key | Idempotent check |

## Project Structure

```
src/
├── Domain/
│   └── {Feature}/
│       └── Events/
│           └── {Entity}{Action}Event.cs
└── Infrastructure/
    ├── Messaging/
    │   ├── Consumers/
    │   │   ├── {EventName}Consumer.cs
    │   │   └── {EventName}ConsumerDefinition.cs
    │   ├── ProcessedMessage.cs
    │   └── ProcessedMessageStore.cs
    └── DependencyInjection.cs  ← AddMassTransit()
```

## Instructions

**Input cần từ user:** Tên event, service nguồn, hành động khi nhận event.

### Step 1: Create Integration Event Contract

Nếu event từ service khác (shared contract):

`src/{Solution}.Contracts/Events/{EventName}Event.cs`:

```csharp
namespace {Namespace}.Contracts.Events;

public sealed record {EventName}Event(
    Guid EventId,
    DateTime OccurredAt,
    Guid {SourceEntity}Id,
    string Name)
{
    // Constructor không tham số cho deserialization
    public {EventName}Event() : this(Guid.Empty, DateTime.UtcNow, Guid.Empty, string.Empty) { }
}
```

Nếu event nội bộ:

`src/{Solution}/Domain/{Feature}/Events/{Entity}{Action}Event.cs`:

```csharp
using {Namespace}.Domain.Primitives;

namespace {Namespace}.Domain.{Feature}.Events;

public sealed class {Entity}{Action}Event : IDomainEvent
{
    public {Entity}{Action}Event(Guid {entity}Id, string name)
    {
        {Entity}Id = {entity}Id;
        Name = name;
        OccurredAt = DateTime.UtcNow;
    }

    public Guid {Entity}Id { get; }
    public string Name { get; }
    public DateTime OccurredAt { get; }
}
```

### Step 2: Create Consumer

`src/{Solution}/Infrastructure/Messaging/Consumers/{EventName}Consumer.cs`:

```csharp
using MassTransit;
using Microsoft.Extensions.Logging;

namespace {Namespace}.Infrastructure.Messaging.Consumers;

public sealed class {EventName}Consumer : IConsumer<{EventName}Event>
{
    private readonly ILogger<{EventName}Consumer> _logger;

    public {EventName}Consumer(ILogger<{EventName}Consumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<{EventName}Event> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing {EventName} with EventId {EventId}",
            nameof({EventName}Event),
            message.EventId);

        try
        {
            // TODO: Thực hiện side-effect
            // Ví dụ: cập nhật cache, gửi notification, sync data...

            _logger.LogInformation(
                "Successfully processed {EventName} EventId {EventId}",
                nameof({EventName}Event),
                message.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process {EventName} EventId {EventId}",
                nameof({EventName}Event),
                message.EventId);
            throw;
        }
    }
}
```

### Step 3: Create Consumer Definition

`src/{Solution}/Infrastructure/Messaging/Consumers/{EventName}ConsumerDefinition.cs`:

```csharp
using MassTransit;

namespace {Namespace}.Infrastructure.Messaging.Consumers;

public sealed class {EventName}ConsumerDefinition : ConsumerDefinition<{EventName}Consumer>
{
    public {EventName}ConsumerDefinition()
    {
        EndpointName = $"{{solution}}.{{eventName}}";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<{EventName}Consumer> consumerConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r =>
            r.Exponential(3, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100)));
    }
}
```

### Step 4: Register Consumer in MassTransit

```csharp
public static IServiceCollection AddMassTransitMessaging(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddMassTransit(x =>
    {
        x.AddConsumer<{EventName}Consumer, {EventName}ConsumerDefinition>();

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(configuration.GetSection("RabbitMQ:Host").Value, host =>
            {
                host.Username(configuration.GetSection("RabbitMQ:Username").Value);
                host.Password(configuration.GetSection("RabbitMQ:Password").Value);
            });

            cfg.ReceiveEndpoint("{{solution}}.{{eventName}}", e =>
            {
                e.ConfigureConsumer<{EventName}Consumer>(context);
            });
        });
    });

    return services;
}
```

### Step 5: Add ProcessedMessages Table (Idempotent Consumer)

`src/{Solution}/Infrastructure/Messaging/ProcessedMessage.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace {Namespace}.Infrastructure.Messaging;

public class ProcessedMessage
{
    public Guid MessageId { get; private set; }
    public string ConsumerName { get; private set; } = default!;
    public DateTime ConsumedAt { get; private set; } = DateTime.UtcNow;

    private ProcessedMessage() { }

    public static ProcessedMessage Create(Guid messageId, string consumerName) =>
        new()
        {
            MessageId = messageId,
            ConsumerName = consumerName
        };
}

internal sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");

        builder.HasKey(x => new { x.MessageId, x.ConsumerName });

        builder.Property(x => x.MessageId)
            .HasColumnName("message_id");

        builder.Property(x => x.ConsumerName)
            .HasColumnName("consumer_name")
            .HasMaxLength(200);

        builder.Property(x => x.ConsumedAt)
            .HasColumnName("consumed_at");
    }
}
```

`src/{Solution}/Infrastructure/Messaging/ProcessedMessageStore.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace {Namespace}.Infrastructure.Messaging;

public interface IProcessedMessageStore
{
    Task<bool> ExistsAsync(Guid messageId, string consumerName, CancellationToken ct = default);
    Task MarkAsync(Guid messageId, string consumerName, CancellationToken ct = default);
}

public sealed class ProcessedMessageStore : IProcessedMessageStore
{
    private readonly ApplicationDbContext _dbContext;

    public ProcessedMessageStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ExistsAsync(Guid messageId, string consumerName, CancellationToken ct = default) =>
        await _dbContext.ProcessedMessages
            .AnyAsync(x => x.MessageId == messageId && x.ConsumerName == consumerName, ct);

    public async Task MarkAsync(Guid messageId, string consumerName, CancellationToken ct = default)
    {
        var message = ProcessedMessage.Create(messageId, consumerName);
        _dbContext.ProcessedMessages.Add(message);
        await _dbContext.SaveChangesAsync(ct);
    }
}
```

## Checklist

- [ ] Integration event là `record` với constructor không tham số cho deserialization
- [ ] Consumer implement `IConsumer<TEvent>`
- [ ] ConsumerDefinition cấu hình retry policy (exponential, transient errors only)
- [ ] DLQ được configure cho business errors
- [ ] Structured logging với EventId, consumer name

## Edge Cases

- Consumer gọi HTTP service: wrap trong Polly retry.
- Side-effect là gửi email: dùng Outbox pattern.
- Event volume cao: dùng `IConsumer<Batch<T>>` cho batch processing.

## References

- `ai-rules/02-cqrs-pattern.md`
- `ai-rules/05-resilience.md`
- `ai-rules/06-observability.md`
- `ai-rules/08-efcore.md`
