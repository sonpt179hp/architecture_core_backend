---
name: generate-query
description: >
  Scaffold a complete CQRS Query use case: Query record, Dapper-based QueryHandler,
  response DTO, and Controller GET action.
  Use when the user asks to create a read operation (list, get-by-id, search, export).
allowed-tools:
  - Read(**/*.cs)
  - Read(**/ai-rules/*.md)
  - Glob(src/**/*.cs)
  - Glob(src/**/*.csproj)
  - Edit(**/*.cs)
---

# Skill: Generate CQRS Query

## Purpose

Scaffold đầy đủ bộ file cho một read use case mới theo CQRS read-path convention của project.
Read path dùng Dapper + SQL thuần, trả thẳng DTO — không đi qua EF Core hay Domain entity.
Tham chiếu `ai-rules/02-cqrs-pattern.md` §DO #3 và §DON'T #1/#3.

## Instructions

**Input cần từ user:** Tên query (ví dụ: `GetDocumentList`, `GetDocumentById`), bounded context, loại query (list có phân trang hay single item), các filter parameters.

1. **Xác định đường dẫn target:**
   - Query files: `src/{BoundedContext}/Application/Features/{QueryName}/`
   - DTO: `src/{BoundedContext}/Application/Features/{QueryName}/{ResultName}Dto.cs`
   - Controller: `src/{BoundedContext}/Api/Controllers/`

2. **Tạo DTO** tại `Features/{QueryName}/{ResultName}Dto.cs`:
   - Dùng `record` để đảm bảo immutability
   - Chỉ chứa các field cần thiết cho UI/client
   - Không expose internal domain fields không cần thiết
   - Nếu là list: tên `*{Summary,Item,Dto}Dto.cs`
   - Nếu là single: tên `*{Detail}Dto.cs`

3. **Tạo Query record** tại `Features/{QueryName}/{QueryName}.cs`:
   - Nếu là list: embed `int Page, int PageSize` hoặc inherit `PaginationQuery`
   - Return type: `PagedResult<{Dto}>` cho list, `{Dto}?` cho single item
   - Thêm filter parameters (Keyword, Status, DateRange, v.v.)
   - Implement `IRequest<{ReturnType}>`

4. **Tạo QueryHandler** tại `Features/{QueryName}/{QueryName}Handler.cs`:
   - Inject `IDbConnection` (Dapper) và `ITenantContext`
   - Viết SQL query với các điều kiện bắt buộc:
     - `WHERE tenant_id = @TenantId` (bắt buộc, xem `ai-rules/03-security-tenancy.md`)
     - `LIMIT @PageSize OFFSET @Offset` cho phân trang
     - `COUNT(*) OVER()` hoặc separate count query cho `totalCount`
     - `ORDER BY` (list query không bao giờ trả unordered)
   - **KHÔNG** dùng EF Core, **KHÔNG** gọi Repository
   - Map kết quả SQL sang DTO bằng Dapper
   - Đặt `CancellationToken ct` làm tham số cuối

5. **Tạo Validator** tại `Features/{QueryName}/{QueryName}Validator.cs`:
   - Validate pagination: `Page >= 1`, `PageSize ∈ [1, 100]`
   - Validate filter parameters (Email format, DateRange logical, v.v.)

6. **Cập nhật Controller**: Thêm GET action:
   - `[HttpGet]` cho list, `[HttpGet("{id:guid}")]` cho single
   - `[FromQuery]` cho pagination/filter params
   - Trả `200 OK` với `PagedResult<T>` hoặc `T`
   - Trả `404 NotFound` (ProblemDetails) nếu single item không tồn tại
   - Truyền `HttpContext.RequestAborted` làm CancellationToken

7. **Kiểm tra SQL trước khi hoàn thành:**
   - Có `WHERE tenant_id = @TenantId` (KHÔNG BAO GIỜ thiếu)
   - Có `ORDER BY` cho list query
   - PageSize có giới hạn max trong Validator

## Edge Cases

- Nếu query cần JOIN phức tạp (>3 bảng): gợi ý tạo Database View trước, handler query từ View.
- Nếu cần full-text search: gợi ý dùng `ILIKE` (simple) hoặc PostgreSQL `tsvector` (advanced).
- Nếu là export (CSV/Excel): nhắc user về timeout, recommend dùng background job thay vì sync response.
- Nếu cần search cross-tenant (admin): nhắc dùng `[RequireAdminScope]` và audit log, không dùng skill này.
- Nếu query trả về flat structure đơn giản (< 80 chars SQL): vẫn dùng Dapper, không cần View.

## References

- `ai-rules/02-cqrs-pattern.md` — read path dùng Dapper, không dùng EF cho read phức tạp
- `ai-rules/03-security-tenancy.md` — TenantId bắt buộc trong mọi SQL query
- `ai-rules/04-api-contract.md` — phân trang chuẩn, ProblemDetails 404, HTTP status codes
- `ai-rules/06-observability.md` — structured logging trong handler