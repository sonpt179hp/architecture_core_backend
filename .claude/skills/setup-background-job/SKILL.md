---
name: setup-background-job
description: >
  Scaffold background job infrastructure: Outbox table + processor BackgroundService,
  Inbox/ProcessedMessages table, and optional Hangfire/Quartz setup for scheduled tasks.
  Use when the bounded context needs reliable async event publishing or scheduled processing.
allowed-tools:
  - Read(**/*.cs)
  - Read(**/ai-rules/*.md)
  - Glob(src/**/*.cs)
  - Glob(src/**/*.csproj)
  - Edit(**/*.cs)
---

# Skill: Setup Background Job Infrastructure

## Purpose

Dựng Outbox Pattern để đảm bảo event không bị mất khi broker down,
Inbox/ProcessedMessages để consumer idempotent,
và Hangfire (nếu cần) cho scheduled jobs.

## Instructions

**Input:** Tên bounded context, loại jobs cần (Outbox, Inbox, Scheduled).

1. **Đọc cấu trúc project** để xác định:
   - DbContext đã tồn tại chưa
   - Migration project nào sẽ chứa Outbox migration
   - IBus (MassTransit) đã đăng ký chưa

2. **Tạo `OutboxMessage` entity** tại `Infrastructure/Outbox/OutboxMessage.cs`:
   ```csharp
   public class OutboxMessage
   {
       public Guid Id          { get; private set; } = Guid.NewGuid();
       public string Type      { get; private set; } = default!;
       public string Payload   { get; private set; } = default!;
       public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
       public DateTime? ProcessedAt { get; set; }
       public string? Error    { get; set; }
       public int RetryCount   { get; set; }

       private OutboxMessage() { }

       public static OutboxMessage Create(object domainEvent)
       {
           var type = domainEvent.GetType().AssemblyQualifiedName!;
           return new OutboxMessage
           {
               Type    = type,
               Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
           };
       }
   }
   ```

3. **Thêm `DbSet` vào DbContext** và tạo EF configuration:
   ```csharp
   // DbContext
   public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

   // OutboxMessageConfiguration.cs
   builder.ToTable("outbox_messages");
   builder.HasKey(x => x.Id);
   builder.HasIndex(x => x.ProcessedAt)
       .HasFilter("processed_at IS NULL")
       .HasDatabaseName("idx_outbox_unprocessed");
   ```

4. **Tạo `OutboxProcessor` BackgroundService** tại `Infrastructure/BackgroundJobs/OutboxProcessor.cs`:
   - Dùng `IServiceScopeFactory` để tạo scope mới mỗi iteration (tránh DbContext stale)
   - Poll batch (`LIMIT 100 FOR UPDATE SKIP LOCKED`)
   - Publish sự kiện lên MassTransit `IBus`
   - Đánh dấu `ProcessedAt`, ghi `Error` và tăng `RetryCount` nếu lỗi
   - Tham chiếu template trong `ai-rules/13-background-jobs.md`

5. **Nếu cần Inbox Pattern** — tạo `ProcessedMessages` entity:
   ```csharp
   public class ProcessedMessage
   {
       public Guid Id           { get; private set; }
       public string ConsumerName { get; private set; } = default!;
       public DateTime ConsumedAt { get; private set; } = DateTime.UtcNow;
   }
   ```
   Primary key composite `(Id, ConsumerName)`.

6. **Nếu user cần Scheduled Jobs với Hangfire:**
   ```csharp
   // Package: dotnet add package Hangfire.AspNetCore Hangfire.PostgreSql
   services.AddHangfire(cfg =>
       cfg.UsePostgreSqlStorage(config.GetConnectionString("Postgres")));
   services.AddHangfireServer();
   // Dashboard (chỉ dev/staging)
   app.MapHangfireDashboard("/jobs");
   ```

7. **Đăng ký services trong DI:**
   ```csharp
   services.AddHostedService<OutboxProcessor>();
   // Nếu có Inbox:
   services.AddScoped<IProcessedMessageStore, DbProcessedMessageStore>();
   ```

8. **Kiểm tra lại:**
   - `OutboxMessage` được lưu cùng transaction với domain event (test bằng code review)
   - `OutboxProcessor` dùng `IServiceScopeFactory`, không inject DbContext trực tiếp
   - Retry có giới hạn (MaxRetry = 5), sau đó đánh dấu Error chứ không retry mãi
   - `SKIP LOCKED` trong SQL để tránh nhiều instance xử lý cùng message

## Edge Cases

- Nếu dùng MassTransit Outbox built-in: không cần tạo thủ công — dùng `cfg.UseMessageData()` và `cfg.AddEntityFrameworkOutbox()` thay vào.
- Nếu event payload thay đổi schema: dùng `AssemblyQualifiedName` để deserialize hoặc version event.
- Nếu broker là Kafka (không phải RabbitMQ): thay `IBus.Publish` bằng `ITopicProducer<TMessage>`.
- Nếu job volume cao (>1000 msg/phút): cân nhắc tăng batch size, giảm polling interval, hoặc dùng push-based trigger thay poll.

## References

- `ai-rules/13-background-jobs.md` — Outbox/Inbox pattern, polling strategy, SKIP LOCKED
- `ai-rules/05-resilience.md` — Idempotent Consumer, retry limits, dead-letter
- `ai-rules/06-observability.md` — Background worker phải phát metrics/traces
- `ai-rules/08-efcore.md` — Transaction boundary, không auto-migrate
- `ai-rules/10-dependency-injection.md` — Đăng ký HostedService, IServiceScopeFactory
