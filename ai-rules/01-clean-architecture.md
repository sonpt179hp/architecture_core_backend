# 01 – Clean Architecture Rules

**Nguồn:** `design_pattern_architecture.md` §2.1 · `backend_core_technical_guidelines.md` §4.6

---

## DO

1. **Tuân thủ dependency rule tuyệt đối:**
   ```
   Domain ← Application ← Infrastructure ← API
   ```
   `Domain` và `Application` không được import bất kỳ package nào từ Infrastructure (EntityFrameworkCore, Dapper, MassTransit, Serilog, Polly...).

2. **Đặt Aggregate Root, Value Objects, Domain Events** vào `Domain` layer cho các bounded context cốt lõi (Document, Office Management).

3. **Định nghĩa interface repository** (ví dụ `IDocumentRepository`) trong `Application` layer. Cài đặt cụ thể (`EfDocumentRepository`) đặt trong `Infrastructure` layer.

4. **Dùng CRUD đơn giản** (không có Aggregate Root, không có Value Object) cho các entity phụ trợ như Setting, Master Data.

5. **Áp dụng Architecture Test** (NetArchTest / ArchUnitNET) để enforce dependency rule trong CI pipeline.

6. **Mỗi bounded context** (Document, OrgManagement, UserIdentity) là một module/project riêng, tự quản migration riêng.

## DON'T

1. **KHÔNG** để controller hoặc Infrastructure class nào kế thừa hoặc inject trực tiếp vào Domain entity.

2. **KHÔNG** để Domain layer tham chiếu `Microsoft.EntityFrameworkCore`, `Dapper`, `MassTransit` hoặc bất kỳ framework infrastructure nào.

3. **KHÔNG** dùng `[Table]`, `[Column]` EF annotation trực tiếp trên Domain entity — dùng Fluent Configuration trong `Infrastructure`.

4. **KHÔNG** tạo "God Service" chứa logic của nhiều Aggregate khác nhau.

5. **KHÔNG** áp dụng full DDD (Aggregate Root, Domain Events, Specification) cho Setting/Master Data — đây là over-engineering.

## Ví dụ minh họa

```csharp
// ❌ WRONG — EF annotation trong Domain
public class Document : AggregateRoot
{
    [Column("doc_title")]
    public string Title { get; set; }
}

// ✅ CORRECT — Domain entity thuần
public class Document : AggregateRoot
{
    public DocumentTitle Title { get; private set; }
    private Document() { }

    public static Document Create(DocumentTitle title, TenantId tenantId)
    {
        if (title.IsEmpty) throw new DomainException("Title is required");
        return new Document { Title = title, TenantId = tenantId };
    }
}

// ✅ CORRECT — Fluent config trong Infrastructure
public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.Property(d => d.Title)
               .HasConversion(v => v.Value, v => DocumentTitle.Create(v))
               .HasColumnName("doc_title");
    }
}
```
