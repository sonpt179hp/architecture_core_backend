# 03 – Security & Tenancy Rules

**Nguồn:** `backend_core_technical_guidelines.md` §4.2

---

## DO

1. **Luôn resolve `TenantId`** từ JWT claim hoặc trusted header do API Gateway inject.
   **KHÔNG BAO GIỜ** đọc từ request body hoặc query string.

2. **Định nghĩa** `ICurrentUser` và `ITenantContext` trong `Application` layer, cài đặt trong `Infrastructure` (đọc từ `IHttpContextAccessor`).

3. **Bắt buộc** mọi entity multi-tenant implement interface `ITenantEntity` có property `TenantId`.

4. **Đăng ký EF Core Global Query Filter** cho tất cả `ITenantEntity`:
   ```csharp
   builder.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
   ```

5. **Dùng Policy-based Authorization** cho mọi action nghiệp vụ nhạy cảm:
   ```csharp
   [Authorize(Policy = "document:publish")]
   ```

6. **Mọi thao tác admin bypass tenant** (cross-tenant query) phải:
   - Được đánh dấu `[RequireAdminScope]`
   - Ghi Audit Log với đầy đủ context (who, what, when, why)
   - Được review định kỳ

7. **Tách biệt** Audit Log (ai làm gì, trên bản ghi nào) khỏi Technical Log (exception, latency).

## DON'T

1. **KHÔNG** hardcode role string trong Controller hay Handler:
   ```csharp
   // ❌ WRONG
   if (user.IsInRole("Admin")) { ... }

   // ✅ CORRECT
   if (await _authz.AuthorizeAsync(user, "document:delete")) { ... }
   ```

2. **KHÔNG** tin tưởng `TenantId` từ request payload nếu đã có claim trong token.

3. **KHÔNG** để Global Query Filter bị disable ngầm khi dùng `.IgnoreQueryFilters()` mà không có audit justification.

4. **KHÔNG** log token, password, nội dung văn bản bí mật, hay thông tin PII vào Technical Log.

5. **KHÔNG** để `ICurrentUser` hoặc `ITenantContext` được inject vào Domain layer.

## Ví dụ minh họa

```csharp
// ── Application/Abstractions/ICurrentUser.cs
public interface ICurrentUser
{
    Guid UserId { get; }
    string UserName { get; }
    bool IsAuthenticated { get; }
}

// ── Application/Abstractions/ITenantContext.cs
public interface ITenantContext
{
    Guid TenantId { get; }
    bool IsAdminBypass { get; }
}

// ── Infrastructure/Identity/CurrentUser.cs
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public Guid UserId =>
        Guid.Parse(_accessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public string UserName =>
        _accessor.HttpContext!.User.FindFirstValue(ClaimTypes.Name)!;
    public bool IsAuthenticated =>
        _accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}

// ── Domain entity implement ITenantEntity
public interface ITenantEntity
{
    Guid TenantId { get; }
}

public class Document : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    // ...
}

// ── EF Core Global Query Filter
public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
    }
}
```
