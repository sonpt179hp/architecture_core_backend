ư# Skill: Setup Caching Infrastructure

## Purpose

Dựng hạ tầng caching với Redis, cache-aside pattern, TTL rõ ràng.
Dùng `ICacheService` abstraction, cache-aside pattern, graceful fallback khi Redis down.

## Convention Mapping

| Artifact | Convention | Pattern |
|---|---|---|
| **ICacheService** | Interface trong `Application/Abstractions/Caching/` | |
| **CacheService** | Implementation trong `Infrastructure/Caching/` | |
| **RedisOptions** | Options class với `SectionName` | |
| **DI Registration** | `AddScoped<ICacheService, CacheService>` | |
| **Serialization** | `System.Text.Json` với `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` | |

## ICacheService Interface

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default);
}
```

## Project Structure

```
src/
├── Application/
│   └── Abstractions/
│       └── Caching/
│           ├── ICacheService.cs
│           └── I{Entity}CacheService.cs  ← Feature cache interface
└── Infrastructure/
    └── Caching/
        ├── CacheKeys.cs
        ├── CacheService.cs
        ├── RedisOptions.cs
        └── {Feature}/
            └── {Entity}CachingService.cs
```

## Instructions

**Input:** Tên service hoặc feature cần cache, TTL mặc định.

### Step 1: Create CacheKeys Helper

`src/{Solution}/Infrastructure/Caching/CacheKeys.cs`:

```csharp
namespace {Namespace}.Infrastructure.Caching;

public static class CacheKeys
{
    public static string Entity(Guid entityId) =>
        $"entity:{entityId}";

    public static string EntityList(int page, int pageSize) =>
        $"entities:page:{page}:size:{pageSize}";

    public static string EntityByTenant(Guid tenantId, Guid entityId) =>
        $"tenant:{tenantId}:entity:{entityId}";

    // Thêm cache keys khác theo nhu cầu
}
```

### Step 2: Create ICacheService Implementation

`src/{Solution}/Infrastructure/Caching/CacheService.cs`:

```csharp
using System.Text.Json;
using {Namespace}.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace {Namespace}.Infrastructure.Caching;

internal sealed class CacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(key, ct);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var options = new DistributedCacheEntryOptions();

        if (expiry.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiry;
        }

        await cache.SetAsync(key, bytes, options, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default) =>
        await cache.RemoveAsync(key, ct);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory();
        await SetAsync(key, value, expiry, ct);
        return value;
    }
}
```

### Step 3: Create Feature Cache Service

`src/{Solution}/Infrastructure/Caching/{Feature}/{Entity}CachingService.cs`:

```csharp
using {Namespace}.Application.Abstractions.Caching;
using {Namespace}.Domain.{Feature};

namespace {Namespace}.Infrastructure.Caching.{Feature};

internal sealed class {Entity}CachingService : I{Entity}CacheService
{
    private readonly ICacheService _cache;
    private readonly ILogger<{Entity}CachingService> _logger;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);

    public {Entity}CachingService(
        ICacheService cache,
        ILogger<{Entity}CachingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<{Entity}?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var key = CacheKeys.Entity(id);
        try
        {
            return await _cache.GetAsync<{Entity}>(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache get failed for key {Key}", key);
            return null;
        }
    }

    public async Task SetAsync({Entity} entity, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var key = CacheKeys.Entity(entity.Id.Value);
        try
        {
            await _cache.SetAsync(key, entity, ttl ?? DefaultTtl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache set failed for key {Key}", key);
        }
    }

    public async Task InvalidateAsync(Guid id, CancellationToken ct = default)
    {
        var key = CacheKeys.Entity(id);
        try
        {
            await _cache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache invalidate failed for key {Key}", key);
        }
    }
}
```

### Step 4: Register in DependencyInjection

Trong `Infrastructure/DependencyInjection.cs`:

```csharp
services.AddStackExchangeRedisCache(_ => { });
services.AddScoped<ICacheService, CacheService>();
services.AddScoped<I{Entity}CacheService, {Entity}CachingService>();
```

### Step 5: Integrate Into Query Handler (Cache-Aside)

```csharp
internal sealed class Get{Entity}ByIdQueryHandler(
    IApplicationDbContext dbContext,
    I{Entity}CacheService cache) : IQueryHandler<Get{Entity}ByIdQuery, Get{Entity}ByIdResponse>
{
    public async ValueTask<Result<Get{Entity}ByIdResponse>> Handle(
        Get{Entity}ByIdQuery query,
        CancellationToken cancellationToken)
    {
        // Step 1: Try cache
        var cached = await cache.GetByIdAsync(query.{Entity}Id, cancellationToken);
        if (cached is not null)
        {
            return cached.ToResponse();
        }

        // Step 2: Query DB
        var entityId = new {Entity}Id(query.{Entity}Id);
        var entity = await dbContext.{Entities}
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entityId, cancellationToken);

        if (entity is null)
        {
            return {Entity}Errors.NotFound;
        }

        // Step 3: Cache result
        await cache.SetAsync(entity, cancellationToken);

        return entity.ToResponse();
    }
}
```

### Step 6: Integrate Into Command Handler (Invalidation)

```csharp
internal sealed class Create{Entity}CommandHandler(
    IApplicationDbContext dbContext,
    I{Entity}CacheService cache) : ICommandHandler<Create{Entity}Command, {Entity}Id>
{
    public async ValueTask<Result<{Entity}Id>> Handle(
        Create{Entity}Command command,
        CancellationToken cancellationToken)
    {
        var result = {Entity}.Create(command.Name, command.Price);

        if (result.IsFailure)
        {
            return Result<{Entity}Id>.Failure(result.Error);
        }

        var entity = result.Value;
        dbContext.{Entities}.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate list caches
        await cache.InvalidateListCachesAsync(cancellationToken);

        return Result<{Entity}Id>.Success(entity.Id);
    }
}
```

## Checklist

- [ ] Cache key có prefix rõ ràng theo entity
- [ ] TTL được đặt rõ ràng (mặc định 10 phút)
- [ ] Redis down không crash app — có fallback sang DB
- [ ] Write operations invalidate cache đúng key
- [ ] Cache service là Scoped lifetime
- [ ] Log warning khi Redis unavailable, không throw

## Edge Cases

- Entity có relationship phức tạp: chỉ cache entity root.
- Cache list query: cache theo hash của filter params, cẩn thận cache pollution.
- Multi-tenant: luôn include `TenantId` trong cache key.
- Cache warming: tạo `IHostedService` load hot data sau startup.

## References

- `ai-rules/12-caching.md` — Redis, cache-aside, invalidation, decorator pattern
- `ai-rules/03-security-tenancy.md` — TenantId bắt buộc trong cache key
- `ai-rules/10-dependency-injection.md` — Decorator registration với Scrutor
- `ai-rules/06-observability.md` — logging and operational concerns
