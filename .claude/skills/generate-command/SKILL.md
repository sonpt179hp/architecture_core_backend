---
name: generate-command
description: >
  Scaffold a complete CQRS Command use case: Command record, CommandHandler,
  FluentValidation Validator, and Controller action.
  Use when the user asks to create a write operation (create, update, delete, publish, approve).
allowed-tools:
  - Read(**/*.cs)
  - Read(**/ai-rules/*.md)
  - Glob(src/**/*.cs)
  - Glob(src/**/*.csproj)
  - Edit(**/*.cs)
---

# Skill: Generate CQRS Command

## Purpose

Scaffold đầy đủ bộ file cho một write use case mới theo đúng CQRS convention của project.
Đảm bảo tuân thủ `ai-rules/02-cqrs-pattern.md` và `ai-rules/01-clean-architecture.md`.

## Instructions

**Input cần từ user:** Tên use case (ví dụ: `PublishDocument`), bounded context (ví dụ: `Documents`), tên Aggregate (ví dụ: `Document`), return type (thường là `Guid` hoặc `void`/`Unit`).

1. **Xác định đường dẫn target** bằng cách đọc cấu trúc thư mục hiện tại:
   - Command files: `src/{BoundedContext}/Application/Features/{UseCase}/`
   - Controller: `src/{BoundedContext}/Api/Controllers/`

2. **Tạo `{UseCase}Command.cs`** tại `Features/{UseCase}/`:
   - Dùng `record`, không phải `class`
   - Implement `IRequest<{ReturnType}>` từ MediatR
   - Chỉ chứa input data (không có method, không có business logic)
   - Đặt namespace khớp với thư mục

3. **Tạo `{UseCase}CommandValidator.cs`** tại cùng thư mục:
   - Kế thừa `AbstractValidator<{UseCase}Command>`
   - Chỉ validate format/required (NotEmpty, MaximumLength, GuidNotEmpty, v.v.)
   - **KHÔNG** đặt business rule (xem `ai-rules/02-cqrs-pattern.md` §DON'T #4)

4. **Tạo `{UseCase}CommandHandler.cs`** tại cùng thư mục:
   - Kế thừa `IRequestHandler<{UseCase}Command, {ReturnType}>`
   - Inject: `I{Aggregate}Repository`, `IUnitOfWork`, `ICurrentUser`, `ITenantContext`
   - Pattern trong `Handle()`:
     ```
     load aggregate → call domain method → SaveChangesAsync → return result
     ```
   - Đặt `CancellationToken ct` làm tham số cuối

5. **Cập nhật Controller**: Thêm action method mới:
   - `[HttpPost]` cho create, `[HttpPut("{id:guid}")]` cho update, `[HttpDelete("{id:guid}")]` cho delete
   - Route theo `/api/v{version:apiVersion}/[controller]`
   - Thêm `[FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey` nếu là POST tạo mới
   - Trả `CreatedAtAction` (201) cho POST, `NoContent()` (204) cho PUT/DELETE
   - Truyền `HttpContext.RequestAborted` làm CancellationToken
   - Thêm `[Authorize(Policy = "{resource}:{action}")]`

6. **Kiểm tra trước khi hoàn thành:**
   - Namespace khớp cấu trúc thư mục
   - Không import Infrastructure namespace trong Application layer
   - CancellationToken được truyền qua toàn bộ async call chain
   - Validator đăng ký tự động qua `AddValidatorsFromAssembly` (không cần đăng ký thủ công)

## Edge Cases

- Nếu thư mục `Features/{UseCase}/` đã tồn tại: hỏi user trước khi overwrite bất kỳ file nào.
- Nếu aggregate chưa có Repository interface (`I{Aggregate}Repository`): nhắc user tạo interface trước, đề nghị dùng skill `generate-domain-entity`.
- Nếu use case là bulk operation trên nhiều bản ghi: nhắc dùng `ExecuteUpdateAsync`/`ExecuteDeleteAsync` theo `ai-rules/08-efcore.md` §DO #5.
- Nếu aggregate chưa tồn tại: dừng và đề nghị user chạy skill `generate-domain-entity` trước.

## References

- `ai-rules/01-clean-architecture.md` — dependency rule, layer separation
- `ai-rules/02-cqrs-pattern.md` — CQRS write path, validator responsibilities
- `ai-rules/03-security-tenancy.md` — ITenantContext, policy-based authorization
- `ai-rules/04-api-contract.md` — HTTP status codes, Idempotency-Key, ProblemDetails
- `ai-rules/08-efcore.md` — transaction boundary, persistence concerns
