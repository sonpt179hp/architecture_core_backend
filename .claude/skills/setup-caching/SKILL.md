---
name: setup-caching
description: >
  Scaffold distributed caching infrastructure: Redis connection, cache service abstraction,
  cached repository decorator pattern, cache key conventions with TenantId prefix,
  and cache invalidation helpers. Use when adding caching to a bounded context.
allowed-tools:
  - Read(**/*.cs)
  - Read(**/ai-rules/*.md)
  - Glob(src/**/*.cs)
  - Glob(src/**/*.csproj)
  - Edit(**/*.cs)
---

# Skill: Setup Caching Infrastructure

## Purpose

Dựng hạ tầng caching với Redis, cache-aside pattern, TTL rõ ràng, cache key có TenantId prefix.
Dùng Decorator Pattern để bọc repository — không nhét cache logic vào handler.

## Instructions

**Input:** Tên repository hoặc service cần cache (ví dụ: `DocumentRepository`), TTL mặc định (ví dụ: 10 phút).

1. **Đọc cấu trúc project hiện tại** để xác định:
   - Repository interface đã tồn tại chưa
   - Redis đã được đăng ký trong DI chưa
   - Có `ITenantContext` chưa

2. **Đăng ký Redis trong `Infrastructure/DependencyInjection.cs`** nếu chưa có:
   ```csharp
   services.AddStackExchangeRedisCache(opts =>
       opts.Configuration = config.GetConnectionString("Redis"));
   ```

3. **Tạo `CacheKeys` helper** tại `Infrastructure/Caching/CacheKeys.cs`:
   ```csharp
   public static class CacheKeys
   {
       public static string Document(Guid tenantId, Guid docId) =>
           $"tenant:{tenantId}:doc:{docId}";

       public static string DocumentList(Guid tenantId, int page, int pageSize) =>
           $"tenant:{tenantId}:docs:page:{page}:size:{pageSize}";

       public static string OrgTree(Guid tenantId) =>
           $"tenant:{tenantId}:org-tree";
   }
   ```

4. **Tạo Cached Repository Decorator** tại `Infrastructure/Caching/Cached{Entity}Repository.cs`:
   ```csharp
   public class CachedDocumentRepository : IDocumentRepository
   {
       private readonly IDocumentRepository _inner;
       private readonly IDistributedCache _cache;
       private readonly ITenantContext _tenant;
       private readonly ILogger<CachedDocumentRepository> _logger;
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
           await _cache.RemoveAsync(CacheKeys.Document(_tenant.TenantId, entity.Id), ct);
       }
   }
   ```

5. **Đăng ký Decorator trong DI** (dùng Scrutor package):
   ```csharp
   services.AddScoped<IDocumentRepository, EfDocumentRepository>();
   services.Decorate<IDocumentRepository, CachedDocumentRepository>();
   ```
   Nếu chưa có Scrutor: `dotnet add package Scrutor`

6. **Thêm cache invalidation** cho các method write (Update, Delete):
   - Sau mỗi write operation → `_cache.RemoveAsync(key)`
   - Nếu có list cache → invalidate cả list key pattern

7. **Kiểm tra lại:**
   - Cache key luôn có `tenant:{tenantId}:` prefix
   - TTL được đặt rõ ràng, không cache mãi mãi
   - Redis down không làm crash app — có fallback về DB
   - Write operations invalidate cache đúng key

## Edge Cases

- Nếu entity có relationship phức tạp: chỉ cache entity root, không cache toàn bộ graph.
- Nếu cần cache list query: cache theo hash của filter params, nhưng cẩn thận với cache pollution.
- Nếu Redis cluster: đảm bảo key distribution đều (không để tất cả key cùng prefix vào 1 shard).
- Nếu cần cache warming: tạo background job load hot data vào cache sau khi app start.

## References

- `ai-rules/12-caching.md` — Cache-aside pattern, TTL strategy, TenantId prefix, invalidation
- `ai-rules/03-security-tenancy.md` — TenantId bắt buộc trong cache key
- `ai-rules/10-dependency-injection.md` — Decorator registration với Scrutor
- `ai-rules/06-observability.md` — Log warning khi Redis unavailable, không log Error
