---
name: setup-error-handling
description: >
  Scaffold the complete error handling infrastructure: custom exception hierarchy,
  global exception handler (IExceptionHandler), ProblemDetails factory, and
  exception-to-HTTP status mapping. Use once per bounded context or at solution level.
allowed-tools:
  - Read(**/*.cs)
  - Read(**/ai-rules/*.md)
  - Glob(src/**/*.cs)
  - Glob(src/**/*.csproj)
  - Edit(**/*.cs)
---

# Skill: Setup Error Handling Infrastructure

## Purpose

Dựng đầy đủ hạ tầng xử lý lỗi nhất quán cho toàn solution: exception hierarchy theo layer,
global handler trả `ProblemDetails`, mapping exception → HTTP status code chuẩn.
Không để mỗi controller tự try/catch riêng.

## Instructions

**Input:** Tên solution hoặc bounded context muốn setup (ví dụ: `Documents`).

1. **Đọc cấu trúc project hiện tại** để xác định đường dẫn đúng:
   - `Domain/Exceptions/` — DomainException
   - `Application/Exceptions/` — NotFoundException, ConflictException
   - `Infrastructure/Exceptions/` — InfrastructureException
   - `Api/Middleware/` hoặc `Api/Infrastructure/` — GlobalExceptionHandler

2. **Tạo base exception classes:**

   `Domain/Exceptions/DomainException.cs`:
   ```csharp
   public abstract class DomainException : Exception
   {
       protected DomainException(string message) : base(message) { }
       protected DomainException(string message, Exception inner) : base(message, inner) { }
   }
   ```

   `Application/Exceptions/NotFoundException.cs`:
   ```csharp
   public class NotFoundException : Exception
   {
       public NotFoundException(string entityName, object id)
           : base($"{entityName} with id '{id}' was not found.") { }
   }
   ```

   `Application/Exceptions/ConflictException.cs`:
   ```csharp
   public class ConflictException : Exception
   {
       public ConflictException(string message) : base(message) { }
       public ConflictException(string message, Exception inner) : base(message, inner) { }
   }
   ```

   `Infrastructure/Exceptions/InfrastructureException.cs`:
   ```csharp
   public class InfrastructureException : Exception
   {
       public InfrastructureException(string message, Exception inner) : base(message, inner) { }
   }
   ```

3. **Tạo Global Exception Handler** tại `Api/Infrastructure/GlobalExceptionHandler.cs`:
   - Implement `IExceptionHandler` (.NET 8)
   - Map exception types → HTTP status codes theo bảng trong `ai-rules/09-error-handling.md`
   - Log với full context (TraceId, Method, Path, ExceptionType)
   - Không expose stack trace ở production
   - Handle `OperationCanceledException` riêng (return false, không log Error)
   - Handle `ValidationException` (FluentValidation) → 422 với field errors

4. **Đăng ký trong Program.cs hoặc extension method:**
   ```csharp
   builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
   builder.Services.AddProblemDetails();
   // ...
   app.UseExceptionHandler();
   ```

5. **Tạo 1-2 ví dụ DomainException cụ thể** phù hợp với bounded context:
   ```csharp
   // Ví dụ cho Documents context
   public class DocumentAlreadyPublishedException : DomainException
   {
       public DocumentAlreadyPublishedException(Guid documentId)
           : base($"Document '{documentId}' is already published.") { }
   }
   ```

6. **Kiểm tra lại:**
   - `GlobalExceptionHandler` đã đăng ký trước middleware khác trong pipeline chưa
   - `OperationCanceledException` không bị log như Error
   - Stack trace không hiển thị ở production (kiểm tra `IHostEnvironment`)
   - `ValidationException` của FluentValidation map sang 422 với danh sách lỗi theo field

## Edge Cases

- Nếu `IExceptionHandler` chưa có (< .NET 8): dùng `UseMiddleware<GlobalExceptionMiddleware>()` thay thế.
- Nếu cần custom mapping cho exception bên thứ ba (Stripe, Twilio...): thêm case vào switch expression trong handler.
- Nếu có nhiều bounded context: base exception classes (DomainException, NotFoundException) nên đặt ở shared project, không nhân bản.

## References

- `ai-rules/09-error-handling.md`
- `ai-rules/04-api-contract.md`
- `ai-rules/06-observability.md`
- `ai-rules/10-dependency-injection.md`
