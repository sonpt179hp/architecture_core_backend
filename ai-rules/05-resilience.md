# 05 – Resilience Pattern Rules

**Nguồn:** `backend_core_technical_guidelines.md` §4.4

---

## DO

1. **Phân biệt lỗi transient vs lỗi business:**
   - **Retry CHỈ** áp dụng cho lỗi transient (network timeout, DB connection refused, HTTP 503).
   - **Lỗi business** (validation fail, 404 Not Found, domain rule violation) — **KHÔNG retry**.

2. **Đặt timeout explicit** cho mọi external call:
   ```csharp
   builder.AddHttpClient("OrgService")
          .AddStandardResilienceHandler(opts =>
          {
              opts.Retry.MaxRetryAttempts = 3;
              opts.Retry.Delay = TimeSpan.FromMilliseconds(300);
              opts.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
              opts.Timeout.Timeout = TimeSpan.FromSeconds(10);
          });
   ```

3. **Cài đặt Idempotent Consumer** cho mọi MassTransit consumer:
   - Lưu `MessageId` đã xử lý vào bảng `ProcessedMessages` trước khi thực hiện side-effects.
   - Nếu `MessageId` đã tồn tại → skip ngay, không làm gì.

4. **Thiết kế Dead Letter Queue (DLQ)** cho mọi consumer.
   Configure: `UseMessageRetry(r => r.Exponential(...))` + `x-dead-letter-exchange`.

5. **Đặt cơ chế replay DLQ có kiểm soát** — không replay tự động vô điều kiện.

6. Retry dùng **exponential backoff với jitter** (Polly built-in).

## DON'T

1. **KHÔNG** để retry loop vô hạn hoặc retry ngay lập tức không delay.

2. **KHÔNG** retry sau lỗi `DbUpdateConcurrencyException` mà không reload entity trước:
   ```csharp
   // ❌ WRONG
   await _db.SaveChangesAsync(); // concurrency exception → retry ngay → throw lại

   // ✅ CORRECT
   await _db.SaveChangesAsync();
   catch (DbUpdateConcurrencyException)
   {
       await entry.ReloadAsync(ct); // reload rồi apply lại hoặc trả 409
   }
   ```

3. **KHÔNG** để Circuit Breaker open-state block toàn bộ request.
   Design fallback: trả cached data hoặc trả lỗi rõ ràng.

4. **KHÔNG** dùng chung một Polly Policy instance có state (Circuit Breaker) cho nhiều service khác nhau.

5. **KHÔNG** bỏ qua `MessageId` duplicate — mỗi consumer phải idempotent.

## Ví dụ minh họa

```csharp
// ── Idempotent Consumer với MassTransit
public class DocumentPublishedConsumer : IConsumer<DocumentPublishedEvent>
{
    public async Task Consume(ConsumeContext<DocumentPublishedEvent> context)
    {
        var messageId = context.MessageId
            ?? throw new InvalidOperationException("MessageId required");

        if (await _processedMessages.ExistsAsync(messageId))
        {
            _logger.LogDebug("Message {MessageId} already processed, skipping", messageId);
            return; // Already processed — idempotent skip
        }

        await using var tx = await _db.BeginTransactionAsync(ct);
        await _processedMessages.MarkAsync(messageId, context.Metadata.ConsumerType.Name);

        // ... actual side-effects ...

        await tx.CommitAsync(ct);
        _logger.LogInformation("Message {MessageId} processed successfully", messageId);
    }
}

// ── Consumer Definition với retry và DLQ
public class DocumentPublishedConsumerDefinition
    : ConsumerDefinition<DocumentPublishedConsumer>
{
    public DocumentPublishedConsumerDefinition()
    {
        EndpointName = "document-service.document-published";

        ConfigureConsumerConfigurator(cfg =>
        {
            cfg.UseMessageRetry(r => r.Exponential(
                retryLimit: 3,
                minInterval: TimeSpan.FromMilliseconds(100),
                maxInterval: TimeSpan.FromSeconds(1),
                intervalDelta: TimeSpan.FromMilliseconds(100)));

            cfg.UseDeadLetterQueueDeadLetterTransport();
            cfg.Ignore<BusinessException>(); // Business error → DLQ ngay, không retry
        });
    }
}
```
