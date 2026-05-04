---
name: caching
description: >
  Caching strategies for this .NET 8 LTS multi-tenant Clean Architecture project.
  Default: IDistributedCache (Redis) with Cache-Aside pattern, TenantId in all cache
  keys, and Decorator pattern on repositories. NOT HybridCache (.NET 10 only).
  Load this skill when implementing caching, optimizing read performance, reducing
  database load, or when the user mentions "cache", "IDistributedCache", "Cache-Aside",
  "Redis", "Decorator pattern", "TenantId cache key", "cache invalidation",
  "distributed cache", "IMemoryCache", or "cache stampede".
---

# Caching

## Core Principles

1. **IDistributedCache is the default** — This stack targets .NET 8 LTS. `HybridCache` is a
   .NET 9+ API and is **not available here**. Use `IDistributedCache` backed by Redis via
   `AddStackExchangeRedisCache`. See ADR-004.
2. **Cache-Aside pattern** — Application code checks the cache first; on a miss, fetches from
   the database, stores the result, then returns it. Invalidate on write mutations.
3. **TenantId in every cache key** — This is a multi-tenant system. A cache key without a
   TenantId will cause data leakage across tenants. Always use `CacheKeys.*` helpers.
4. **Decorator pattern on repositories** — Apply caching transparently via a `Cached*Repository`
   decorator registered with Scrutor. Handlers never call cache APIs directly.
5. **Always set TTL** — Every cached entry must have an explicit expiration. No unbounded caches.
6. **Redis fallback** — Wrap all cache reads/writes in try/catch. A Redis outage must degrade
   gracefully to database reads, not bring down the API.

## Patterns

### Registration

```csharp
// Infrastructure/DependencyInjection.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "docmgmt:"; // namespace prefix
});

// Scrutor decorator — registers EfDocumentRepository then wraps it
builder.Services.AddScoped<IDocumentRepository, EfDocumentRepository>();
builder.Services.Decorate<IDocumentRepository, CachedDocumentRepository>();
```

### Cache Key Helpers

```csharp
// Application/Common/Cache/CacheKeys.cs
public static class CacheKeys
{
    // Always include tenantId to prevent cross-tenant data leakage
    public static string Document(Guid tenantId, Guid documentId)
        => $"tenant:{tenantId}:document:{documentId}";

    public static string DocumentList(Guid tenantId, string filter)
        => $"tenant:{tenantId}:documents:{filter}";

    public static string UserPermissions(Guid tenantId, Guid userId)
        => $"tenant:{tenantId}:user:{userId}:permissions";

    public static string OrgTree(Guid tenantId)
        => $"tenant:{tenantId}:org-tree";

    public static string MasterData(Guid tenantId, string type)
        => $"tenant:{tenantId}:master:{type}";
}
```

### Cached Repository Decorator

```csharp
// Infrastructure/Repositories/CachedDocumentRepository.cs
internal sealed class CachedDocumentRepository(
    IDocumentRepository inner,
    IDistributedCache cache,
    ICurrentTenantService tenant,
    ILogger<CachedDocumentRepository> logger) : IDocumentRepository
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public async Task<DocumentDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var key = CacheKeys.Document(tenant.TenantId, id);

        try
        {
            var cached = await cache.GetStringAsync(key, ct);
            if (cached is not null)
                return JsonSerializer.Deserialize<DocumentDto>(cached, _json);
        }
        catch (Exception ex)
        {
            // Redis failure → degrade gracefully, still serve from DB
            logger.LogWarning(ex, "Cache read failed for key {Key}", key);
        }

        var document = await inner.GetByIdAsync(id, ct);

        if (document is not null)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTtl.Document
                };
                await cache.SetStringAsync(key, JsonSerializer.Serialize(document, _json), options, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cache write failed for key {Key}", key);
            }
        }

        return document;
    }

    public async Task InvalidateAsync(Guid id, CancellationToken ct = default)
    {
        await inner.InvalidateAsync(id, ct); // delegate to EF repo for DB write

        var key = CacheKeys.Document(tenant.TenantId, id);
        try { await cache.RemoveAsync(key, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Cache invalidation failed for key {Key}", key); }
    }
}
```

### TTL Reference Table

