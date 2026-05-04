# 04 – API Contract Rules

**Nguồn:** `backend_core_technical_guidelines.md` §4.3

---

## DO

1. **Prefix tất cả route** bằng `/api/v{version}/`:
   ```csharp
   [ApiController]
   [Route("api/v{version:apiVersion}/[controller]")]
   ```

2. **Trả `ProblemDetails`** (RFC 7807) cho mọi response lỗi:
   ```json
   {
     "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
     "title": "Validation Failed",
     "status": 422,
     "errors": { "Title": ["Title is required"] },
     "traceId": "00-abc123-def456-00"
   }
   ```

3. **Chuẩn hóa pagination request:**
   ```csharp
   public record PaginationQuery(int Page = 1, int PageSize = 20);
   ```

   **Chuẩn hóa pagination response:**
   ```csharp
   public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
   ```

4. **Đặt `Idempotency-Key` header** cho command quan trọng (tạo văn bản, phê duyệt, phát hành):
   ```csharp
   [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey
   ```

5. Sử dụng `CancellationToken` từ `HttpContext.RequestAborted` ở mọi controller action.

6. Trả `201 Created` với `Location` header cho POST tạo resource mới.

7. **Đặt API version trong URL** (`/api/v1/`) — không dùng query param hay header versioning.

## DON'T

1. **KHÔNG** trả response lỗi theo custom format riêng (ví dụ: `{ "error": "...", "code": 99 }`).
   Phải dùng `ProblemDetails` hoặc `ValidationProblemDetails`.

2. **KHÔNG** bỏ qua `totalCount` trong phản hồi phân trang.

3. **KHÔNG** dùng `200 OK` cho mọi response — phân biệt:
   - `201 Created` — POST tạo mới
   - `204 NoContent` — PUT/DELETE thành công
   - `200 OK` — GET thành công
   - `404 NotFound` — resource không tồn tại

4. **KHÔNG** đặt business data (TenantId, internal IDs) trực tiếp trong URL path nếu có thể lấy từ token.

5. **KHÔNG** cho phép client truyền `pageSize` không giới hạn:
   ```csharp
   RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
   ```

## Ví dụ minh họa

```csharp
// ── Controller
[HttpPost]
[Authorize(Policy = "document:create")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
[ProducesResponseType(StatusCodes.Status201Created)]
public async Task<IActionResult> Create(
    [FromBody] CreateDocumentCommand command,
    [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
    CancellationToken ct)
{
    var id = await _mediator.Send(command, ct);
    return CreatedAtAction(
        nameof(GetById),
        new { id, version = "1" },
        new { id });
}

// ── Pagination validator
public class GetDocumentListQueryValidator : AbstractValidator<GetDocumentListQuery>
{
    public GetDocumentListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).DefaultSeverity = Severity.Warning;
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
```
