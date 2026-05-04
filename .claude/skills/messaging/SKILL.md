---
name: messaging
description: >
  Asynchronous messaging patterns for .NET 8 LTS applications. Covers MassTransit
  with RabbitMQ (MIT licensed, recommended default), Outbox pattern (save OutboxMessage
  in same DB transaction as aggregate, SELECT FOR UPDATE SKIP LOCKED polling),
  Inbox pattern (idempotent consumers via MessageId in ProcessedMessages), Dead Letter
  Queue (DLQ), ConsumerDefinition retry configuration, and broker setup.
  Load this skill when implementing event-driven communication, background processing,
  or when the user mentions "MassTransit", "message bus", "RabbitMQ", "Azure Service Bus",
  "event", "publish", "consumer", "outbox", "inbox", "idempotent consumer",
  "dead letter", "DLQ", "integration event", "queue", or "pub/sub".
---

# Messaging (.NET 8)

## Core Principles

1. **MassTransit with RabbitMQ is the default** — MIT licensed, production-proven, supports RabbitMQ and Azure Service Bus. Wolverine requires a commercial license from v9 and is not recommended for new projects.
2. **Outbox pattern for reliable publishing** — Save `OutboxMessage` in the same DB transaction as the aggregate. A `BackgroundService` polls with `SELECT FOR UPDATE SKIP LOCKED`, publishes to the broker, then marks `ProcessedAt`. Guarantees exactly-once publishing with no dual-write problem.
3. **Inbox pattern for idempotent consumers** — Every consumer checks `MessageId` in the `ProcessedMessages` table before executing side-effects. Write `MessageId` and side-effects in the same transaction. Skip if already processed.
4. **DLQ for every consumer** — Configure `UseDeadLetterQueueDeadLetterTransport()` in `ConsumerDefinition`. Failed messages after max retries go to DLQ — never lost, never blocking the queue.
5. **Messages are contracts** — Put message types in a shared `Contracts` project. Simple records with primitive types only. No behavior, no methods, no domain logic in messages.

## Patterns

### MassTransit Setup

```csharp
// Program.cs
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumers(typeof(Program).Assembly);
    x.AddConsumerDefinitions(typeof(Program).Assembly); // picks up ConsumerDefinition<T>

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"]);
            h.Password(builder.Configuration["RabbitMq:Password"]);
        });
        cfg.ConfigureEndpoints(context);
    });
});
```

### Publishing via Outbox (Reliable)

Never publish directly to the broker from a command handler. Save an `OutboxMessage` in the same transaction as the aggregate instead.

```csharp
// Message contract — shared Contracts project
public record OrderCreated(Guid OrderId, string CustomerId, decimal Total, DateTimeOffset CreatedAt);

// Application/Commands/CreateOrderHandler.cs
public class CreateOrderHandler(AppDbContext db)
{
    public async Task<Guid> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = Order.Create(cmd.CustomerId, cmd.Items);
        db.Orders.Add(order);

        // OutboxMessage saved in the same transaction — atomic with the aggregate
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(OrderCreated).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new OrderCreated(
                order.Id, order.CustomerId, order.Total, DateTimeOffset.UtcNow)),
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct); // both order + outbox message — or neither
        return order.Id;
    }
}
```

### Outbox Pattern — Entity + Processor

```csharp
// Domain/OutboxMessage.cs
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;    // AssemblyQualifiedName
    public string Payload { get; set; } = null!; // JSON
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
}

// Infrastructure/OutboxProcessor.cs
public class OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBatchAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        // SELECT FOR UPDATE SKIP LOCKED — prevents concurrent processors from double-publishing
        var messages = await db.OutboxMessages
            .FromSqlRaw("""
                SELECT * FROM outbox_messages
                WHERE processed_at IS NULL AND retry_count < 5
                ORDER BY created_at
                LIMIT 20
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        foreach (var msg in messages)
        {
            try
            {
                var type = Type.GetType(msg.Type)!;
                var payload = JsonSerializer.Deserialize(msg.Payload, type)!;
                await publisher.Publish(payload, type, ct);
                msg.ProcessedAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish outbox message {Id}", msg.Id);
                msg.RetryCount++;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
```

**Why `SELECT FOR UPDATE SKIP LOCKED`**: Allows multiple `OutboxProcessor` instances to poll the same table concurrently without picking up the same row. Each row is locked by one processor; others skip it.

### Idempotent Consumer (Inbox Pattern)

```csharp
public class OrderCreatedConsumer(AppDbContext db) : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var messageId = context.MessageId?.ToString()
            ?? throw new InvalidOperationException("MessageId is required.");

        // Inbox check — skip if already processed
        if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == messageId))
            return;

        // Side-effects + inbox record in the same transaction — exactly-once guarantee
        await using var tx = await db.Database.BeginTransactionAsync();

        db.Notifications.Add(new OrderNotification(context.Message.OrderId));
        db.ProcessedMessages.Add(new ProcessedMessage(messageId, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        await tx.CommitAsync();
    }
}
```

