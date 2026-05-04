# 07 – Testing Strategy Rules

**Nguồn:** `backend_core_technical_guidelines.md` §4.6

---

## DO

1. **Unit Test** bao phủ:
   - Domain entity methods và invariants
   - Value Object creation và validation
   - Domain Event raising
   - FluentValidation validators
   Không cần mock DB — Domain không phụ thuộc DB.

2. **Integration Test** chạy với PostgreSQL thật (Testcontainers-dotnet):
   ```csharp
   var postgres = new PostgreSqlBuilder()
       .WithImage("postgres:16")
       .Build();
   await postgres.StartAsync();
   ```

3. **Architecture Test** trong project riêng (`ArchitectureTests`), dùng `NetArchTest` hoặc `ArchUnitNET`:
   ```csharp
   var result = Types.InAssembly(DomainAssembly)
       .ShouldNot().HaveDependencyOn("Infrastructure")
       .GetResult();
   Assert.True(result.IsSuccessful);
   ```

4. **Contract Test** cho event: verify rằng event schema publish bởi producer khớp với consumer's expectation. Dùng Pact hoặc JSON schema snapshot test.

5. **Đặt test theo cấu trúc song song với production code:**
   ```
   tests/
     Unit/
       Domain/
       Application/
     Integration/
     Architecture/
     Contract/
   ```

6. **Mỗi Integration Test** phải clean up DB sau khi chạy:
   - Dùng transaction rollback: `await using var tx = await db.BeginTransactionAsync()` → `await tx.RollbackAsync()`
   - Hoặc reset container sau mỗi test class.

## DON'T

1. **KHÔNG** dùng `UseInMemoryDatabase()` cho Integration Test.
   InMemory provider không hỗ trợ transaction, constraint, JSON column, raw SQL.

2. **KHÔNG** mock `DbContext` trong Integration Test — dùng DB thật.

3. **KHÔNG** đặt business logic test vào Integration Test — logic đó thuộc Unit Test Domain.

4. **KHÔNG** bỏ qua Architecture Test vì "mất thời gian" — đây là safety net quan trọng nhất cho Clean Architecture.

5. **KHÔNG** chia sẻ state giữa các test:
   - Không shared static fields
   - Không shared DB rows không cleanup

## Ví dụ minh họa

```csharp
// ── Architecture Test
[Fact]
public void Domain_Should_Not_Reference_Infrastructure()
{
    var result = Types.InAssembly(typeof(Document).Assembly)
        .ShouldNot()
        .HaveDependencyOn("Infrastructure")
        .GetResult();

    Assert.True(result.IsSuccessful,
        string.Join("\n",
            result.FailingTypes?.Select(t => t.FullName) ?? Enumerable.Empty<string>()));
}

// ── Unit Test — Domain logic không cần DB
[Fact]
public void Document_Publish_Should_Raise_DocumentPublishedEvent()
{
    // Arrange
    var tenantId = TenantId.New();
    var doc = Document.Create(DocumentTitle.Create("Test Doc"), tenantId);

    // Act
    doc.Publish(publishedBy: UserId.New());

    // Assert
    var publishedEvent = Assert.Single(doc.DomainEvents.OfType<DocumentPublishedEvent>());
    Assert.Equal(doc.Id, publishedEvent.DocumentId);
    Assert.Equal(tenantId, publishedEvent.TenantId);
}

// ── Integration Test — dùng Testcontainers
public class CreateDocumentTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgres = null!;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task CreateDocument_Should_Persist_To_Database()
    {
        await using var tx = await _db.BeginTransactionAsync();
        // ... test logic ...
    }
}
```
