---
name: dependency-injection
description: >
  Dependency injection patterns for .NET 8 Clean Architecture. Covers module-based
  extension methods, service lifetimes, startup validation, MediatR + FluentValidation
  assembly scan registration, keyed services, the decorator pattern, and common DI pitfalls.
  Load this skill when registering services, resolving lifetime issues, designing
  service composition, or when the user mentions "DI", "dependency injection",
  "service registration", "AddScoped", "AddTransient", "AddSingleton", "keyed
  services", "decorator", "Scrutor", "IServiceCollection", "AddApplication",
  "AddInfrastructure", "captive dependency", "ValidateOnBuild", or "ValidateScopes".
---

# Dependency Injection

## Core Principles

1. **Constructor injection is the default** — Inject dependencies through the constructor (primary constructors make this clean). No service locator, no property injection.
2. **Match lifetimes carefully** — A singleton must never depend on a scoped or transient service. This is the most common DI bug. DbContext = Scoped, ICurrentUser = Scoped, ITenantContext = Scoped, IMemoryCache = Singleton.
3. **Register interfaces, resolve interfaces** — Register `services.AddScoped<IOrderService, OrderService>()`, not the concrete type.
4. **Keyed services for strategy pattern** — .NET 8+ keyed services replace manual factory patterns for selecting between implementations.
5. **Module extension methods** — Each architectural layer owns its DI registration via a single extension method (`AddApplication()`, `AddInfrastructure()`, `AddApiServices()`). `Program.cs` only calls these top-level methods; it never touches individual service registrations.
6. **Startup validation** — Always set `ValidateOnBuild = true` and `ValidateScopes = true` in development. This catches missing registrations and captive dependencies at startup, not at runtime.

## Patterns

### Module Extension Methods (Clean Architecture)

```csharp
// Application/DependencyInjection.cs
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceExtensions).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TracingBehavior<,>));
        });

        // FluentValidation — scan Application assembly for all validators
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddScoped<ITenantContext, TenantContextService>();

        return services;
    }
}

// Infrastructure/DependencyInjection.cs
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("Default"),
                    npgsql => npgsql
                        .CommandTimeout(30)
                        .EnableRetryOnFailure(2))
                .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

        services.AddScoped<IAuditInterceptor, AuditInterceptor>();
        services.AddScoped<IDocumentRepository, EfDocumentRepository>();

        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration.GetConnectionString("Redis"));

        services.AddSingleton<IMemoryCache, MemoryCache>();

        // Outbox processor — scoped because it uses DbContext
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddHostedService<OutboxBackgroundService>();

        return services;
    }
}

// Api/DependencyInjection.cs
public static class ApiServiceExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddHttpContextAccessor();
        services.AddProblemDetails();
        return services;
    }
}

// Program.cs — calls only module-level methods
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices();
```

**Why**: `Program.cs` remains a 20-line composition root. Each layer owns its registrations, can be tested in isolation, and can be added to new host types (worker service, function app) by calling one method.

### Startup Validation

```csharp
// Program.cs — in development: fail fast on misconfiguration
builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateOnBuild = context.HostingEnvironment.IsDevelopment();
    options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
});
```

`ValidateOnBuild` verifies every registered service can be constructed. `ValidateScopes` detects when a singleton captures a scoped service (captive dependency). Both throw at startup rather than on the first failing request.

### MediatR + FluentValidation Assembly Scan

```csharp
// Validation pipeline behavior — registered once in AddApplication()
public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}

// FluentValidation registration — scans Application assembly automatically
services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
// No need to register individual validators; all IValidator<T> implementations are found.
```

### Keyed Services (.NET 8+)

```csharp
// Registration
builder.Services.AddKeyedScoped<INotificationService, EmailNotificationService>("email");
builder.Services.AddKeyedScoped<INotificationService, SmsNotificationService>("sms");

// Resolution via attribute in handler constructor
public class OrderHandler([FromKeyedServices("email")] INotificationService notifier)
{
    public async Task Handle(CreateOrder.Command command, CancellationToken ct)
    {
        await notifier.SendAsync(notification, ct);
    }
}
```

### Decorator Pattern (Scrutor)

```csharp
// Base service
builder.Services.AddScoped<IOrderService, OrderService>();

// Wrap with caching decorator using Scrutor
builder.Services.Decorate<IOrderService, CachedOrderService>();

public class CachedOrderService(IOrderService inner, IMemoryCache cache) : IOrderService
{
    public async Task<Order?> GetAsync(Guid id, CancellationToken ct)
    {
        return await cache.GetOrCreateAsync($"order:{id}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);
            return await inner.GetAsync(id, ct);
        });
    }
}
```

### Factory Pattern

When you need runtime logic to select an implementation:

```csharp
builder.Services.AddScoped<IPaymentProcessor>(sp =>
{
    var config = sp.GetRequiredService<IOptions<PaymentOptions>>().Value;
    return config.Provider switch
    {
        "stripe" => ActivatorUtilities.CreateInstance<StripeProcessor>(sp),
        "paypal" => ActivatorUtilities.CreateInstance<PayPalProcessor>(sp),
        _ => throw new InvalidOperationException($"Unknown payment provider: {config.Provider}")
    };
});
```

### Options Registration

```csharp
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

public class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _jwt = options.Value;
}
```

## Anti-patterns

### Don't Capture Scoped Services in Singletons

```csharp
// BAD — DbContext (Scoped) captured by singleton = stale data + memory leak
builder.Services.AddSingleton<OrderCache>(); // depends on AppDbContext

// GOOD — use IServiceScopeFactory in singleton
public class OrderCache(IServiceScopeFactory scopeFactory)
{
    public async Task<Order?> GetAsync(Guid id)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Orders.FindAsync(id);
    }
}
```

### Don't Register Everything in Program.cs

```csharp
// BAD — Program.cs knows about every individual service
builder.Services.AddScoped<IDocumentRepository, EfDocumentRepository>();
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();
// ... 50 more lines

// GOOD — Program.cs delegates to layer-owned extension methods
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices();
```

### Don't Skip Startup Validation in Development

```csharp
// BAD — captive dependency discovered only when the endpoint is hit
builder.Services.AddSingleton<ReportService>(); // depends on AppDbContext (Scoped)

// GOOD — ValidateOnBuild + ValidateScopes throws at startup
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Stateless service | Scoped (default) |
| IMemoryCache, configuration, metrics | Singleton |
| DbContext, ICurrentUser, ITenantContext | Scoped |
| Multiple implementations | Keyed services (strategy pattern) |
| Cross-cutting behavior | Decorator pattern (Scrutor) |
| MediatR pipeline cross-cutting | `IPipelineBehavior<,>` |
| Convention-based registration | Scrutor `.Scan()` |
| Runtime implementation selection | Factory delegate |
| Strongly-typed config | `AddOptions<T>().BindConfiguration()` |
| Layer-level service grouping | Extension method per layer (`AddApplication()` etc.) |
