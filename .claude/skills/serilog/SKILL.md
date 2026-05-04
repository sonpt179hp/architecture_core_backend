---
name: serilog
description: >
  Structured logging with Serilog for .NET 8 applications. Covers two-stage
  bootstrap, appsettings configuration, enrichers, sinks, request logging,
  destructuring, CorrelationId enrichment, and Audit vs Technical log separation.
  Load this skill when setting up Serilog, configuring log sinks, enrichers,
  or structured logging, or when the user mentions "Serilog", "structured
  logging", "log enrichment", "Seq", "LogContext", "UseSerilog",
  "WriteTo", "message template", "Serilog.Expressions", "request logging",
  "log sink", "rolling file", "audit log", or "CorrelationId".
---

# Serilog

## Core Principles

1. **Two-stage initialization** — Create a bootstrap logger for startup, then replace it with the full logger after DI is ready. This captures startup errors that would otherwise be lost.
2. **`builder.Host.UseSerilog()` for .NET 8** — Use `builder.Host.UseSerilog()` in .NET 8. `builder.Services.AddSerilog()` (which supports `ReadFrom.Services()`) requires .NET 9+. In .NET 8, wire DI-aware services (e.g. enrichers) through `ReadFrom.Configuration` only.
3. **Message templates, not interpolation** — `{PropertyName}` syntax creates structured data that can be queried. String interpolation (`$"..."`) breaks structure and allocates even when the log level is disabled.
4. **Configure via appsettings.json** — Keep log levels, sinks, and overrides in configuration so they can change per environment without redeployment.
5. **CorrelationId in every log line** — Push `TraceId` into `LogContext` at the middleware level. Requires `.Enrich.FromLogContext()`. See the **logging** skill for the middleware.
6. **Audit Log = separate sink** — Audit events (who did what to which record) go to a dedicated sink (separate Seq workspace or DB table). Technical logs (exceptions, latency) stay in the main pipeline.

## Patterns

### Two-Stage Bootstrap Setup (.NET 8)

```csharp
using Serilog;

// Stage 1: Bootstrap logger — captures startup errors before DI
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting application");

    var builder = WebApplication.CreateBuilder(args);

    // Stage 2: Full logger — .NET 8 uses builder.Host.UseSerilog()
    builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "MyApp.Api"));

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>(); // pushes TraceId to LogContext
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent",
                httpContext.Request.Headers.UserAgent.ToString());
        };
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
```

### appsettings.json Configuration

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] [{TraceId}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": { "serverUrl": "http://localhost:5341" }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithEnvironmentName"],
    "Destructure": [
      { "Name": "ToMaximumDepth", "Args": { "maximumDestructuringDepth": 4 } },
      { "Name": "ToMaximumStringLength", "Args": { "maximumStringLength": 1024 } },
      { "Name": "ToMaximumCollectionCount", "Args": { "maximumCollectionCount": 10 } }
    ]
  }
}
```

Override section uses namespace prefixes matched against `SourceContext`. More specific prefixes take precedence.

### CorrelationId Enrichment with LogContext

`LogContext.PushProperty` attaches a property to **all** log events within the `using` scope. Place this in middleware so the entire request pipeline is enriched automatically.

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

        context.Response.Headers[TraceIdHeader] = traceId;

        // Every log event in this request now carries TraceId
        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("TenantId", context.User.FindFirstValue("tenant_id") ?? "unknown"))
        {
            await next(context);
        }
    }
}
```

Requires `.Enrich.FromLogContext()` in the logger configuration.

### Audit Log Sink Separation

Audit events go to a dedicated sink. Use `Serilog.Expressions` to route by `EventType` property.

```csharp
// In logger configuration (code-based, for clarity)
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    // Main technical log — everything
    .WriteTo.Seq("http://localhost:5341")
    // Audit log — only events tagged EventType = "Audit", separate Seq instance
    .WriteTo.Conditional(
        e => e.Properties.ContainsKey("EventType") &&
             e.Properties["EventType"].ToString() == "\"Audit\"",
        wt => wt.Seq("http://audit-seq:5341")));

// Usage — emit an audit event
using (LogContext.PushProperty("EventType", "Audit"))
{
    _logger.LogInformation(
        "Document {DocumentId} status changed to {NewStatus} by {UserId}",
        document.Id, newStatus, currentUser.Id);
}

// Better — encapsulate in a service
public interface IAuditLogger
{
    void Log(string messageTemplate, params object[] args);
}

public class SerilogAuditLogger(ILogger<SerilogAuditLogger> logger) : IAuditLogger
{
    public void Log(string messageTemplate, params object[] args)
    {
        using (LogContext.PushProperty("EventType", "Audit"))
            logger.LogInformation(messageTemplate, args);
    }
}
```

