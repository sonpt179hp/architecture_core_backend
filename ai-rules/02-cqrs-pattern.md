# 02 – CQRS Pattern Rules

**Nguồn:** `design_pattern_architecture.md` §2.2 · `backend_core_technical_guidelines.md` §4.1

---

## DO

1. **Mỗi API endpoint** sinh đúng bộ 3: `{UseCase}Command` / `{UseCase}Query` + Handler + Validator.
   Đặt trong `Features/{UseCase}/`: `Command.cs`, `CommandHandler.cs`, `CommandValidator.cs`.

2. **Write path** dùng EF Core với Change Tracking (không `AsNoTracking`).

3. **Read path** dùng Dapper + SQL thuần hoặc Database View, trả thẳng DTO — không đi qua Domain entity.

4. **Đăng ký** `ValidationBehavior<TRequest, TResponse>` và `LoggingBehavior<TRequest, TResponse>` vào MediatR Pipeline.

5. **Command handler pattern:**
   ```
   validate (qua pipeline) → load aggregate → gọi domain method → persist → raise domain events
   ```

6. **Query handler pattern:**
   ```
   lấy IDbConnection → chạy SQL/Dapper → map DTO → trả về
   ```

7. Đặt `CancellationToken` làm tham số cuối cùng trong mọi Handler.

8. **Phân trang chuẩn** cho list query:
   ```csharp
   public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
   ```

## DON'T

1. **KHÔNG** để Query handler gọi vào Repository của Domain để lấy data rồi map thủ công sang DTO.

2. **KHÔNG** để Command handler viết SQL thô (Dapper) để thực hiện ghi dữ liệu.

3. **KHÔNG** dùng EF Core `Include()` chain dài trong Query handler — dùng SQL thuần hoặc View thay thế.

4. **KHÔNG** đặt business validation (ví dụ: "văn bản đã phát hành không được sửa") trong FluentValidation Validator.
   Validator chỉ validate format/required của input DTO. Business rule thuộc về Domain entity.

5. **KHÔNG** chia sẻ chung 1 Handler giữa 2 use case khác nhau dù input tương tự.

## Ví dụ minh họa

```csharp
// ── Command ── Application layer
public record CreateDocumentCommand(string Title, string Content, Guid IssuingOfficeId)
    : IRequest<Guid>;

public class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.IssuingOfficeId).NotEmpty();
        // KHÔNG validate business rule ở đây
    }
}

public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, Guid>
{
    public async Task<Guid> Handle(CreateDocumentCommand cmd, CancellationToken ct)
    {
        var doc = Document.Create(DocumentTitle.Create(cmd.Title), _tenantContext.TenantId);
        await _repo.AddAsync(doc, ct);
        await _uow.SaveChangesAsync(ct);
        return doc.Id;
    }
}

// ── Query ── Application layer
public record GetDocumentListQuery(int Page, int PageSize, string? Keyword)
    : IRequest<PagedResult<DocumentSummaryDto>>;

public class GetDocumentListQueryHandler
    : IRequestHandler<GetDocumentListQuery, PagedResult<DocumentSummaryDto>>
{
    public async Task<PagedResult<DocumentSummaryDto>> Handle(
        GetDocumentListQuery q, CancellationToken ct)
    {
        const string sql = @"
            SELECT d.id, d.title, d.issued_at, o.name AS issuing_office
            FROM documents d
            JOIN offices o ON o.id = d.issuing_office_id
            WHERE d.tenant_id = @TenantId
              AND (@Keyword IS NULL OR d.title ILIKE '%' || @Keyword || '%')
            ORDER BY d.issued_at DESC
            LIMIT @PageSize OFFSET @Offset";

        var rows = await _db.QueryAsync<DocumentSummaryDto>(sql, new {
            _tenant.TenantId, q.Keyword, q.PageSize,
            Offset = (q.Page - 1) * q.PageSize
        });
        // ...
    }
}
```