### ConsumerDefinition with Retry + DLQ

```csharp
public class OrderCreatedConsumerDefinition : ConsumerDefinition<OrderCreatedConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<OrderCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Exponential(
                retryLimit: 5,
                minInterval: TimeSpan.FromSeconds(1),
                maxInterval: TimeSpan.FromMinutes(5),
                intervalDelta: TimeSpan.FromSeconds(2));
            r.Ignore<BusinessException>();    // domain errors will not succeed on retry
            r.Ignore<ValidationException>(); // validation errors will not succeed on retry
        });

        endpointConfigurator.UseDeadLetterQueueDeadLetterTransport(); // DLQ after max retries
        endpointConfigurator.UseDeadLetterQueueFaultTransport();
    }
}
```

### ProcessedMessages Table

```csharp
// Domain/ProcessedMessage.cs
public class ProcessedMessage
{
    public ProcessedMessage(string messageId, DateTimeOffset processedAt)
    {
        MessageId = messageId;
        ProcessedAt = processedAt;
    }

    public string MessageId { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }
}

// EF Core configuration
modelBuilder.Entity<ProcessedMessage>(e =>
{
    e.HasKey(m => m.MessageId);
    e.HasIndex(m => m.ProcessedAt); // for periodic cleanup jobs
});
```

### MassTransit Saga (Stateful Orchestration)

```csharp
public class OrderSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;
    public string? CustomerId { get; set; }
    public bool PaymentReceived { get; set; }
}

public class OrderStateMachine : MassTransitStateMachine<OrderSagaState>
{
    public State WaitingForPayment { get; private set; } = null!;
    public Event<OrderCreated> OrderCreatedEvent { get; private set; } = null!;
    public Event<PaymentCompleted> PaymentCompletedEvent { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);
        Event(() => OrderCreatedEvent, x => x.CorrelateById(m => m.Message.OrderId));

        Initially(
            When(OrderCreatedEvent)
                .Then(ctx => ctx.Saga.CustomerId = ctx.Message.CustomerId)
                .TransitionTo(WaitingForPayment));

        During(WaitingForPayment,
            When(PaymentCompletedEvent)
                .Then(ctx => ctx.Saga.PaymentReceived = true)
                .Finalize());
    }
}
```

## Anti-patterns

### DON'T Publish Without the Outbox

```csharp
// BAD — if SaveChanges succeeds but Publish fails, data is inconsistent
await db.SaveChangesAsync(ct);
await publisher.Publish(new OrderCreated(...), ct); // can fail — dual-write problem

// GOOD — save OutboxMessage in the same transaction as the aggregate
db.OutboxMessages.Add(new OutboxMessage { /* ... */ });
await db.SaveChangesAsync(ct); // atomic: both succeed or both fail
// BackgroundService publishes asynchronously with retry
```

### DON'T Skip the Idempotency Check

```csharp
// BAD — duplicate side-effects on broker redelivery or retry
public async Task Consume(ConsumeContext<OrderCreated> context)
{
    db.Notifications.Add(new OrderNotification(context.Message.OrderId));
    await db.SaveChangesAsync(); // creates duplicate notification on every retry
}

// GOOD — check MessageId first, write record in same transaction
if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == messageId))
    return; // idempotent — safe to receive multiple times
```

### DON'T Put Logic in Message Contracts

```csharp
// BAD — behavior in a message type breaks consumer decoupling
public record OrderCreated(Guid OrderId)
{
    public decimal CalculateShipping() => /* domain logic */; // NO
}

// GOOD — messages are pure data, no behavior
public record OrderCreated(Guid OrderId, string CustomerId, decimal Total, DateTimeOffset CreatedAt);
```

### DON'T Use Fire-and-Forget Publishing

```csharp
// BAD — no delivery guarantee, exceptions are swallowed
_ = Task.Run(() => publisher.Publish(new OrderCreated(...)));

// GOOD — use the Outbox (transactional, guaranteed delivery with retry)
db.OutboxMessages.Add(new OutboxMessage { /* ... */ });
await db.SaveChangesAsync(ct);
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| New project messaging (default) | MassTransit + RabbitMQ (MIT licensed) |
| Reliable event publishing | Outbox pattern — save OutboxMessage in same DB transaction |
| Prevent duplicate processing | Inbox pattern — check MessageId in ProcessedMessages |
| Failed message handling | DLQ via `UseDeadLetterQueueDeadLetterTransport()` |
| Retry transient failures | `ConsumerDefinition` + exponential `UseMessageRetry` |
| Skip business error retries | `r.Ignore<BusinessException>()` |
| Concurrent polling without duplicates | `SELECT FOR UPDATE SKIP LOCKED` in OutboxProcessor |
| Simple 2-3 step workflow | Event choreography |
| Complex workflow with compensation | MassTransit saga (`MassTransitStateMachine<TState>`) |
| Local development broker | RabbitMQ via Docker |
| Production cloud broker | Azure Service Bus or RabbitMQ |
