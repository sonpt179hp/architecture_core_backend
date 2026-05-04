# 06 – Observability Rules

**Nguồn:** `backend_core_technical_guidelines.md` §4.5

---

## DO

1. **Dùng Serilog** với sink structured (JSON) — không dùng `Console.WriteLine` hay `Debug.WriteLine` trong production code.

2. **Mỗi request HTTP phải carry `CorrelationId` / `TraceId`** xuyên suốt:
   ```csharp
   Log.ForContext("CorrelationId", Activity.Current?.TraceId.ToString())
      .Information("Handling {CommandName}", typeof(TRequest).Name);
   ```

3. **Đăng ký OpenTelemetry** với Activity Source cho:
   - HTTP incoming requests (`UseOpenTelemetry()`)
   - MediatR handlers (custom `ActivitySource`)
   - MassTransit consumers (built-in OpenTelemetry support)
   - EF Core queries (`AddEntityFrameworkInstrumentation()`)

4. **Expose hai health check endpoint riêng biệt:**
   - `/health/live` — chỉ check process còn sống (không check DB)
   - `/health/ready` — check DB, RabbitMQ, external dependencies

5. **Background workers** (Outbox processor, Inbox processor) phải phát metrics về:
   - Số message xử lý thành công / thất bại
   - Latency trung bình
   - Queue depth hiện tại

6. Log mức `Warning` khi retry xảy ra, `Error` khi Circuit Breaker open.

## DON'T

1. **KHÔNG** log token JWT, password, secret key.
   Dùng `[LogMasked]` hoặc custom Serilog destructuring policy.

2. **KHÔNG** log nội dung bí mật của văn bản (body, attachments).
   Chỉ log metadata (documentId, action, userId).

3. **KHÔNG** dùng `LogLevel.Information` cho noise log trong hot-path.
   Dùng `Debug` hoặc `Verbose`.

4. **KHÔNG** để `/health/ready` fail khi dependency không critical.
   Dùng `DegradedHealthCheck` thay vì `Unhealthy`.

5. **KHÔNG** gộp Audit Log vào Technical Log.
   Audit Log phải tách riêng storage/sink (ví dụ: bảng `audit_logs` trong DB hoặc Seq sink riêng).

## Ví dụ minh họa

```csharp
// ── Serilog structured log trong Handler
_logger.LogInformation(
    "Document {DocumentId} published by {UserId} at {IssuedAt}",
    doc.Id, _currentUser.UserId, doc.IssuedAt);

// ── CorrelationId middleware
app.Use(async (ctx, next) =>
{
    var traceId = ctx.Request.Headers["X-Trace-Id"].FirstOrDefault()
        ?? Activity.Current?.TraceId.ToString()
        ?? Guid.NewGuid().ToString();

    ctx.Items["TraceId"] = traceId;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", traceId))
    {
        await next();
    }
});

// ── Health checks registration
builder.Services.AddHealthChecks()
    .AddNpgSql(connStr, name: "postgres", tags: ["ready"])
    .AddRabbitMQ(rabbitConnFactory, name: "rabbitmq", tags: ["ready"])
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

app.MapHealthChecks("/health/live",
    new() { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks("/health/ready",
    new() { Predicate = r => r.Tags.Contains("ready") });
```