| Data Type           | TTL       | Rationale |
|---------------------|-----------|-----------|
| Master / lookup data | 60 min   | Changes infrequently; high read volume |
| User / tenant config | 15 min   | May change, staleness risk is low |
| Document metadata   | 10 min    | Moderate churn; acceptable lag |
| Org tree / hierarchy | 30 min   | Rarely changes; expensive to compute |
| Permission sets     | 15 min    | Security-sensitive; short window |

```csharp
// Application/Common/Cache/CacheTtl.cs
public static class CacheTtl
{
    public static readonly TimeSpan MasterData   = TimeSpan.FromMinutes(60);
    public static readonly TimeSpan UserConfig   = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan Document     = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan OrgTree      = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan Permissions  = TimeSpan.FromMinutes(15);
}
```

### Cache Invalidation on Write Commands

```csharp
// Application/Documents/Commands/UpdateDocumentCommandHandler.cs
internal sealed class UpdateDocumentCommandHandler(
    IDocumentRepository repository,     // decorated — handles cache invalidation
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateDocumentCommand>
{
    public async Task Handle(UpdateDocumentCommand command, CancellationToken ct)
    {
        var document = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException(nameof(Document), command.Id);

        document.Update(command.Title, command.Content);

        await unitOfWork.SaveChangesAsync(ct);
        await repository.InvalidateAsync(command.Id, ct); // removes from cache
    }
}
```

## Anti-patterns

### Don't Use HybridCache (.NET 10 Only)

```csharp
// BAD — HybridCache does not exist in .NET 8 LTS
builder.Services.AddHybridCache();
var result = await hybridCache.GetOrCreateAsync("key", factory); // compile error

// GOOD — IDistributedCache with Cache-Aside
var cached = await cache.GetStringAsync(key, ct);
if (cached is null) { /* fetch from DB, then cache.SetStringAsync(...) */ }
```

### Don't Cache Without TenantId in the Key

```csharp
// BAD — tenant A's data leaks to tenant B
var key = $"document:{documentId}";
await cache.SetStringAsync(key, json, options, ct);

// GOOD — always scope to tenant
var key = CacheKeys.Document(tenant.TenantId, documentId);
await cache.SetStringAsync(key, json, options, ct);
```

### Don't Cache Without TTL (Infinite TTL)

```csharp
// BAD — cached forever, stale data guaranteed
await cache.SetStringAsync(key, json);

// GOOD — explicit TTL
await cache.SetStringAsync(key, json,
    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl.Document }, ct);
```

### Don't Cache PII Without Encryption

```csharp
// BAD — plaintext PII in Redis
await cache.SetStringAsync($"user:{userId}:profile", JsonSerializer.Serialize(profile));

// GOOD — encrypt PII or don't cache it; use short-lived tokens or query DB directly
```

### Don't Cache Paginated Lists

```csharp
// BAD — page 1 of 20 with 10 filters cached; combinatorial explosion, staleness nightmare
var key = $"tenant:{tenantId}:documents:page:{page}:size:{size}:filter:{filter}";
await cache.SetStringAsync(key, json, ...);

// GOOD — cache individual entity reads; use Dapper read-path for list queries
var document = await GetByIdAsync(id, ct); // cacheable
var list = await _dapperReader.QueryListAsync(filter, page, ct); // do NOT cache
```

### Don't Let Cache Failure Crash the API

```csharp
// BAD — Redis outage takes down the entire API
var cached = await cache.GetStringAsync(key, ct); // throws, request fails

// GOOD — always wrap in try/catch, degrade to DB
try { cached = await cache.GetStringAsync(key, ct); }
catch (Exception ex) { logger.LogWarning(ex, "Cache read failed"); }
// fall through to DB query
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Entity read (by ID) | Cache in `Cached*Repository` decorator with TTL |
| List / paginated query | Do NOT cache; use Dapper read-path |
| Master / lookup data | Cache with `CacheTtl.MasterData` (60 min) |
| Permissions / auth data | Cache with `CacheTtl.Permissions` (15 min), invalidate on role change |
| After write command | Invalidate via `repository.InvalidateAsync` |
| Redis unavailable | Degrade to DB — log warning, do not throw |
| Multi-tenant isolation | Always use `CacheKeys.*` helpers with `TenantId` |
