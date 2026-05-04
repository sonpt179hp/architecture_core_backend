---
name: add-event-handler
description: >
  Scaffold a MassTransit consumer for an integration event with Idempotent Consumer pattern,
  ProcessedMessages table check, dead-letter queue configuration, and OpenTelemetry instrumentation.
  Use when the user asks to subscribe to or handle a domain/integration event from another service.
allowed-tools:
  - Read(**/*.cs)
  - Read(**/ai-rules/*.md)
  - Glob(src/**/*.cs)
  - Glob(src/**/*.csproj)
  - Edit(**/*.cs)
---

# Skill: Add Event Handler (MassTransit Consumer)

## Purpose

Scaffold đầy đủ MassTransit consumer cho một integration event với Idempotent Consumer pattern.
Đảm bảo không xử lý trùng message, có dead-letter queue fallback, và có đầy đủ tracing/logging.
Tuân thủ `ai-rules/05-resilience.md` và `ai-rules/06-observability.md`.

## Instructions

**Input cần từ user:** Tên event (ví dụ: `DocumentPublishedEvent`), service nguồn phát event, hành động cần thực hiện khi nhận event (ví dụ: "cập nhật cache cây tổ chức", "gửi notification").

1. **Xác định hoặc tạo Event contract:**
   - Nếu event từ service khác: đặt trong shared `Contracts` project tại `src/Contracts/Events/{EventName}.cs`
   - Nếu event nội bộ: đặt trong `src/{BoundedContext}/Domain/Events/{EventName}.cs`
   - Dùng `record` với `Guid EventId`, `DateTime OccurredAt`, và các payload fields
   - Implement `IDomainEvent` hoặc `IIntegrationEvent`

2. **Tạo Consumer class** tại `src/{BoundedContext}/Infrastructure/Messaging/Consumers/{EventName}Consumer.cs`:
   ```csharp
   public class {EventName}Consumer : IConsumer<{EventName}>
   ```
   - Inject: `IDbConnection` hoặc `IUnitOfWork`, `IProcessedMessageStore`, `ILogger<{EventName}Consumer>`
   - **Pattern bắt buộc** trong `Consume()`:
     ```
     a. Lấy MessageId từ context.MessageId
     b. Kiểm tra await _processed.ExistsAsync(messageId) — nếu đã xử lý thì return sớm
     c. Mở transaction
     d. await _processed.MarkAsync(messageId, consumerName) — đánh dấu đã xử lý
     e. Thực hiện side-effects
     f. Commit transaction
     ```

3. **Tạo Consumer Definition** tại `{EventName}ConsumerDefinition.cs`:
   ```csharp
   public class {EventName}ConsumerDefinition : ConsumerDefinition<{EventName}Consumer>
   ```
   - Đặt `EndpointName` theo convention: `{service-name}.{event-name}` (kebab-case)
   - Configure retry policy: `UseMessageRetry(r => r.Exponential(3, 100ms, 1s, 100ms))`
   - Configure dead-letter: `r.Ignore<BusinessException>()` — lỗi business không retry, đi thẳng DLQ

4. **Đăng ký Consumer** trong `Infrastructure/DependencyInjection.cs` hoặc `MassTransitConfig.cs`:
   ```csharp
   cfg.AddConsumer<{EventName}Consumer, {EventName}ConsumerDefinition>();
   ```

5. **Kiểm tra hoặc tạo bảng `processed_messages`** nếu chưa tồn tại:
   - Columns: `message_id UUID`, `consumer_name VARCHAR(200)`, `consumed_at TIMESTAMPTZ`
   - Primary key: `(message_id, consumer_name)`
   - Index: unique constraint trên `(message_id, consumer_name)`
   - Nhắc user chạy migration trước khi deploy

6. **Thêm logging và tracing:**
   - Log `Information` khi bắt đầu xử lý: `"Processing {EventName} with MessageId {MessageId}"`
   - Log `Debug` khi skip duplicate: `"Message {MessageId} already processed, skipping"`
   - Log `Information` khi thành công: `"Message {MessageId} processed successfully"`
   - Log `Error` khi thất bại: `"Failed to process {EventName} with MessageId {MessageId}"`
   - Đảm bảo `Activity.Current` được propagate từ MassTransit (built-in OpenTelemetry)

7. **Kiểm tra lại trước khi hoàn thành:**
   - `MessageId` null-check có xử lý (throw hoặc log warning và skip)
   - Transaction bao gồm cả việc mark processed và side-effects
   - Lỗi business (`BusinessException`, `ValidationException`) được `Ignore` trong retry — sẽ đi thẳng vào DLQ
   - Consumer có `ILogger` và log đủ: start, skip (duplicate), success, error

## Edge Cases

- Nếu consumer cần gọi HTTP service ngoài: wrap trong Polly retry (xem `ai-rules/05-resilience.md`), **KHÔNG** để timeout mặc định.
- Nếu side-effect là gửi email/notification: gợi ý dùng Outbox pattern thay vì gọi trực tiếp trong consumer để tránh mất message.
- Nếu event volume cao (>1000/s): gợi ý batch consumer (`IConsumer<Batch<T>>`).
- Nếu consumer cần xử lý nhiều loại event tương tự: vẫn tạo riêng consumer cho từng event, không dùng polymorphism.
- Nếu chưa có `IProcessedMessageStore`: tạo interface + implementation trước khi scaffold consumer.

## References

- `ai-rules/02-cqrs-pattern.md`
- `ai-rules/05-resilience.md`
- `ai-rules/06-observability.md`
- `ai-rules/08-efcore.md`
