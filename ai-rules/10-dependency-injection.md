# 10 – Dependency Injection & Service Registration Rules

**Nguồn:** `backend_core_technical_guidelines.md` §4.1 · `design_pattern_architecture.md` §2.1

---

## Mediator Library

Dự án dùng **Mediator (Arch.Ext)** với các interface `ICommand`, `IQuery`, `ICommandHandler`, `IQueryHandler`.
**KHÔNG dùng MediatR (Jimmy Bogard).**

---

## DO

1. **Tổ chức registration theo module/bounded context** bằng extension methods:
   ```csharp
   // Program.cs — chỉ gọi module-level extensions
   builder.Services
       .AddDocumentModule(builder.Configuration)
       .AddOrgModule(builder.Configuration)
       .AddInfrastructure(builder.Configuration)
       .AddApiServices();
   ```

2. **Mỗi layer tự đăng ký qua `IServiceCollection` extension** đặt trong layer đó:
   - `Application/DependencyInjection.cs` → `AddApplication()`
   - `Infrastructure/DependencyInjection.cs` → `AddInfrastructure()`
   - `Api/DependencyInjection.cs` → `AddApiServices()`

3. **Chọn service lifetime đúng:**
   | Lifetime | Khi nào dùng | Ví dụ |
   |---|---|---|
   | `Singleton` | Stateless, thread-safe, khởi tạo tốn kém | `IMemoryCache`, config options |
   | `Scoped` | Gắn với HTTP request, có trạng thái per-request | `DbContext`, `ICurrentUser`, `ITenantContext` |
   | `Transient` | Lightweight, stateless, không share | Simple calculators, validators |

4. **Bật validation container ở startup** để phát hiện lỗi ngay:
   ```csharp
   builder.Host.UseDefaultServiceProvider((ctx, opts) =>
   {
       opts.ValidateScopes  = ctx.HostingEnvironment.IsDevelopment();
       opts.ValidateOnBuild = ctx.HostingEnvironment.IsDevelopment();
   });
   ```

5. **Register Mediator và FluentValidation theo assembly scan:**
   ```csharp
   services.AddMediator(options =>
   {
       options.Namespace = "{Namespace}.Application";
       options.ServiceLifetime = ServiceLifetime.Scoped;
   });
   services.AddValidatorsFromAssembly(typeof(CreateDocumentCommand).Assembly);
   services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
   services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
   ```

6. **Dùng `Keyed Services`** (.NET 8) khi cần nhiều implementation của cùng interface:
   ```csharp
   services.AddKeyedScoped<IStorageService, LocalStorage>("local");
   services.AddKeyedScoped<IStorageService, S3Storage>("s3");
   // Inject:
   public MyService([FromKeyedServices("s3")] IStorageService storage) { }
   ```

## DON'T

1. **KHÔNG** inject `IServiceProvider` vào constructor — đây là Service Locator anti-pattern:
   ```csharp
   // ❌ WRONG
   public class MyService(IServiceProvider sp)
   {
       var repo = sp.GetRequiredService<IDocumentRepository>();
   }
   // ✅ CORRECT
   public class MyService(IDocumentRepository repo) { }
   ```

2. **KHÔNG** đăng ký `DbContext` là Singleton — gây lỗi nghiêm trọng trong concurrent environment:
   ```csharp
   // ❌ WRONG
   services.AddSingleton<AppDbContext>();
   // ✅ CORRECT
   services.AddDbContext<AppDbContext>(options => ..., ServiceLifetime.Scoped);
   ```

3. **KHÔNG** đăng ký concrete class trực tiếp mà không có interface — khóa chặt dependency, khó test:
   ```csharp
   // ❌ WRONG
   services.AddScoped<DocumentService>();
   // ✅ CORRECT
   services.AddScoped<IDocumentService, DocumentService>();
   ```

4. **KHÔNG** để `Program.cs` phình to với hàng trăm dòng registration — tách hết vào module extensions.

5. **KHÔNG** inject Scoped service vào Singleton — gây captive dependency bug:
   ```csharp
   // ❌ WRONG — DbContext (Scoped) inject vào Singleton → lỗi
   public class CacheSingleton(AppDbContext db) { ... }
   // ✅ CORRECT — dùng IServiceScopeFactory nếu cần tạo scope thủ công
   public class CacheSingleton(IServiceScopeFactory factory) { ... }
   ```

6. **KHÔNG** dùng `new` trực tiếp để tạo service trong application code.

## Ví dụ minh họa

```csharp
// ── Program.cs (gọn)
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication()
    .AddApiServices();

var app = builder.Build();
app.Run();

// ── Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.Namespace = "{Namespace}.Application";
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        services.AddValidatorsFromAssembly(typeof(CreateDocumentCommand).Assembly);

        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}

// ── Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // Database
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("Postgres"),
                npgsql => npgsql.CommandTimeout(30)));

        // Outbox processor
        services.AddHostedService<OutboxProcessor>();

        // Redis cache
        services.AddStackExchangeRedisCache(opts =>
            opts.Configuration = config.GetConnectionString("Redis"));

        return services;
    }
}
```
