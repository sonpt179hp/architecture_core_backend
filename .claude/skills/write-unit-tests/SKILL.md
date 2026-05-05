---
name: write-unit-tests
description: >
  Scaffold xUnit unit tests for Domain entities, Value Objects, FluentValidation validators,
  and CQRS CommandHandlers/QueryHandlers (with mocked dependencies).
  Use when the user asks to write, generate, or add unit tests for a specific class or use case.
allowed-tools:
  - Read(**/*.cs)
  - Read(**/ai-rules/*.md)
  - Glob(src/**/*.cs)
  - Glob(tests/**/*.cs)
  - Glob(src/**/*.csproj)
  - Edit(**/*.cs)
---

# Skill: Write Unit Tests

## Purpose

Scaffold đầy đủ unit test theo đúng convention xUnit + FluentAssertions của project.
Domain tests không cần mock DB — chạy hoàn toàn in-memory.
Handler tests dùng mock repository/unit-of-work (NSubstitute hoặc Moq).
Tuân thủ `ai-rules/07-testing.md` §DO #1 và §DON'T #3.

## Instructions

**Input cần từ user:** Tên class cần test (ví dụ: `Document`, `CreateDocumentCommandHandler`, `CreateDocumentCommandValidator`), bounded context.

1. **Xác định loại test cần viết** dựa trên class target:
   - `AggregateRoot` / `ValueObject` / `DomainException` → **Domain Unit Test** (không mock gì)
   - `CommandHandler` / `QueryHandler` → **Handler Unit Test** (mock repository, unit of work, current user)
   - `AbstractValidator<T>` → **Validator Unit Test** (không mock gì)

2. **Xác định đường dẫn đặt test:**
   ```
   tests/
     {BoundedContext}.UnitTests/
       Domain/
         {AggregateName}Tests.cs          ← Domain entity tests
         ValueObjects/{ValueObjectName}Tests.cs
       Application/
         {UseCase}/{UseCaseName}CommandHandlerTests.cs
         {UseCase}/{UseCaseName}ValidatorTests.cs
   ```

3. **Template: Domain Aggregate Test** — không mock, test behavior trực tiếp:
   ```csharp
   public class {AggregateName}Tests
   {
       [Fact]
       public void {MethodName}_Should_Raise_{EventName}_When_{Condition}()
       {
           // Arrange
           var tenantId = TenantId.Create(Guid.NewGuid());
           var entity = {AggregateName}.Create(/* valid params */);

           // Act
           entity.{MethodName}(/* params */);

           // Assert
           var evt = Assert.Single(entity.DomainEvents.OfType<{EventName}>());
           Assert.Equal(entity.Id, evt.{EntityId});
       }

       [Fact]
       public void {MethodName}_Should_Throw_DomainException_When_{InvalidCondition}()
       {
           // Arrange
           var entity = {AggregateName}.Create(/* valid params */);

           // Act & Assert
           Assert.Throws<{SpecificDomainException}>(
               () => entity.{MethodName}(/* invalid params */));
       }
   }
   ```

4. **Template: Value Object Test** — test Create factory và equality:
   ```csharp
   public class {ValueObjectName}Tests
   {
       [Theory]
       [InlineData("valid-value-1")]
       [InlineData("valid-value-2")]
       public void Create_Should_Succeed_With_Valid_Value(string value)
       {
           var vo = {ValueObjectName}.Create(value);
           Assert.Equal(value, vo.Value);
       }

       [Theory]
       [InlineData("")]
       [InlineData(null)]
       [InlineData("x-too-long-string")]
       public void Create_Should_Throw_DomainException_With_Invalid_Value(string? value)
       {
           Assert.Throws<DomainException>(() => {ValueObjectName}.Create(value!));
       }

       [Fact]
       public void Two_ValueObjects_With_Same_Value_Should_Be_Equal()
       {
           var a = {ValueObjectName}.Create("test");
           var b = {ValueObjectName}.Create("test");
           Assert.Equal(a, b);
       }
   }
   ```

5. **Template: Validator Test** — test FluentValidation rules:
   ```csharp
   public class {UseCaseName}CommandValidatorTests
   {
       private readonly {UseCaseName}CommandValidator _sut = new();

       [Fact]
       public async Task Should_Pass_Validation_When_All_Fields_Are_Valid()
       {
           var command = new {UseCaseName}Command(/* valid params */);
           var result = await _sut.ValidateAsync(command);
           Assert.True(result.IsValid);
       }

       [Theory]
       [InlineData(null)]
       [InlineData("")]
       public async Task Should_Fail_Validation_When_{Field}_Is_Empty(string? value)
       {
           var command = new {UseCaseName}Command(/* invalid field */ value!);
           var result = await _sut.ValidateAsync(command);
           Assert.False(result.IsValid);
           Assert.Contains(result.Errors, e => e.PropertyName == nameof({UseCaseName}Command.{Field}));
       }
   }
   ```

6. **Template: CommandHandler Test** — mock repository + unit of work:
   ```csharp
   public class {UseCaseName}CommandHandlerTests
   {
       private readonly I{Aggregate}Repository _repository;
       private readonly IUnitOfWork _unitOfWork;
       private readonly ICurrentUser _currentUser;
       private readonly {UseCaseName}CommandHandler _sut;

       public {UseCaseName}CommandHandlerTests()
       {
           _repository  = Substitute.For<I{Aggregate}Repository>();    // NSubstitute
           _unitOfWork  = Substitute.For<IUnitOfWork>();
           _currentUser = Substitute.For<ICurrentUser>();
           _currentUser.UserId.Returns(Guid.NewGuid());

           _sut = new {UseCaseName}CommandHandler(_repository, _unitOfWork, _currentUser);
       }

       [Fact]
       public async Task Handle_Should_Return_{Result}_When_Input_Is_Valid()
       {
           // Arrange
           var command = new {UseCaseName}Command(/* valid params */);
           // setup mocks if needed
           _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
               .Returns({AggregateName}.Create(/* ... */));

           // Act
           var result = await _sut.Handle(command, CancellationToken.None);

           // Assert
           Assert.NotNull(result);
           await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
       }

       [Fact]
       public async Task Handle_Should_Throw_NotFoundException_When_{Aggregate}_Not_Found()
       {
           _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
               .Returns((I{Aggregate}?) null);

           var command = new {UseCaseName}Command(Guid.NewGuid());

           await Assert.ThrowsAsync<NotFoundException>(
               () => _sut.Handle(command, CancellationToken.None));
       }
   }
   ```

7. **Kiểm tra danh sách package trong `.csproj` của test project:**
   - `xunit`, `xunit.runner.visualstudio`
   - `FluentAssertions` (nếu project dùng)
   - `NSubstitute` hoặc `Moq` (nhất quán với project hiện tại)
   - `coverlet.collector` (code coverage)
   - Không cần `Microsoft.EntityFrameworkCore.InMemory` — unit test không cần DB

8. **Đặt tên test theo convention AAA + Given/When/Then:**
   - `{Method}_Should_{ExpectedResult}_When_{Condition}()`
   - Không dùng tên chung chung: `TestCreate`, `Test1`

9. **Kiểm tra lại trước khi hoàn thành:**
   - Domain tests không import `Infrastructure`, `EF Core`, `Dapper`
   - Handler tests mock hoàn toàn external dependencies
   - Mỗi test chỉ assert một behavior duy nhất
   - Test names mô tả rõ scenario

## Edge Cases

- Nếu project dùng `Moq` thay `NSubstitute`: thay `Substitute.For<T>()` bằng `new Mock<T>()` và `.Object`, `.Setup()`, `.Verify()`.
- Nếu Aggregate có nhiều behaviors: tạo nested `public class` bên trong test class để nhóm theo method (ví dụ: `public class WhenPublishing { ... }`).
- Nếu ValueObject dùng implicit operator: test thêm conversion round-trip.
- Nếu Handler dùng `ITenantContext`: mock và set `TenantId` hợp lệ giống `ICurrentUser`.
- Nếu domain method có nhiều nhánh (guard clauses): viết riêng test case cho từng nhánh, đặt `[Theory]` với `[InlineData]`.
- Nếu test project chưa tồn tại: tạo `.csproj` tham chiếu đúng project và thêm vào solution.

## References

- `ai-rules/07-testing.md` — Testing strategy, unit/integration boundary
- `ai-rules/02-cqrs-pattern.md` — Command/Query handler structure
- `ai-rules/01-clean-architecture.md` — Không import Infrastructure trong Domain test
