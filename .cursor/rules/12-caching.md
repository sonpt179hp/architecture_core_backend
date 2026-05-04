# 12 – Caching Strategy Rules

**Nguồn:** `backend_core_technical_guidelines.md` §4.4 · §4.5

---

## DO

1. **Dùng `IDistributedCache` (Redis)** cho multi-instance deployment — không dùng `IMemoryCache` cho data cần share giữa các instance:
   ```csharp
   services.AddStackExchangeRedisCache(opts =>
       opts.Configuration = config.GetConnectionString("Redis"));
   ```

2. **Áp dụng Cache-Aside Pattern** — không bao giờ để cache là nguồn sự thật duy nhất:
   ```
   1. Đọc từ cache
   2. Cache miss → đọc từ DB
   3. Ghi vào cache với TTL
   4. Trả về data
   ```

3. **Cache key phải include TenantId** để tránh cross-tenant data leak:
   ```csharp
   var key = CacheKeys.Document(_tenantContext.TenantId, documentId);
   // → "tenant:{tenantId}:doc:{documentId}"
   ```

4. **Luôn đặt TTL rõ ràng** — không bao giờ cache không có expiry:
   ```csharp
   var options = new DistributedCacheEntryOptions
   {
       AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
       SlidingExpiration               = TimeSpan.FromMinutes(2),
   };
   ```

5. **Invalidate cache chủ động** sau khi write:
   ```csharp
   await _cache.RemoveAsync(CacheKeys.Document(tenantId, documentId), ct);
   ```

6. **Dùng Decorator Pattern** để bọc repository — không nhét cache logic vào handler:
   ```csharp
   services.AddScoped<IDocumentRepository, EfDocumentRepository>();
   services.Decorate<IDocumentRepository, CachedDocumentRepository>();
   ```

7. **Serialize/deserialize dùng `System.Text.Json`** — nhỏ, nhanh, không cần thư viện ngoài.

## DON'T

1. **KHÔNG** cache data nhạy cảm (nội dung văn bản mật, thông tin PII) mà không mã hóa.

2. **KHÔNG** cache cross-tenant data — mỗi cache entry phải scoped theo TenantId:
   ```csharp
   // ❌ WRONG — key không có tenantId
   var key = $"documents:{id}";
   // ✅ CORRECT
   var key = $"tenant:{tenantId}:documents:{id}";
   ```

3. **KHÔNG** cache mãi mãi (TTL = null) cho data thay đổi thường xuyên.

4. **KHÔNG** để cache miss block toàn bộ request nếu Redis down — implement fallback:
   ```csharp
   try { cached = await _cache.GetStringAsync(key, ct); }
   catch (RedisException ex)
   {
       _logger.LogWarning(ex, "Redis unavailable, falling back to database");
       return await _inner.GetByIdAsync(id, ct); // fallback
   }
   ```

5. **KHÔNG** cache kết quả query phân trang động (vì filter/page thay đổi liên tục) — chỉ cache entity đơn lẻ theo ID.

6. **KHÔNG** dùng `IMemoryCache` cho data cần consistent giữa nhiều instance API.

## TTL Reference

| Loại data | TTL gợi ý |
|---|---|
| Master data (danh mục, cấu hình) | 60 phút |
| Thông tin user/tenant | 15 phút |
| Document detail | 10 phút |
| Cây tổ chức (ltree) | 30 phút |
| Token/session | = thời gian hết hạn token |

## Ví dụ minh họa

```csharp
// ── Infrastructure/Caching/CacheKeys.cs
public static class CacheKeys
{
    public static string Document(Guid tenantId, Guid docId) =>
        $"tenant:{tenantId}:doc:{docId}";

    public static string OrgTree(Guid tenantId) =>
        $"tenant:{tenantId}:org-tree";

    public static string UserPermissions(Guid tenantId, Guid userId) =>
        $"tenant:{tenantId}:user:{userId}:permissions";
}

// ── Infrastructure/Caching/CachedDocumentRepository.cs
public class CachedDocumentRepository : IDocumentRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var key = CacheKeys.Document(_tenant.TenantId, id);

        try
        {
            var cached = await _cache.GetStringAsync(key, ct);
            if (cached is not null)
                return JsonSerializer.Deserialize<Document>(cached);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for key {CacheKey}", key);
        }

        var entity = await _inner.GetByIdAsync(id, ct);

        if (entity is not null)
        {
            try
            {
                await _cache.SetStringAsync(key, JsonSerializer.Serialize(entity),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl }, ct);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Failed to cache document {DocumentId}", id);
            }
        }

        return entity;
    }

    public async Task UpdateAsync(Document entity, CancellationToken ct)
    {
        await _inner.UpdateAsync(entity, ct);
        // Invalidate sau khi update
        await _cache.RemoveAsync(CacheKeys.Document(_tenant.TenantId, entity.Id), ct);
    }
}

// ── Infrastructure/DependencyInjection.cs
services.AddScoped<IDocumentRepository, EfDocumentRepository>();
services.Decorate<IDocumentRepository, CachedDocumentRepository>();
// (Scrutor package cho Decorate)
```
