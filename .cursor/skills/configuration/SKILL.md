---
name: configuration
description: >
  Configuration patterns for .NET 8 applications. Covers the Options pattern,
  IOptionsSnapshot vs IOptions, secrets management, feature flags, Docker Swarm
  Secrets, Azure Key Vault, and environment-based configuration.
  Load this skill when setting up application configuration, managing secrets,
  binding configuration sections, or when the user mentions "configuration",
  "appsettings", "Options pattern", "IOptions", "IOptionsSnapshot", "secrets",
  "user secrets", "environment variables", "connection string", "feature flags",
  "Docker secrets", "SectionName", or "config binding".
---

# Configuration

## Core Principles

1. **Options pattern always** — Never read `IConfiguration` directly in services. Bind configuration sections to strongly-typed classes with validation. This applies to **all layers** — Domain and Application must never receive `IConfiguration` as a dependency.
2. **Validate on startup** — Use `ValidateDataAnnotations()` and `ValidateOnStart()` to catch misconfiguration before the first request.
3. **Secrets never in source** — Use user secrets in development, Azure Key Vault, Docker Swarm Secrets, or environment variables in production. Never commit secrets to git.
4. **Configuration layering** — `appsettings.json` → `appsettings.{Environment}.json` → environment variables → user secrets. Later sources override earlier ones.
5. **`SectionName` constant** — Every options class declares a `public const string SectionName` so the binding key is not duplicated as a magic string across the codebase.

## Patterns

### Options Pattern with SectionName Constant

```csharp
// Options class with validation attributes
public class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public required string ConnectionString { get; init; }

    [Range(1, 100)]
    public int MaxRetryCount { get; init; } = 3;

    [Range(1, 60)]
    public int CommandTimeoutSeconds { get; init; } = 30;
}

// Registration with validation — use nameof or SectionName constant, never a magic string
builder.Services.AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart(); // Fails at startup if configuration is invalid
```

```json
{
  "Database": {
    "ConnectionString": "",
    "MaxRetryCount": 3,
    "CommandTimeoutSeconds": 30
  }
}
```

### Injecting Options

```csharp
// IOptions<T> — singleton, value fixed at startup; use for immutable config
public class OrderService(IOptions<DatabaseOptions> options)
{
    private readonly DatabaseOptions _db = options.Value;
}

// IOptionsSnapshot<T> — scoped, re-reads per request (for hot-reload config)
public class OrderService(IOptionsSnapshot<FeatureFlags> flags)
{
    private readonly FeatureFlags _flags = flags.Value;
}

// IOptionsMonitor<T> — singleton, actively watches for changes; use in background services
public class BackgroundWorker(IOptionsMonitor<WorkerOptions> options)
{
    public void DoWork()
    {
        var current = options.CurrentValue;
    }
}
```

### Feature Flags

Use a dedicated options class with `bool` properties — no library dependency needed for simple flags.

```csharp
public class FeatureFlags
{
    public const string SectionName = "FeatureFlags";

    public bool UseNewPricingEngine { get; init; }
    public bool EnableBulkImport { get; init; }
    public bool ShowBetaDashboard { get; init; }
}

// Registration
builder.Services.AddOptions<FeatureFlags>()
    .BindConfiguration(FeatureFlags.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// appsettings.json
{
  "FeatureFlags": {
    "UseNewPricingEngine": false,
    "EnableBulkImport": true,
    "ShowBetaDashboard": false
  }
}

// Usage — IOptionsSnapshot re-reads per request, so flags can be toggled without restart
public class PricingService(IOptionsSnapshot<FeatureFlags> flags)
{
    public decimal Calculate(Order order) =>
        flags.Value.UseNewPricingEngine
            ? NewEngine.Calculate(order)
            : LegacyEngine.Calculate(order);
}
```

### Custom Validation (Complex Rules)

```csharp
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(options =>
        !string.IsNullOrEmpty(options.Key) &&
        options.Key.Length >= 32 &&
        options.ExpirationMinutes > 0,
        "JWT key must be at least 32 characters and expiration must be positive")
    .ValidateOnStart();
```

### Azure Key Vault (Production)

```csharp
if (builder.Environment.IsProduction())
{
    var keyVaultUri = new Uri(builder.Configuration["KeyVault:Uri"]!);
    builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
}
```

Key Vault secrets use `--` as the hierarchy separator: a secret named `Database--ConnectionString` maps to `Database:ConnectionString` in the Options system.

