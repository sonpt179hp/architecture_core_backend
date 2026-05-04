# 11 – Configuration Management Rules

**Nguồn:** `backend_core_technical_guidelines.md` §4.2 · §4.7

---

## DO

1. **Dùng Options Pattern** thay vì đọc `IConfiguration` trực tiếp:
   - `IOptions<T>` — giá trị bất biến trong lifetime của app (Singleton)
   - `IOptionsSnapshot<T>` — reload mỗi request (Scoped)
   - `IOptionsMonitor<T>` — live reload với callback khi config thay đổi

2. **Validate configuration tại startup** — fail fast thay vì fail sau:
   ```csharp
   services.AddOptions<DatabaseOptions>()
       .BindConfiguration("Database")
       .ValidateDataAnnotations()
       .ValidateOnStart();
   ```

3. **Cấu trúc appsettings theo section rõ ràng:**
   ```json
   {
     "Database": { "ConnectionString": "...", "MaxRetryCount": 3 },
     "Redis":    { "ConnectionString": "..." },
     "Jwt":      { "Issuer": "...", "Audience": "...", "ExpiryMinutes": 60 },
     "RabbitMq": { "Host": "...", "Username": "...", "Password": "..." },
     "Features": { "EnableAiSuggestions": false }
   }
   ```

4. **Phân tách config theo môi trường:**
   ```
   appsettings.json              ← default values, không có secrets
   appsettings.Development.json  ← dev overrides, không commit secrets
   appsettings.Production.json   ← production non-secret values
   ```
   Secrets ở dev dùng `dotnet user-secrets`. Ở production dùng Docker Swarm Secrets hoặc Vault.

5. **Options class dùng Data Annotations để validate:**
   ```csharp
   public class JwtOptions
   {
       public const string SectionName = "Jwt";

       [Required] public string Issuer    { get; set; } = default!;
       [Required] public string Audience  { get; set; } = default!;
       [Required] public string SecretKey { get; set; } = default!;
       [Range(1, 1440)] public int ExpiryMinutes { get; set; } = 60;
   }
   ```

6. **Feature flags** cho toggle tính năng không cần redeploy:
   ```csharp
   public class FeatureFlags
   {
       public bool EnableAiSuggestions { get; set; }
       public bool EnableBulkImport    { get; set; }
   }
   // Usage:
   if (_features.Value.EnableAiSuggestions) { ... }
   ```

## DON'T

1. **KHÔNG** đọc `IConfiguration["Section:Key"]` trực tiếp trong business logic:
   ```csharp
   // ❌ WRONG
   var connStr = _config["Database:ConnectionString"];
   // ✅ CORRECT
   var connStr = _dbOptions.Value.ConnectionString;
   ```

2. **KHÔNG** commit secrets, connection strings, API keys vào source code hay appsettings.json:
   ```json
   // ❌ WRONG — commit vào git
   { "Jwt": { "SecretKey": "my-super-secret-key" } }
   ```
   Dùng `dotnet user-secrets set "Jwt:SecretKey" "..."` khi phát triển.

3. **KHÔNG** bỏ qua `ValidateOnStart()` — nếu config thiếu/sai sẽ crash runtime thay vì startup.

4. **KHÔNG** inject `IConfiguration` vào Domain hoặc Application layer.

5. **KHÔNG** dùng `string` constant trực tiếp để trỏ section name:
   ```csharp
   // ❌ WRONG — dễ typo, không refactor được
   .BindConfiguration("Dtabase")
   // ✅ CORRECT
   .BindConfiguration(DatabaseOptions.SectionName)
   ```

## Ví dụ minh họa

```csharp
// ── Infrastructure/Options/DatabaseOptions.cs
public class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string ConnectionString { get; set; } = default!;

    [Range(1, 10)]
    public int MaxRetryCount { get; set; } = 3;

    [Range(5, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;
}

// ── Infrastructure/Options/RabbitMqOptions.cs
public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required] public string Host     { get; set; } = default!;
    [Required] public string Username { get; set; } = default!;
    [Required] public string Password { get; set; } = default!;
    public int Port { get; set; } = 5672;
}

// ── Infrastructure/DependencyInjection.cs
services.AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

services.AddOptions<RabbitMqOptions>()
    .BindConfiguration(RabbitMqOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ── appsettings.json (safe — không có secrets)
{
  "Database": {
    "MaxRetryCount": 3,
    "CommandTimeoutSeconds": 30
  },
  "Jwt": {
    "Issuer": "https://auth.mbfs.vn",
    "Audience": "mbfs-api",
    "ExpiryMinutes": 60
  },
  "Features": {
    "EnableAiSuggestions": false,
    "EnableBulkImport": true
  }
}

// ── appsettings.Development.json (không commit ConnectionString thật)
{
  "Database": {
    "ConnectionString": "Host=localhost;Database=mbfs_dev;Username=postgres"
  }
}
```
