# Skill: Setup Configuration Layer

## Purpose

Chuẩn hóa config theo Options Pattern, validate ngay tại startup, tách secrets khỏi source code.
Không để business logic đọc `IConfiguration` trực tiếp.

## Convention Mapping

| Artifact | Convention | Pattern |
|---|---|---|
| **Options class** | `public sealed class {Name}Options` | |
| **SectionName** | `public const string SectionName = "{Name}";` | |
| **Validation** | `ValidateDataAnnotations()` + `ValidateOnStart()` | |
| **Registration** | `AddOptions<T>().Bind().Validate().ValidateOnStart()` | |
| **appsettings** | `appsettings.json` + `appsettings.{Environment}.json` | |

## Project Structure

```
src/
├── Infrastructure/
│   └── Options/
│       ├── {Name}Options.cs
│       └── {Other}Options.cs
└── Api/
    └── appsettings.json
```

## Instructions

**Input:** Tên module cần cấu hình (Jwt, Redis, RabbitMQ, Database...).

### Step 1: Create Options Class

`src/{Solution}/Infrastructure/Options/{Name}Options.cs`:

```csharp
namespace {Namespace}.Infrastructure.Options;

public sealed class {Name}Options
{
    public const string SectionName = "{Name}";

    // Required fields
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 80;

    // Optional fields với defaults
    public string? Password { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}
```

**Quy tắc Options class:**

- Có `SectionName = "{Name}"` constant
- Dùng auto-properties
- Required fields không có default hoặc có default rõ ràng
- Có Data Annotations: `[Required]`, `[Range]`, `[Url]`

### Step 2: Update appsettings.json

```json
{
  "{Name}": {
    "Host": "localhost",
    "Port": 6379,
    "Password": null,
    "TimeoutSeconds": 30,
    "MaxRetries": 3
  }
}
```

### Step 3: Register Options in DI

```csharp
private static IServiceCollection Add{Name}(this IServiceCollection services, IConfiguration configuration)
{
    services
        .AddOptions<{Name}Options>()
        .Bind(configuration.GetSection({Name}Options.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    return services;
}
```

### Step 4: Inject into Service

```csharp
// KHÔNG làm thế này
public class MyService(IConfiguration config)
{
    var host = config["{Name}:Host"]; // ❌ WRONG
}

// MÀ LÀM THẾ NÀY
public class MyService(IOptions<{Name}Options> options)
{
    var host = options.Value.Host; // ✅ CORRECT
}
```

## Checklist

- [ ] Options class có `SectionName` constant
- [ ] Options được validate bằng Data Annotations
- [ ] `ValidateOnStart()` được gọi — app fail fast nếu config sai
- [ ] Secrets không bị hardcode trong appsettings.json
- [ ] Business services dùng `IOptions<T>` thay vì `IConfiguration`

## Edge Cases

- Secret rotation (Key Vault): dùng `IOptionsMonitor<T>`.
- Environment-specific config: tạo `appsettings.{Environment}.json`.
- Secrets từ environment: dùng `Environment.GetEnvironmentVariable()`.

## References

- `ai-rules/11-configuration.md`
- `ai-rules/10-dependency-injection.md`
- `ai-rules/03-security-tenancy.md`