### Docker Swarm Secrets (Production)

Docker Swarm mounts secrets as files at `/run/secrets/<name>`. Use a custom configuration provider or the built-in file provider.

```csharp
// Program.cs — load Docker secrets as configuration
var secretsPath = "/run/secrets";
if (Directory.Exists(secretsPath))
{
    foreach (var secretFile in Directory.GetFiles(secretsPath))
    {
        // Secret file name becomes the config key; file content becomes the value
        // Convention: name secrets as "Database__ConnectionString" (double underscore = colon)
        builder.Configuration.AddKeyPerFile(secretsPath, optional: true, reloadOnChange: false);
        break; // AddKeyPerFile scans the whole directory; call once
    }
}
```

```yaml
# docker-compose.yml
services:
  api:
    image: myapp-api
    secrets:
      - database_connectionstring
secrets:
  database_connectionstring:
    external: true
# Secret name "database_connectionstring" → env-style: maps to Database:ConnectionString
# via AddKeyPerFile with double-underscore convention
```

**Why**: Docker Swarm Secrets avoid environment variable exposure in `docker inspect`. The secret is only visible inside the container filesystem and is never in the image or compose file.

### Configuration for Multiple Environments

```csharp
// Named options — different config per named instance
builder.Services.AddOptions<SmtpOptions>("internal")
    .BindConfiguration("Smtp:Internal");
builder.Services.AddOptions<SmtpOptions>("customer")
    .BindConfiguration("Smtp:Customer");

// Usage with IOptionsSnapshot
public class EmailService(IOptionsSnapshot<SmtpOptions> options)
{
    public async Task SendInternalEmail(string to, string body)
    {
        var smtp = options.Get("internal");
        // ...
    }
}
```

## Anti-patterns

### NEVER Inject IConfiguration into Domain or Application Layers

```csharp
// BAD — Domain/Application layer depends on infrastructure concern
public class OrderService(IConfiguration config)
{
    public void Process()
    {
        var timeout = int.Parse(config["Database:CommandTimeout"]!); // stringly-typed, no validation
    }
}

// BAD — Application handler knows about raw configuration
public class CreateOrderHandler(IConfiguration config) : IRequestHandler<CreateOrderCommand> { }

// GOOD — Application layer receives strongly-typed options only
public class OrderService(IOptions<DatabaseOptions> options)
{
    public void Process()
    {
        var timeout = options.Value.CommandTimeoutSeconds; // typed, validated at startup
    }
}
```

**Why**: `IConfiguration` is an infrastructure concern. Domain and Application layers should not know how configuration is stored or loaded. Options classes create a clean boundary and enable testing without file system dependencies.

### Don't Put Secrets in appsettings.json

```json
// BAD — committed to source control
{
  "Jwt": { "Key": "super-secret-key" },
  "Database": { "ConnectionString": "Server=prod;Password=secret" }
}

// GOOD — appsettings.json has structure only; secrets come from external source
{
  "Jwt": { "Key": "", "Issuer": "myapp", "Audience": "myapp" },
  "Database": { "ConnectionString": "" }
}
```

### Don't Skip Startup Validation

```csharp
// BAD — misconfiguration discovered at runtime when handler is first called
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// GOOD — fail fast at startup
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### Don't Use Magic Strings for Section Names

```csharp
// BAD — "Database" appears in multiple files; typo causes silent null binding
builder.Services.AddOptions<DatabaseOptions>().BindConfiguration("Database");
builder.Services.AddOptions<DatabaseOptions>().BindConfiguration("Databse"); // typo

// GOOD — SectionName constant defined once on the options class
builder.Services.AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName);
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Binding config to class | Options pattern with `BindConfiguration` + `SectionName` |
| Immutable config (read once) | `IOptions<T>` |
| Config that can change per request | `IOptionsSnapshot<T>` |
| Background service watching config | `IOptionsMonitor<T>` |
| Development secrets | `dotnet user-secrets` |
| Production secrets (cloud) | Azure Key Vault + `DefaultAzureCredential` |
| Production secrets (on-prem/Swarm) | Docker Swarm Secrets + `AddKeyPerFile` |
| Validating config | `ValidateDataAnnotations()` + `ValidateOnStart()` |
| Multiple configs of same type | Named options with `IOptionsSnapshot<T>.Get(name)` |
| Feature toggles | `FeatureFlags` options class with `bool` properties |
