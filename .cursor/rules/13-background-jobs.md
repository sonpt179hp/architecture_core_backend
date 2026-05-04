# 13 – Background Jobs & Outbox/Inbox Pattern Rules

**Nguồn:** `design_pattern_architecture.md` §2.3 · `backend_core_technical_guidelines.md` §4.4

---

## Outbox Pattern (Write-Side — Đảm bảo Event Không Mất)

```
HTTP Request
  → Command Handler
    → Lưu Aggregate vào DB
    → Lưu OutboxMessage vào DB (cùng transaction)
  ← Response trả về ngay

Background OutboxProcessor
  → Poll DB lấy message chưa publish
  → Publish lên Message Broker
  → Đánh dấu message đã xử lý
```

## Inbox Pattern (Read-Side — Đảm bảo Consumer Idempotent)

```
Consumer nhận message
  → Kiểm tra MessageId trong ProcessedMessages
  → Đã tồn tại? → Skip
  → Chưa? → Mở transaction
           → Ghi MessageId vào ProcessedMessages
           → Thực hiện side-effects
           → Commit
```

---

## DO

1. **Outbox: Lưu OutboxMessage cùng transaction với Aggregate** — đảm bảo atomicity:
   ```csharp
   // Trong CommandHandler
   var doc = Document.Create(...);
   _db.Documents.Add(doc);
   _db.OutboxMessages.Add(new OutboxMessage(nameof(DocumentCreatedEvent), doc.Id));
   await _db.SaveChangesAsync(ct); // 1 transaction duy nhất
   ```

2. **OutboxProcessor chạy như `BackgroundService`** — poll và publish định kỳ:
   - Lấy batch message (thường 100 records) theo `SKIP LOCKED` để tránh concurrent processing
   - Publish lên broker
   - Đánh dấu `ProcessedAt = UtcNow`
   - Lỗi publish → retry theo exponential backoff, sau N lần → đánh dấu Error

3. **Inbox: Lưu MessageId vào `ProcessedMessages` trong cùng transaction với side-effects** (xem `ai-rules/05-resilience.md`).

4. **Jobs phải idempotent** — chạy lại nhiều lần vẫn cho kết quả đúng.

5. **Dùng `SELECT ... FOR UPDATE SKIP LOCKED`** khi poll Outbox để tránh nhiều instance cùng process 1 message:
   ```csharp
   var messages = await _db.OutboxMessages
       .FromSql(@"SELECT * FROM outbox_messages
                  WHERE processed_at IS NULL
                  LIMIT 100
                  FOR UPDATE SKIP LOCKED")
       .ToListAsync(ct);
   ```

6. **Hangfire / Quartz** cho scheduled jobs có lịch cố định (report hàng ngày, cleanup hàng tuần):
   - Hangfire: dễ setup, có dashboard built-in
   - Quartz: mạnh hơn, hỗ trợ cluster, CRON expression đầy đủ

7. **Background job phải phát metrics** — số job xử lý, số lỗi, latency (xem `ai-rules/06-observability.md`).

## DON'T

1. **KHÔNG** publish event trực tiếp trong transaction:
   ```csharp
   // ❌ WRONG — broker down → event mất, nhưng DB đã commit
   await _db.SaveChangesAsync(ct);
   await _bus.Publish(new DocumentCreatedEvent(doc.Id), ct);
   // ✅ CORRECT — dùng Outbox
   ```

2. **KHÔNG** poll Outbox quá dày (< 1 giây) — gây DB load không cần thiết.
   Interval gợi ý: 5-10 giây cho hầu hết use case.

3. **KHÔNG** để job chạy quá lâu (> 5 phút) trong 1 execution — tách thành nhiều job nhỏ.

4. **KHÔNG** retry vô hạn — đặt max retry (thường 5-10 lần), sau đó đưa vào Dead Letter.

5. **KHÔNG** chạy long-running task trong `IHostedService.StartAsync` — dùng `BackgroundService.ExecuteAsync`.

6. **KHÔNG** dùng `Task.Factory.StartNew` để chạy background work — dùng `IBackgroundJobClient` (Hangfire) hoặc `IScheduler` (Quartz).

## Outbox Table Schema

```sql
CREATE TABLE outbox_messages (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    type            VARCHAR(300) NOT NULL,
    payload         JSONB        NOT NULL,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    processed_at    TIMESTAMPTZ,
    error           TEXT,
    retry_count     INT          NOT NULL DEFAULT 0
);

CREATE INDEX idx_outbox_unprocessed ON outbox_messages (created_at)
    WHERE processed_at IS NULL;
```

## Ví dụ minh họa

```csharp
// ── Domain/Outbox/OutboxMessage.cs
public class OutboxMessage
{
    public Guid   Id          { get; private set; } = Guid.NewGuid();
    public string Type        { get; private set; }
    public string Payload     { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? Error      { get; set; }
    public int RetryCount     { get; set; }

    public OutboxMessage(string type, object payload)
    {
        Type    = type;
        Payload = JsonSerializer.Serialize(payload);
    }
}

// ── Infrastructure/BackgroundJobs/OutboxProcessor.cs
public class OutboxProcessor : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 100;
    private const int MaxRetry  = 5;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "OutboxProcessor batch failed");
            }

            await Task.Delay(Interval, ct);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        var messages = await db.OutboxMessages
            .FromSql($"""
                SELECT * FROM outbox_messages
                WHERE processed_at IS NULL AND retry_count < {MaxRetry}
                ORDER BY created_at
                LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        foreach (var msg in messages)
        {
            try
            {
                var eventType = Type.GetType(msg.Type)
                    ?? throw new InvalidOperationException($"Unknown event type: {msg.Type}");
                var payload   = JsonSerializer.Deserialize(msg.Payload, eventType)!;

                await bus.Publish(payload, eventType, ct);

                msg.ProcessedAt = DateTime.UtcNow;
                _logger.LogInformation("Outbox message {MessageId} published", msg.Id);
            }
            catch (Exception ex)
            {
                msg.RetryCount++;
                msg.Error = ex.Message;
                _logger.LogWarning(ex, "Failed to publish outbox message {MessageId}, retry {Count}",
                    msg.Id, msg.RetryCount);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}

// ── Hangfire scheduled job example
[DisableConcurrentExecution(10 * 60)] // 10 phút timeout
public class DailyReportJob
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900])]
    public async Task ExecuteAsync(IJobCancellationToken ct)
    {
        // idempotent logic...
    }
}
```
