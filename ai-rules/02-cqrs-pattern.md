# 02 – CQRS Pattern Rules

**Nguồn:** `design_pattern_architecture.md` §2.2 · `backend_core_technical_guidelines.md` §4.1

---

## Mediator Library

Dự án dùng **Mediator (MediatR từ Arch.Ext)**, KHÔNG phải MediatR (Jimmy Bogard's).

| | Mediator (Arch.Ext) | MediatR |
|---|---|---|
| Packages | `Arch.Extensions.MediatR` | `MediatR` |
| Interface | `ICommand`, `ICommand<T>` | `IRequest`, `IRequest<T>` |
| Handler | `ICommandHandler<,>`, `IQueryHandler<,>` | `IRequestHandler<,>` |
| Sender | `ISender` | `ISender` |

---

## CQRS Decision Matrix

| Thao tác | Cách thực hiện |
|---|---|
| **Tạo mới (Create)** | EF Core |
| **Cập nhật (Update)** | EF Core |
| **Xóa (Delete)** | EF Core |
| **Đọc đơn giản** (1 entity, theo ID) | EF Core `AsNoTracking()` |
| **Đọc phức tạp** (nhiều bảng, JOIN, filter phức tạp) | Dapper + **Stored Procedure** |

---

## DO

1. **Mỗi API endpoint** sinh đúng bộ 3: `{UseCase}Command` / `{UseCase}Query` + Handler + Validator.
   Đặt trong `Features/{UseCase}/`: `Command.cs`, `CommandHandler.cs`, `CommandValidator.cs`.

2. **Write path** dùng **EF Core** với Change Tracking (không `AsNoTracking`).

3. **Read path đơn giản** dùng EF Core `AsNoTracking()` — không cần Dapper cho query đơn bảng.

4. **Read path phức tạp** dùng **Dapper + Stored Procedure**, trả thẳng DTO — không đi qua Domain entity.

5. **Đăng ký** `ValidationBehavior<TRequest, TResponse>` và `LoggingBehavior<TRequest, TResponse>` vào Mediator Pipeline.

6. **Command handler pattern:**
   ```
   validate (qua pipeline) → load aggregate → gọi domain method → persist → raise domain events
   ```

7. **Query handler pattern:**
   ```
   Read đơn giản: EF Core AsNoTracking() → map ToResponse()
   Read phức tạp: IDbConnection → gọi Stored Procedure → map DTO → trả về
   ```

8. Đặt `CancellationToken` làm tham số cuối cùng trong mọi Handler.

9. **Phân trang chuẩn** cho list query:
   ```csharp
   public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
   ```

## DON'T

1. **KHÔNG** để Query handler gọi vào Repository của Domain để lấy data rồi map thủ công sang DTO.

2. **KHÔNG** để Command handler viết SQL thô (Dapper) để thực hiện ghi dữ liệu. Luôn dùng EF Core.

3. **KHÔNG** dùng EF Core `Include()` chain dài trong Query handler — chuyển sang Dapper + Stored Procedure.

4. **KHÔNG** viết SQL thuần trong code C#. Dùng Stored Procedure cho read path phức tạp.

5. **KHÔNG** đặt business validation (ví dụ: "văn bản đã phát hành không được sửa") trong FluentValidation Validator.
   Validator chỉ validate format/required của input DTO. Business rule thuộc về Domain entity.

6. **KHÔNG** chia sẻ chung 1 Handler giữa 2 use case khác nhau dù input tương tự.

7. **KHÔNG** dùng MediatR (Jimmy Bogard) — dùng Mediator (Arch.Ext).

## Ví dụ minh họa

```csharp
// ── Application/Abstractions/Messaging/ICommand.cs
namespace {Namespace}.Application.Abstractions.Messaging;

public interface ICommand;
public interface ICommand<out TResponse>;
public interface IQuery<TResponse>;
```

```csharp
// ── Command ── Application layer (Write: EF Core)
using {Namespace}.Application.Abstractions.Messaging;
using {Namespace}.Domain.Common;

public sealed record CreateDocumentCommand(string Title, string Content, Guid IssuingOfficeId)
    : ICommand<Result<Guid>>;

public class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.IssuingOfficeId).NotEmpty();
        // KHÔNG validate business rule ở đây
    }
}

internal sealed class CreateDocumentCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : ICommandHandler<CreateDocumentCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(
        CreateDocumentCommand command,
        CancellationToken cancellationToken)
    {
        var createResult = Document.Create(
            DocumentTitle.Create(command.Title),
            tenantContext.TenantId);
        if (createResult.IsFailure)
            return Result<Guid>.Failure(createResult.Error);

        var doc = createResult.Value;
        dbContext.Documents.Add(doc);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(doc.Id);
    }
}
```

```csharp
// ── Query ── Application layer (Read phức tạp: Dapper + Stored Procedure)
using {Namespace}.Application.Abstractions.Messaging;
using {Namespace}.Domain.Common;
using Dapper;

public sealed record GetDocumentListQuery(int Page, int PageSize, string? Keyword)
    : IQuery<Result<PagedResult<DocumentSummaryDto>>>;

internal sealed class GetDocumentListQueryHandler(
    IDbConnection dbConnection,
    ITenantContext tenantContext)
    : IQueryHandler<GetDocumentListQuery, PagedResult<DocumentSummaryDto>>
{
    public async ValueTask<Result<PagedResult<DocumentSummaryDto>>> Handle(
        GetDocumentListQuery q, CancellationToken ct)
    {
        var parameters = new
        {
            tenantContext.TenantId,
            q.Keyword,
            q.PageSize,
            Offset = (q.Page - 1) * q.PageSize
        };

        using var multi = await dbConnection.QueryMultipleAsync(
            "sp_Documents_List",
            parameters,
            commandType: CommandType.StoredProcedure);

        var rows = (await multi.ReadAsync<DocumentSummaryDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new PagedResult<DocumentSummaryDto>(rows, totalCount, q.Page, q.PageSize);
    }
}
```

```csharp
// ── Query ── Application layer (Read đơn giản: EF Core)
internal sealed class GetDocumentByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetDocumentByIdQuery, Result<GetDocumentByIdResponse>>
{
    public async ValueTask<Result<GetDocumentByIdResponse>> Handle(
        GetDocumentByIdQuery query, CancellationToken ct)
    {
        var doc = await dbContext.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == new DocumentId(query.Id), ct);

        if (doc is null)
            return DocumentErrors.NotFound;

        return doc.ToResponse();
    }
}
```
