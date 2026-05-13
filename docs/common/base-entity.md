# Base Entity chung cho toàn bộ table

- **Ngày cập nhật:** 08/05/2026

---

## 1. Schema (DBML)

```dbml
Project dashboard_portal {
  database_type: "SQLServer"
}

Table BaseEntity [headercolor: #42A5F5] {
  TenantId   uniqueidentifier [not null,            note: 'Định danh tenant; lấy từ JWT claim, không đọc từ request body']
  CreatedAt  datetime2        [not null,            note: 'UTC – tự set khi INSERT']
  CreatedBy  uniqueidentifier [not null,            note: 'UserId người tạo']
  UpdatedAt  datetime2        [null,                note: 'UTC – null nếu chưa từng update']
  UpdatedBy  uniqueidentifier [null,                note: 'null nếu chưa từng update']
  IsDeleted  bit              [not null, default: 0, note: 'Soft-delete; không xóa vật lý khỏi DB']
  DeletedAt  datetime2        [null,                note: 'null khi chưa xóa']
  DeletedBy  uniqueidentifier [null,                note: 'null khi chưa xóa']
  RowVersion rowversion       [not null,            note: 'Optimistic concurrency – EF Core tự quản lý']
}
```

---

## 2. C# class

`BaseEntity<TId>` là lớp abstract dùng chung. Mọi entity trong dự án đều kế thừa lớp này.

```csharp
// Domain/Primitives/BaseEntity.cs
public abstract class BaseEntity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected BaseEntity(TId id, Guid tenantId, Guid createdBy)
    {
        Id        = id;
        TenantId  = tenantId;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    protected BaseEntity() { Id = default!; } // EF Core

    public TId       Id         { get; private set; }
    public Guid      TenantId   { get; private set; }
    public DateTime  CreatedAt  { get; private set; }
    public Guid      CreatedBy  { get; private set; }
    public DateTime? UpdatedAt  { get; private set; }
    public Guid?     UpdatedBy  { get; private set; }
    public bool      IsDeleted  { get; private set; }
    public DateTime? DeletedAt  { get; private set; }
    public Guid?     DeletedBy  { get; private set; }
    public byte[]    RowVersion { get; private set; } = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public void SetUpdated(Guid updatedBy)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete(Guid deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }
}
```

**Giải thích:**

- `IDomainEvent` — interface marker rỗng, dùng để Domain layer không phụ thuộc thư viện ngoài (MassTransit/MediatR). Infrastructure layer mới convert sang `INotification` để publish.
- `SetUpdated()` / `SoftDelete()` — method duy nhất được phép thay đổi audit fields; không set trực tiếp từ ngoài.
- `UpdatedAt`, `UpdatedBy`, `DeletedAt`, `DeletedBy` để nullable vì lần đầu tạo chưa có giá trị thực.

---

## 3. Cách các table kế thừa

Mỗi entity chỉ cần kế thừa `BaseEntity<TId>`, tự động có đủ các field chung:

```csharp
// Ví dụ: bảng Document
public class Document : BaseEntity<Guid>
{
    public Document(Guid id, Guid tenantId, Guid createdBy, string title)
        : base(id, tenantId, createdBy)
    {
        Title = title;
    }

    protected Document() { } // EF Core

    public string Title { get; private set; } = string.Empty;
}
```

EF Core sẽ tự map tất cả field của `BaseEntity` vào table `Documents` — không cần khai báo lại.

---

## 4. Ghi chú thiết kế

| Field | Nullable | Lý do |
| --- | --- | --- |
| `TenantId` | NO | Mọi bản ghi phải thuộc 1 tenant |
| `CreatedAt` | NO | Luôn có khi INSERT; dùng `datetime2` thay `datetime` để tránh mất precision |
| `CreatedBy` | NO | Audit trail tối thiểu |
| `UpdatedAt` | YES | `null` = chưa từng update |
| `UpdatedBy` | YES | `null` = chưa từng update |
| `IsDeleted` | NO | Soft-delete flag |
| `DeletedAt` | YES | `null` = chưa xóa |
| `DeletedBy` | YES | `null` = chưa xóa |
| `RowVersion` | NO | Ngăn lost-update khi nhiều user cùng sửa 1 bản ghi |

### Quy tắc bắt buộc

- `TenantId` **không bao giờ** đọc từ request body; chỉ từ JWT claim.
- `CreatedAt`, `UpdatedAt`, `DeletedAt` luôn dùng **UTC** (`DateTime.UtcNow`).
- `RowVersion` do EF Core tự quản lý; không set thủ công.
- Chỉ gọi `SoftDelete()` để xóa mềm; không set `IsDeleted = true` trực tiếp.
