# 08 – EF Core 9 Operational Rules

**Nguồn:** `backend_core_technical_guidelines.md` §4.7

---

## DO

1. **Dùng `AsNoTracking()`** hoặc `AsNoTrackingWithIdentityResolution()` cho mọi query read-only qua EF Core.

2. **Mỗi bounded context** có migration project riêng. Apply migration qua CLI hoặc startup job riêng biệt:
   ```bash
   dotnet ef database update --project Infrastructure --startup-project MigrationRunner
   ```

3. **Cài đặt Optimistic Concurrency** cho các entity nghiệp vụ quan trọng:
   ```csharp
   builder.Property<byte[]>("RowVersion")
          .IsRowVersion()
          .HasColumnName("xmin")
          .HasColumnType("xid");  // PostgreSQL native
   ```

4. **Chỉ mở explicit transaction** khi use case thực sự cần atomicity qua nhiều aggregate:
   ```csharp
   await using var tx = await _db.Database.BeginTransactionAsync(ct);
   // ... các thao tác ...
   await tx.CommitAsync(ct);
   ```

5. **Dùng `ExecuteUpdateAsync` / `ExecuteDeleteAsync`** (EF Core 7+) cho bulk operations thay vì load entity rồi loop:
   ```csharp
   await _db.Documents
            .Where(d => d.ArchivedAt < cutoff)
            .ExecuteDeleteAsync(ct);
   ```

6. **Đặt CommandTimeout explicit** trên DbContext options:
   ```csharp
   options.UseNpgsql(connStr, o => o.CommandTimeout(30));
   ```

## DON'T

1. **KHÔNG** gọi `Database.MigrateAsync()` tự động khi app startup ở production.
   Chỉ cho phép ở development/testing.

2. **KHÔNG** dùng `Include()` chain dài (>3 cấp) trong write-path handler.
   Đây là dấu hiệu cần tách query riêng (CQRS read path).

3. **KHÔNG** để transaction mở suốt toàn bộ request scope mà không cần thiết.
   Tăng lock contention, giảm concurrency.

4. **KHÔNG** dùng EF Core cho read path phức tạp.
   Dùng Dapper + SQL thuần (xem `.cursor/rules/02-cqrs-pattern.md`).

5. **KHÔNG** bỏ qua `DbUpdateConcurrencyException`.
   Phải xử lý rõ ràng: reload entity → apply conflict resolution → trả lỗi 409 Conflict cho client.

6. **KHÔNG** share DbContext giữa các thread.
   Mỗi request phải có DbContext scope riêng (đã được DI container đảm bảo mặc định).

## Ví dụ minh họa

```csharp
// ── Concurrency conflict handling
public async Task<IActionResult> UpdateDocument(
    UpdateDocumentCommand cmd, CancellationToken ct)
{
    try
    {
        await _mediator.Send(cmd, ct);
        return NoContent();
    }
    catch (DbUpdateConcurrencyException ex)
    {
        return Conflict(new ProblemDetails
        {
            Status = 409,
            Title = "ConcurrencyConflict",
            Detail = "The document was modified by another user. Please refresh and retry."
        });
    }
}

// ── AsNoTracking cho read
public async Task<IReadOnlyList<DocumentSummaryDto>> GetDocuments(
    Guid tenantId, CancellationToken ct)
{
    return await _db.Documents
                    .AsNoTracking()
                    .Where(d => d.TenantId == tenantId)
                    .OrderByDescending(d => d.CreatedAt)
                    .Select(d => new DocumentSummaryDto(d.Id, d.Title.Value, d.Status))
                    .ToListAsync(ct);
}

// ── Bulk delete
public async Task<int> ArchiveOldDocuments(DateTime cutoff, CancellationToken ct)
{
    return await _db.Documents
                    .Where(d => d.Status == DocumentStatus.Draft
                             && d.CreatedAt < cutoff)
                    .ExecuteDeleteAsync(ct);
}

// ── DbContext configuration
protected override void OnConfiguring(DbContextOptionsBuilder o)
{
    o.UseNpgsql(_connStr, npgsql =>
    {
        npgsql.CommandTimeout(30);
        npgsql.EnableRetryOnFailure(2);
    });
}
```
