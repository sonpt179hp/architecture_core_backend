---
name: opentelemetry
description: >
  OpenTelemetry observability for .NET 8 applications. Covers traces, metrics,
  and logs using the OpenTelemetry SDK with OTLP export. Includes custom
  ActivitySource for MediatR handlers, IMeterFactory metrics, CorrelationId
  extraction from Activity.Current, and Aspire Dashboard integration.
  Load this skill when setting up distributed tracing, custom metrics, OTLP
  export, or when the user mentions "OpenTelemetry", "OTLP", "traces", "spans",
  "Activity", "ActivitySource", "metrics", "IMeterFactory", "Meter", "Counter",
  "Histogram", "Gauge", "telemetry", "observability", "distributed tracing",
  "OTEL", "MediatR tracing", "TraceId", or "Aspire Dashboard".
---

# OpenTelemetry

## Core Principles

1. **Three pillars, one setup** — Configure traces, metrics, and logs through a single `AddOpenTelemetry()` call. Use `UseOtlpExporter()` for cross-cutting export to any OTLP-compatible backend.
2. **Custom ActivitySource for MediatR handlers** — Register a named `ActivitySource` (e.g. `"MyApp.MediatR"`) and start a span in `LoggingBehavior` or a dedicated `TracingBehavior`. This correlates every command/query with its trace. MassTransit has built-in OTel support; enable it with `.AddMassTransitInstrumentation()`.
3. **Use `IMeterFactory` for metrics** — Never create `Meter` instances with `new`. The factory manages lifetime through DI and prevents leaks.
4. **Null-safe activities** — `StartActivity()` returns `null` when no listener is attached. Always use `?.` when setting tags or events.
5. **CorrelationId from `Activity.Current`** — Read `Activity.Current?.TraceId.ToString()` to get the W3C trace ID for any log, response header, or error envelope. This links OTel traces to Serilog logs automatically.
6. **Environment variables over code** — Use `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_SERVICE_NAME` so deployments control telemetry routing without code changes.
7. **Low-cardinality metric tags** — Keep metric tag combinations under ~1000 per instrument. Use span attributes or logs for high-cardinality data like user IDs.

## Patterns

### Full Setup — .NET 8 Compatible Packages

```csharp
// Program.cs
// Packages: OpenTelemetry.Extensions.Hosting, OpenTelemetry.Instrumentation.AspNetCore,
//           OpenTelemetry.Instrumentation.Http, OpenTelemetry.Instrumentation.EntityFrameworkCore,
//           OpenTelemetry.Exporter.OpenTelemetryProtocol

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: builder.Environment.ApplicationName,
            serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("MyApp.MediatR")   // custom MediatR pipeline spans
        .AddSource("MyApp.Orders"))   // custom domain spans
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("MyApp.Orders"))
    .UseOtlpExporter(); // reads OTEL_EXPORTER_OTLP_ENDPOINT env var
```

Configure via environment variables — no code change between environments:
```
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OTEL_SERVICE_NAME=MyApp.Api
OTEL_TRACES_SAMPLER=parentbased_traceidratio
OTEL_TRACES_SAMPLER_ARG=0.1
```

### Custom ActivitySource for MediatR Handlers

Create a dedicated `TracingBehavior` in the Application pipeline to wrap every MediatR command/query in a trace span.

```csharp
// Application/Behaviours/TracingBehavior.cs
public class TracingBehavior<TRequest, TResponse>(ILogger<TracingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ActivitySource Source = new("MyApp.MediatR");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        using var activity = Source.StartActivity(requestName, ActivityKind.Internal);
        activity?.SetTag("mediator.request", requestName);

        try
        {
            var response = await next();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}

// Register in AddApplication()
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TracingBehavior<,>));
```

### CorrelationId from Activity.Current

Use `Activity.Current?.TraceId` as the correlation anchor between OTel traces and Serilog logs.

```csharp
// In CorrelationIdMiddleware — prefer the OTel TraceId when available
var traceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault()
    ?? Activity.Current?.TraceId.ToString()
    ?? Guid.NewGuid().ToString("N");

// In error responses — expose the TraceId so clients can report it
return Problem(
    title: "An error occurred",
    detail: "Contact support with TraceId: " + Activity.Current?.TraceId.ToString(),
    statusCode: 500);
```

Serilog automatically picks up `TraceId` and `SpanId` from the current activity when configured with `OpenTelemetry.Instrumentation.Serilog` or when the OTLP sink is active.

### MassTransit — Built-in OTel Instrumentation

MassTransit has native OpenTelemetry support. No custom ActivitySource needed for consumers.

