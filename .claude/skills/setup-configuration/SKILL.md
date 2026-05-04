---
name: setup-configuration
description: >
  Scaffold the configuration layer: Options classes with validation,
  appsettings.json section structure, secrets strategy, feature flags,
  and registration via AddOptions(). Use when introducing new external dependencies or app modules.
allowed-tools:
  - Read(**/*.cs)
  - Read(appsettings*.json)
  - Read(**/ai-rules/*.md)
  - Glob(src/**/*.cs)
  - Edit(**/*.cs)
  - Edit(appsettings*.json)
---

# Skill: Setup Configuration Layer

## Purpose

Chuẩn hóa config theo Options Pattern, validate ngay tại startup, tách secrets khỏi source code.
Không để business logic đọc `IConfiguration` trực tiếp.

## Instructions

**Input:** Tên module hoặc dependency cần cấu hình (ví dụ: `Database`, `Jwt`, `RabbitMq`, `Redis`).

1. **Đọc `appsettings.json` và `Program.cs` hiện tại** để xác định:
   - Section đã tồn tại chưa
   - Có đang đọc `IConfiguration` trực tiếp ở đâu không

2. **Tạo Options class** tại `Infrastructure/Options/{Name}Options.cs`:
   ```csharp
   public class DatabaseOptions
   {
       public const string SectionName = "Database";

       [Required] public string ConnectionString { get; set; } = default!;
       [Range(1, 10)] public int MaxRetryCount { get; set; } = 3;
       [Range(5, 300)] public int CommandTimeoutSeconds { get; set; } = 30;
   }
   ```
   - Dùng Data Annotations: `[Required]`, `[Range]`, `[Url]`, `[EmailAddress]` khi phù hợp
   - Không dùng `string` literal cho section name — luôn có `SectionName` constant

3. **Cập nhật `appsettings.json` structure** nếu section chưa có:
   ```json
   {
     "Database": {
       "MaxRetryCount": 3,
       "CommandTimeoutSeconds": 30
     },
     "Features": {
       "EnableAiSuggestions": false
     }
   }
   ```
   - Không thêm secrets thật vào file nếu repo sẽ được commit
   - Chỉ thêm placeholder / non-secret defaults

4. **Đăng ký Options trong `Infrastructure/DependencyInjection.cs` hoặc `Program.cs`:**
   ```csharp
   services.AddOptions<DatabaseOptions>()
       .BindConfiguration(DatabaseOptions.SectionName)
       .ValidateDataAnnotations()
       .ValidateOnStart();
   ```

5. **Refactor services đang dùng `IConfiguration` trực tiếp**:
   ```csharp
   // ❌ WRONG
   public class MyService(IConfiguration config) { ... }

   // ✅ CORRECT
   public class MyService(IOptions<DatabaseOptions> options)
   {
       _dbOptions = options.Value;
   }
   ```

6. **Thêm feature flags** nếu module cần bật/tắt theo config:
   ```csharp
   public class FeatureFlags
   {
       public const string SectionName = "Features";
       public bool EnableAiSuggestions { get; set; }
   }
   ```

7. **Kiểm tra lại:**
   - Mọi Options class có `ValidateOnStart()` chưa
   - Không còn `IConfiguration["Section:Key"]` trong business services
   - Secrets không bị hardcode trong appsettings.json

## Edge Cases

- Nếu dependency hỗ trợ secret rotation (Key Vault, Swarm secrets): dùng `IOptionsMonitor<T>` thay vì `IOptions<T>`.
- Nếu config thay đổi theo request/tenant: không dùng global Options — tạo abstraction `ITenantConfigProvider` riêng.
- Nếu section là collection (nhiều endpoints, nhiều queues): dùng `List<TOptionsItem>` trong Options class.
- Nếu project chưa có `appsettings.Development.json`: tạo file này để chứa non-committed dev overrides.

## References

- `ai-rules/11-configuration.md`
- `ai-rules/10-dependency-injection.md`
- `ai-rules/03-security-tenancy.md`
