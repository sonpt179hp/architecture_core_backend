# Skill: Setup Dependency Injection

## Purpose

Tổ chức DI registration gọn, theo module, fail-fast ở startup nếu config sai.
Mỗi layer tự đăng ký qua extension method riêng. Không để `Program.cs` phình to.

## Convention Mapping

| Artifact | Convention | Pattern |
|---|---|---|
| **Mediator** | `AddMediator()` | Namespace filter + Scoped lifetime |
| **Pipeline Behaviors** | `LoggingBehavior` + `ValidationBehavior` | Singleton |
| **Validators** | `AddValidatorsFromAssembly()` | Auto-discover |
| **DbContext** | `AddDbContext<T>()` | Scoped |
| **CacheService** | `AddScoped<ICacheService, CacheService>` | Scoped |
| **Interceptors** | `AddSingleton<T>()` | Shared across DbContext instances |
| **Module pattern** | `AddXxx()` extension methods | Each layer owns its registration |

## Project Structure

```
src/
├── Application/
│   └── DependencyInjection.cs       ← AddApplication()
├── Infrastructure/
│   ├── DependencyInjection.cs       ← AddInfrastructure()
│   ├── Persistence/
│   │   └── ApplicationDbContext.cs
│   ├── Caching/
│   │   ├── ICacheService.cs
│   │   └── CacheService.cs
│   └── Authentication/
│       └── JwtServiceExtensions.cs  ← AddJwtAuth()
└── Api/
    └── Program.cs                   ← Gọi chain .AddXxx()
```

## Instructions

**Input:** Tên bounded context, danh sách services cần đăng ký.

### Step 1: Create Application/DependencyInjection.cs

`src/{Solution}/Application/DependencyInjection.cs`:

```csharp
using System.Reflection;
using {Namespace}.Application.Abstractions.Behaviors;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace {Namespace}.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Mediator với namespace filter và scoped lifetime
        services.AddMediator(options =>
        {
            options.Namespace = "{Namespace}.Application";
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        // FluentValidation tự động discover validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Pipeline Behaviors — Singleton vì không giữ state
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
```

**Quy tắc:**

- `AddMediator()` với `Namespace` và `ServiceLifetime.Scoped`
- `AddValidatorsFromAssembly()` — tự động discover
- Pipeline Behaviors là `Singleton`
- KHÔNG đăng ký DbContext, CacheService, hay Infrastructure services ở đây

### Step 2: Create Infrastructure/DependencyInjection.cs

`src/{Solution}/Infrastructure/DependencyInjection.cs`:

```csharp
using {Namespace}.Application.Abstractions.Caching;
using {Namespace}.Application.Abstractions.Data;
using {Namespace}.Infrastructure.Caching;
using {Namespace}.Infrastructure.Persistence;
using {Namespace}.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {Namespace}.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddCaching(configuration);

        return services;
    }

    private static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Missing connection string 'ConnectionStrings:Default'.");

        // Interceptor là Singleton
        services.AddSingleton<UpdateAuditableEntitiesInterceptor>();

        // DbContext là Scoped
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<UpdateAuditableEntitiesInterceptor>();

            options
                .UseNpgsql(connectionString)
                .AddInterceptors(interceptor);
        });

        // IApplicationDbContext alias
        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    private static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(_ => { });

        services.AddScoped<ICacheService, CacheService>();

        return services;
    }
}
```

### Step 3: Create Infrastructure/Authentication/JwtServiceExtensions.cs

`src/{Solution}/Infrastructure/Authentication/JwtServiceExtensions.cs`:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace {Namespace}.Infrastructure.Authentication;

public static class JwtServiceExtensions
{
    public static IServiceCollection AddJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? new JwtOptions();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                };
            });

        services.AddAuthorization();

        return services;
    }
}
```

### Step 4: Update Program.cs

`src/{Solution}/Api/Program.cs`:

```csharp
using {Namespace}.Api.Middleware;
using {Namespace}.Application;
using {Namespace}.Infrastructure;
using {Namespace}.Infrastructure.Authentication;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext());

    // DI Registration — mỗi layer tự đăng ký
    builder.Services
        .AddApplication()           // Mediator + Validators + Pipeline Behaviors
        .AddInfrastructure(builder.Configuration)  // DbContext + Redis
        .AddJwtAuth(builder.Configuration);        // JWT Authentication

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddHealthChecks();

    // Container validation — fail fast ở Development
    builder.Host.UseDefaultServiceProvider((ctx, opts) =>
    {
        opts.ValidateScopes  = ctx.HostingEnvironment.IsDevelopment();
        opts.ValidateOnBuild = ctx.HostingEnvironment.IsDevelopment();
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
```

## Checklist

- [ ] `Program.cs` gọi chain `.AddApplication().AddInfrastructure().AddJwtAuth()`
- [ ] `AddApplication()` đăng ký Mediator + Validators + Pipeline Behaviors
- [ ] `AddInfrastructure()` đăng ký DbContext + Redis + Interceptors
- [ ] `LoggingBehavior` và `ValidationBehavior` là `Singleton`
- [ ] `DbContext` là `Scoped`
- [ ] Container validation chỉ bật trong Development

## Edge Cases

- Nhiều bounded context: mỗi context có `DependencyInjection.cs` riêng.
- Keyed Services (.NET 8): dùng `AddKeyedScoped<IInterface, Impl>("key")`.
- Circular dependency: tách service thành smaller services.
- Decorator pattern: dùng Scrutor `services.Decorate<IRepo, CachedRepo>()`.

## References

- `{Solution}/Application/DependencyInjection.cs`
- `{Solution}/Infrastructure/DependencyInjection.cs`
- `{Solution}/Api/Program.cs`