```csharp
// In AddInfrastructure() or MassTransit configuration
services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.ConfigureEndpoints(ctx);
    });
});

// OTel tracing for MassTransit (package: MassTransit.Extensions.Diagnostics.OpenTelemetry)
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddSource("MassTransit")); // MassTransit publishes to this source name
```

### Custom Metrics with IMeterFactory

```csharp
public sealed class OrderMetrics
{
    private readonly Counter<int> _ordersCreated;
    private readonly Histogram<double> _orderDuration;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("MyApp.Orders");

        _ordersCreated = meter.CreateCounter<int>(
            "myapp.orders.created", "{orders}", "Number of orders created");

        _orderDuration = meter.CreateHistogram<double>(
            "myapp.orders.duration", "s", "Order processing duration",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries = [0.01, 0.05, 0.1, 0.5, 1, 5, 10]
            });
    }

    public void OrderCreated(string region) =>
        _ordersCreated.Add(1, new KeyValuePair<string, object?>("region", region));

    public void RecordDuration(double seconds) => _orderDuration.Record(seconds);
}

// Registration — singleton because IMeterFactory manages Meter lifetime
builder.Services.AddSingleton<OrderMetrics>();
```

### Custom ActivitySource for Domain Services

```csharp
public sealed class OrderService(ILogger<OrderService> logger)
{
    private static readonly ActivitySource Source = new("MyApp.Orders");

    public async Task<Order> ProcessOrderAsync(CreateOrderRequest request, CancellationToken ct)
    {
        using var activity = Source.StartActivity("ProcessOrder", ActivityKind.Internal);
        activity?.SetTag("order.customer_id", request.CustomerId);

        try
        {
            var order = await SaveOrder(request, ct);
            activity?.SetTag("order.id", order.Id.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);
            return order;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### Aspire Dashboard for Local Development

```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 \
    mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Then point your app at it:
```
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

Dashboard UI is at `http://localhost:18888`.

## Anti-patterns

### Don't Create Meters Per Request

```csharp
// BAD — new Meter per request causes memory leaks
public void HandleRequest()
{
    var meter = new Meter("MyApp");
    meter.CreateCounter<int>("requests").Add(1);
}

// GOOD — singleton via IMeterFactory
public class MyMetrics(IMeterFactory meterFactory)
{
    private readonly Counter<int> _requests =
        meterFactory.Create("MyApp").CreateCounter<int>("myapp.requests");
    public void RequestHandled() => _requests.Add(1);
}
```

### Don't Skip Null Checks on Activity

```csharp
// BAD — NullReferenceException when no listener is attached (e.g. tests)
using var activity = source.StartActivity("Work");
activity.SetTag("key", "value");

// GOOD — null-safe
activity?.SetTag("key", "value");
```

### Don't Use High-Cardinality Metric Tags

```csharp
// BAD — unbounded cardinality causes memory explosion in collectors
_counter.Add(1, new("request.id", Guid.NewGuid().ToString()));
_counter.Add(1, new("user.id", userId));

// GOOD — low-cardinality dimensions only
_counter.Add(1, new("http.method", "GET"), new("http.status_code", 200));
```

### Don't Mix UseOtlpExporter with AddOtlpExporter

```csharp
// BAD — throws NotSupportedException at runtime
builder.Services.AddOpenTelemetry()
    .UseOtlpExporter()
    .WithTracing(t => t.AddOtlpExporter());

// GOOD — use UseOtlpExporter() once for all signals
builder.Services.AddOpenTelemetry().UseOtlpExporter();
```

### Don't Forget to Register Custom Sources

```csharp
// BAD — activities silently dropped (no listener registered)
var source = new ActivitySource("MyApp.MediatR");
using var activity = source.StartActivity("Work"); // returns null!

// GOOD — register in the tracing builder
otel.WithTracing(t => t
    .AddSource("MyApp.MediatR")
    .AddSource("MyApp.Orders"));
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Full observability setup | `AddOpenTelemetry()` + all three signals + `UseOtlpExporter()` |
| MediatR handler tracing | `TracingBehavior<,>` with `ActivitySource("MyApp.MediatR")` |
| MassTransit consumer tracing | `.AddSource("MassTransit")` — built-in, no custom source needed |
| EF Core query tracing | `AddEntityFrameworkCoreInstrumentation()` |
| Custom business metrics | `IMeterFactory` + singleton metrics class |
| Custom trace spans | `ActivitySource` + `StartActivity()` |
| CorrelationId linking | `Activity.Current?.TraceId.ToString()` |
| Local development backend | Aspire Dashboard standalone container |
| Production backend | OTel Collector → Grafana / Datadog / Jaeger |
| Sampling in production | `OTEL_TRACES_SAMPLER=parentbased_traceidratio` + ratio arg |
| High-performance logging | `[LoggerMessage]` source generator |
