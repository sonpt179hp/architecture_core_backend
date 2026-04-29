---
name: setup-dependency-injection
description: >
  Scaffold the DI registration structure: module-level IServiceCollection extension methods,
  Program.cs skeleton, service lifetime assignments, MediatR + FluentValidation pipeline setup,
  and container validation. Use when starting a new bounded context or restructuring Program.cs.
allowed-tools:
  - Read(**/*.cs)
  - Read(appsettings*.json)
  - Read(**/ai-rules/*.md)
  - Glob(src/**/*.cs)
  - Glob(src/**/*.csproj)
  - Edit(**/*.cs)
---

# Skill: Setup Dependency Injection Structure

## Purpose

Tổ chức DI registration gọn, theo module, fail-fast ở startup nếu config sai.
Không để `Program.cs` phình to. Mỗi layer tự đăng ký qua extension method riêng.

## Instructions

**Input:** Tên bounded context (ví dụ: `Documents`) và danh sách services sẽ đăng ký.

1. **Đọc cấu trúc project hiện tại** để xác định:
   - `Program.cs` đang có gì
   - Các project/assembly trong solution
   - Các service đã được đăng ký hay chưa

2. **Tạo `Application/DependencyInjection.cs`:**
   ```csharp
   public static class ApplicationDependencyInjection
   {
       public static IServiceCollection AddApplication(this IServiceCollection services)
       {
           services.AddMediatR(cfg =>
           {
               cfg.RegisterServicesFromAssembly(typeof(Placeholder).Assembly);
               cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
               cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
           });
           services.AddValidatorsFromAssembly(
               typeof(Placeholder).Assembly, includeInternalTypes: true);
           services.AddScoped<ICurrentUser, CurrentUser>();
           services.AddScoped<ITenantContext, TenantContext>();
           return services;
       }
   }
   ```

3. **Tạo `Infrastructure/DependencyInjection.cs`:**
   ```csharp
   public static class InfrastructureDependencyInjection
   {
       public static IServiceCollection AddInfrastructure(
           this IServiceCollection services, IConfiguration config)
       {
           // DB
           services.AddDbContext<AppDbContext>(...);
           // Repositories
           services.AddScoped<IDocumentRepository, EfDocumentRepository>();
           // Cache
           services.AddStackExchangeRedisCache(o => o.Configuration = config.GetConnectionString("Redis"));
           // Background jobs
           services.AddHostedService<OutboxProcessor>();
           return services;
       }
   }
   ```

4. **Cập nhật `Program.cs`** — chỉ gọi extension methods, không đăng ký trực tiếp:
   ```csharp
   builder.Services
       .AddInfrastructure(builder.Configuration)
       .AddApplication()
       .AddApiServices();
   ```

5. **Thêm container validation** ngay sau `Build()`:
   ```csharp
   builder.Host.UseDefaultServiceProvider((ctx, opts) =>
   {
       opts.ValidateScopes  = ctx.HostingEnvironment.IsDevelopment();
       opts.ValidateOnBuild = ctx.HostingEnvironment.IsDevelopment();
   });
   ```

6. **Kiểm tra service lifetimes:**
   - List tất cả services vừa đăng ký và xác nhận Scoped/Singleton/Transient đúng
   - Đặc biệt: `DbContext` phải là Scoped, `ICurrentUser`/`ITenantContext` phải là Scoped
   - Không có Scoped service nào bị inject vào Singleton

7. **Kiểm tra lại:**
   - `Program.cs` < 50 dòng registration
   - Không có `new ServiceName()` trực tiếp trong code
   - MediatR biết assembly chứa Command/Query handlers chưa

## Edge Cases

- Nếu nhiều bounded context: mỗi context có `DependencyInjection.cs` riêng, `Program.cs` gọi từng cái.
- Nếu dùng Scrutor cho Decorator pattern: thêm `services.Decorate<IRepo, CachedRepo>()` sau khi đăng ký repo gốc.
- Nếu cần Keyed Services (.NET 8): dùng `AddKeyedScoped<IInterface, Impl>("key")`.
- Nếu có circular dependency bị phát hiện: tách service thành smaller services, không inject interface của chính service.

## References

- `ai-rules/10-dependency-injection.md` — Lifetime rules, module pattern, ValidateOnBuild
- `ai-rules/11-configuration.md` — Configuration không inject trực tiếp vào business logic
- `ai-rules/09-error-handling.md` — Đăng ký GlobalExceptionHandler
- `ai-rules/12-caching.md` — Đăng ký Redis + Decorator cho Repository