### Request Logging Middleware

Replaces multiple per-request log events from ASP.NET Core with a single summary event.

```csharp
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    options.GetLevel = (httpContext, elapsed, ex) => ex is not null
        ? LogEventLevel.Error
        : httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : httpContext.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Verbose
                : LogEventLevel.Information;

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("UserId",
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous");
    };
});
```

### Structured Logging and Destructuring

```csharp
// Named properties — creates queryable structured data
logger.LogInformation("Order {OrderId} placed by {CustomerId} for {Total:C}",
    orderId, customerId, total);

// @ operator preserves object structure as properties
logger.LogInformation("Processing {@SensorInput}", sensorInput);

// $ operator forces ToString()
logger.LogInformation("Received {$Data}", new[] { 1, 2, 3 });
```

### Serilog.Expressions for Filtering

```csharp
// Exclude health check noise
.Filter.ByExcluding("RequestPath like '/health%'")

// Route errors to a separate file
.WriteTo.Conditional("@l = 'Error'",
    wt => wt.File("logs/errors-.log", rollingInterval: RollingInterval.Day))
```

## Anti-patterns

### Don't Use String Interpolation

```csharp
// BAD — breaks structured logging, allocates even when level is disabled
logger.LogInformation($"Order {orderId} created for {customerId}");

// GOOD — message template with named parameters
logger.LogInformation("Order {OrderId} created for {CustomerId}", orderId, customerId);
```

### Don't Skip CloseAndFlush

```csharp
// BAD — async sinks (Seq, OTLP) lose buffered events on shutdown
app.Run();

// GOOD — wrap in try/finally
try { app.Run(); }
catch (Exception ex) { Log.Fatal(ex, "Unhandled exception"); }
finally { await Log.CloseAndFlushAsync(); }
```

### NEVER Log Sensitive Data

```csharp
// NEVER LOG any of the following:
// - JWT tokens / Bearer tokens
// - Passwords or password hashes
// - Document content / file content
// - PII (full name + address + national ID together)
// - Credit card numbers, bank details

// BAD
logger.LogInformation("Login: {Email} with password {Password}", email, password);
logger.LogDebug("JWT token issued: {Token}", jwtToken);
logger.LogInformation("Document content: {Content}", document.Content);

// GOOD — log only identity references and outcomes
logger.LogInformation("User {UserId} authenticated", userId);
logger.LogInformation("JWT issued for {UserId}, expires {Expiry}", userId, expiry);
logger.LogInformation("Document {DocumentId} retrieved for {TenantId}", docId, tenantId);
```

### Don't Destructure Without Limits

```csharp
// BAD — large object graphs cause memory issues and massive log entries
logger.LogInformation("Request: {@Request}", httpContext.Request);

// GOOD — configure destructuring limits in appsettings.json Destructure section
// BETTER — destructure to specific properties
.Destructure.ByTransforming<HttpRequest>(r => new { r.Method, r.Path })
```

### Don't Use AddSerilog in .NET 8

```csharp
// BAD — AddSerilog() is .NET 9+; will compile but ReadFrom.Services() won't work in .NET 8
builder.Services.AddSerilog((services, lc) => lc.ReadFrom.Services(services));

// GOOD — use builder.Host.UseSerilog() in .NET 8
builder.Host.UseSerilog((context, lc) => lc.ReadFrom.Configuration(context.Configuration));
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Application logging (.NET 8) | Serilog with `builder.Host.UseSerilog()` |
| Log storage (development) | Seq (free single-user) or Aspire Dashboard |
| Log storage (production) | Seq, Elasticsearch (Elastic sink), or OTLP backend |
| Request logging | `UseSerilogRequestLogging()` |
| Scoped properties | `LogContext.PushProperty()` in middleware |
| CorrelationId propagation | CorrelationId middleware + `Enrich.FromLogContext()` |
| Log filtering | `Serilog.Expressions` for expression-based filtering |
| High-performance paths | `[LoggerMessage]` source generator |
| Audit trails | Dedicated `IAuditLogger` with separate Seq sink |
| Log levels by environment | `MinimumLevel.Override` per namespace in appsettings |
| OpenTelemetry integration | `Serilog.Sinks.OpenTelemetry` |
