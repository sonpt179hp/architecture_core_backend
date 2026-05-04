# Skill: Setup API Layer

## Purpose

Dựng đầy đủ tầng API: Program.cs skeleton, Controllers, Middleware, Health Checks, Swagger configuration.
Đây là entry point cuối cùng, chỉ gọi các extension methods từ Application và Infrastructure layers.

## Convention Mapping

| Artifact | Convention | Pattern |
|---|---|---|
| **Entry Point** | `Program.cs` | Chỉ gọi `AddXxxServices()` và middleware pipeline |
| **Base Controller** | `ApiController(ISender sender)` | Kế thừa `ControllerBase` |
| **Serilog** | Bootstrap logger + `UseSerilogRequestLogging()` | |
| **Exception Handler** | `IExceptionHandler` → `GlobalExceptionHandler` | |
| **Health Checks** | `AddNpgSql()` + `AddRedis()` | |
| **Swagger** | `IConfigureOptions<SwaggerGenOptions>` | |

## Project Structure

```
src/
├── Api/
│   ├── Program.cs
│   ├── Controllers/
│   │   ├── ApiController.cs
│   │   └── {Feature}Controller.cs
│   ├── Middleware/
│   │   └── GlobalExceptionHandler.cs
│   ├── Extensions/
│   │   └── ResultExtensions.cs
│   └── OpenApi/
│       └── ConfigureSwaggerOptions.cs
└── appsettings.json
```

## Instructions

**Input:** Tên bounded context, features cần expose qua API.

### Step 1: Create Program.cs

`src/{Solution}/Api/Program.cs`:

```csharp
using {Namespace}.Api.Middleware;
using {Namespace}.Api.OpenApi;
using {Namespace}.Application;
using {Namespace}.Infrastructure;
using {Namespace}.Infrastructure.Authentication;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName());

    // DI Registration — mỗi layer tự đăng ký qua extension methods
    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddJwtAuth(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

    // Exception Handling
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Health Checks
    builder.Services
        .AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("Default")!, name: "postgres")
        .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis");

    // Container Validation — fail fast ở Development
    builder.Host.UseDefaultServiceProvider((ctx, opts) =>
    {
        opts.ValidateScopes  = ctx.HostingEnvironment.IsDevelopment();
        opts.ValidateOnBuild = ctx.HostingEnvironment.IsDevelopment();
    });

    var app = builder.Build();

    // Middleware Pipeline
    app.UseSerilogRequestLogging();

    // Exception handling — ĐẶT ĐẦU TIÊN
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

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

### Step 2: Create ApiController Base

`src/{Solution}/Api/Controllers/ApiController.cs`:

```csharp
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace {Namespace}.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiController(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;
}
```

### Step 3: Create GlobalExceptionHandler

`src/{Solution}/Api/Middleware/GlobalExceptionHandler.cs`:

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace {Namespace}.Api.Middleware;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred");

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Type = "https://httpstatuses.com/500"
        };

        if (environment.IsDevelopment())
        {
            problemDetails.Detail = exception.Message;
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
```

### Step 4: Create Swagger Configuration

`src/{Solution}/Api/OpenApi/ConfigureSwaggerOptions.cs`:

```csharp
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace {Namespace}.Api.OpenApi;

internal sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Enter JWT Bearer token: Bearer {token}",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        options.AddSecurityDefinition("Bearer", securityScheme);

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { securityScheme, Array.Empty<string>() }
        });

        var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    }
}
```

### Step 5: Create Sample Controller

`src/{Solution}/Api/Controllers/{Feature}Controller.cs`:

```csharp
using {Namespace}.Api.Controllers;
using {Namespace}.Application.{Feature}.Commands.Create{Entity};
using {Namespace}.Application.{Feature}.Queries.Get{Entity}ById;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace {Namespace}.Api.Controllers;

public sealed class {Feature}Controller(ISender sender) : ApiController(sender)
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new Get{Entity}ByIdQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] Create{Entity}Command command,
        CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);

        return result.Match(
            id => CreatedAtAction(nameof(GetById), new { id = id.Value }, null),
            error => error.ToActionResult());
    }
}
```

## Checklist

- [ ] `Program.cs` chỉ gọi extension methods, không đăng ký service trực tiếp
- [ ] `GlobalExceptionHandler` implement `IExceptionHandler`, log exception, trả ProblemDetails
- [ ] Serilog bootstrap logger đặt trước `CreateBuilder`
- [ ] Health check endpoint expose tại `/health`
- [ ] Swagger có JWT Bearer authentication
- [ ] Container validation bật ở Development

## Edge Cases

- API versioning: thêm `builder.Services.AddApiVersioning()` và `[ApiVersion("1.0")]`.
- Rate Limiting: thêm `builder.Services.AddRateLimiter()` và `[EnableRateLimiting]`.
- CORS: thêm `builder.Services.AddCors()` và `app.UseCors()`.

## References

- `{Solution}/Api/Program.cs`
- `{Solution}/Api/Middleware/GlobalExceptionHandler.cs`
- `{Solution}/Api/Controllers/ApiController.cs`
