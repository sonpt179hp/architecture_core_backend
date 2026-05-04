---
name: logging
description: >
  Observability for .NET 8 applications. Covers Serilog structured logging,
  OpenTelemetry traces and metrics, health checks, and correlation IDs.
  Load this skill when setting up logging, tracing, metrics, or health monitoring,
  or when the user mentions "Serilog", "logging", "structured log", "OpenTelemetry",
  "traces", "metrics", "health check", "correlation ID", "observability",
  "telemetry", "log enrichment", "X-Trace-Id", "audit log", or "ILogger".
---

# Logging & Observability

## Core Principles

1. **Structured logging with Serilog** — Every log entry is a structured event with named properties, not a formatted string. This enables searching, filtering, and alerting.
2. **OpenTelemetry for distributed tracing** — Traces connect requests across services. Metrics track system health over time.
3. **Health checks for operational readiness** — Every service exposes `/health/live` (process only) and `/health/ready` (DB + Redis + RabbitMQ) for load balancers and orchestrators.
4. **CorrelationId middleware** — Every request gets a unique trace ID (read from `X-Trace-Id` header or `Activity.Current?.TraceId`) that is pushed into Serilog `LogContext` so every log line in the request carries it automatically.
5. **Audit Log vs Technical Log** — These are separate concerns. **Audit**: who did what to which record (business compliance, separate DB table or dedicated Seq sink). **Technical**: exceptions, latency, infrastructure errors. Never mix them.

## Patterns

### CorrelationId Middleware

Reads `X-Trace-Id` header or falls back to the current OpenTelemetry `TraceId`, then pushes it into the Serilog `LogContext` for the entire request lifetime.

```csharp
// Infrastructure/Middleware/CorrelationIdMiddleware.cs
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string TraceIdHeader = "X-Trace-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.Request.Headers[TraceIdHeader].FirstOrDefault()
            ?? Activity.Current?.TraceId.ToString()
            ?? Guid.NewGuid().ToString("N");

        context.Items["TraceId"] = traceId;
        context.Response.Headers[TraceIdHeader] = traceId;

        using (LogContext.PushProperty("TraceId", traceId))
        {
            await next(context);
        }
    }
}

// Program.cs — register before UseSerilogRequestLogging
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
```

**Why**: Every log line in the request now has `TraceId` without any handler needing to know about it. Downstream calls can propagate the header.

### Serilog Setup (Two Health Endpoints)

```csharp
// Program.cs
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProperty("Application", "MyApp.Api")
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] [{TraceId}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.Seq(context.Configuration["Seq:Url"] ?? "http://localhost:5341");
});

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("UserId",
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous");
        diagnosticContext.Set("TraceId", httpContext.Items["TraceId"]);
    };
    // Don't log health check noise
    options.GetLevel = (ctx, _, ex) =>
        ex is not null || ctx.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : ctx.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Verbose
                : LogEventLevel.Information;
});
```

### Health Checks — Live vs Ready

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!,
        name: "database", tags: ["ready"])
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!,
        name: "redis", tags: ["ready"])
    .AddRabbitMQ(builder.Configuration.GetConnectionString("RabbitMq")!,
        name: "rabbitmq", tags: ["ready"]);

// /health/live — only proves the process is alive; no dependency checks
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// /health/ready — proves the app can serve traffic (DB + Redis + RabbitMQ up)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

**Why two endpoints**: Kubernetes uses liveness to restart crashed pods and readiness to stop routing traffic during cold-start or degraded dependencies. Conflating them causes unnecessary restarts or routes traffic to a pod that cannot connect to the DB.

### Audit Log Separation

```csharp
// appsettings.json — separate Seq sink for audit events only
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Seq",
        "Args": { "serverUrl": "http://localhost:5341" }
      },
      {
        "Name": "Conditional",
        "Args": {
          "expression": "EventType = 'Audit'",
          "configureSink": {
            "Name": "Seq",
            "Args": { "serverUrl": "http://audit-seq:5341" }
          }
        }
      }
    ]
  }
}

// Usage — add EventType = Audit to distinguish
logger.LogInformation("Document {DocumentId} approved by {UserId} {@Metadata}",
    doc.Id, currentUser.Id, new { Action = "Approve", TenantId = tenantId });
// Better: use a dedicated IAuditLogger service that always sets EventType = "Audit"
```

### Structured Logging (Correct Usage)

```csharp
// GOOD — structured logging with message template
logger.LogInformation("Processing order {OrderId} for customer {CustomerId}",
    orderId, customerId);

// GOOD — include relevant context
logger.LogWarning("Payment failed for order {OrderId}. Attempt {Attempt} of {MaxAttempts}",
    orderId, attempt, maxAttempts);

// GOOD — log exceptions with structured data
logger.LogError(exception, "Failed to process order {OrderId}", orderId);
```

### OpenTelemetry Integration

> For full OpenTelemetry setup (metrics, tracing, OTLP export), see the **opentelemetry** skill.
> For Serilog sink details and two-stage bootstrap, see the **serilog** skill.

## Anti-patterns

### Don't Use String Interpolation in Log Messages

```csharp
// BAD — allocates string even if level is disabled, breaks structured logging
logger.LogInformation($"Order {orderId} created for {customerId}");

// GOOD — message template with named parameters
logger.LogInformation("Order {OrderId} created for {CustomerId}", orderId, customerId);
```

### Don't Log Sensitive Data

```csharp
// BAD — logging credentials or PII
logger.LogInformation("User logged in: {Email} with password {Password}", email, password);

// NEVER LOG: JWT tokens, passwords, document content, PII, credit card numbers
// GOOD — log only identity references
logger.LogInformation("User {UserId} authenticated", userId);
```

### Don't Mix Audit and Technical Logs

```csharp
// BAD — audit event buried in technical log stream with no differentiation
logger.LogInformation("Document saved");

// GOOD — audit events carry who/what/when explicitly and go to audit sink
_auditLogger.LogAudit("Document {DocumentId} updated by {UserId} in tenant {TenantId}",
    documentId, currentUser.Id, tenantId);
```

### Don't Skip Health Check Tags

```csharp
// BAD — all checks run for liveness AND readiness (DB failure restarts pod)
app.MapHealthChecks("/health");

// GOOD — separate liveness (am I running?) from readiness (can I serve traffic?)
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Application logging | Serilog with structured logging |
| Distributed tracing | OpenTelemetry with OTLP exporter |
| Custom business metrics | `IMeterFactory` + counters/histograms |
| Request trace propagation | CorrelationId middleware (X-Trace-Id header) |
| Container liveness probe | `/health/live` (no dependency checks) |
| Container readiness probe | `/health/ready` (DB + Redis + RabbitMQ tags) |
| Log storage | Seq (development), Elastic/Grafana (production) |
| Log levels | Debug in dev, Information in staging, Warning in production |
| Compliance / who-did-what | Audit log (separate sink, dedicated IAuditLogger) |
| Technical errors / latency | Technical log (main Serilog pipeline) |
