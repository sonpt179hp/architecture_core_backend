---
name: generate-domain-entity
description: >
  Scaffold a complete DDD Aggregate Root: Value Objects, Domain Events,
  EF Core Fluent Configuration, Repository interface, and repository implementation.
  Use when the user asks to model a new core domain concept (not CRUD/master data).
allowed-tools:
  - Read(**/*.cs)
  - Read(**/ai-rules/*.md)
  - Glob(src/**/*.cs)
  - Glob(src/**/*.csproj)
  - Edit(**/*.cs)
---

# Skill: Generate Domain Entity (Aggregate Root)

## Purpose

Scaffold đầy đủ mô hình DDD cho một Aggregate Root mới theo Clean Architecture.
Domain layer phải hoàn toàn không phụ thuộc framework (không EF annotations, không MassTransit imports).
Áp dụng cho core services như Document, OfficeManagement; **không áp dụng** cho CRUD master-data đơn giản.

## Instructions

**Input cần từ user:** Tên aggregate (ví dụ: `Document`), bounded context, danh sách properties chính, danh sách behaviors nghiệp vụ cần model (`Publish`, `Approve`, `Archive`, v.v.).

1. **Xác định loại entity trước khi scaffold:**
   - Nếu là core business concept có invariants, lifecycle, behaviors → dùng Aggregate Root.
   - Nếu là master data/setting đơn giản → dừng và nhắc user dùng CRUD entity nhẹ hơn, không dùng skill này.

2. **Tạo Value Objects** tại `src/{BoundedContext}/Domain/ValueObjects/`:
   - Mỗi primitive quan trọng được wrap thành Value Object có validation riêng (`DocumentTitle`, `DocumentNumber`, `TenantId`).
   - Kế thừa `ValueObject` base class, override `GetEqualityComponents()`.
   - Dùng `static Create(...)` factory method thay vì public constructor.
   - `Create()` phải throw `DomainException` nếu input không hợp lệ.

3. **Tạo Domain Events** tại `src/{BoundedContext}/Domain/Events/`:
   - Mỗi behavior nghiệp vụ quan trọng sinh 1 event: `{Aggregate}{Action}Event`
   - Ví dụ: `DocumentCreatedEvent`, `DocumentPublishedEvent`
   - Implement `IDomainEvent`
   - Chỉ chứa snapshot data tại thời điểm event — không chứa method hoặc dependencies

4. **Tạo Aggregate Root** tại `src/{BoundedContext}/Domain/Aggregates/{Aggregate}.cs`:
   - Kế thừa `AggregateRoot` base class
   - Private constructor + `static Create(...)` factory
   - Mọi property có `private set`
   - Các method hành vi (`Publish()`, `Approve()`, v.v.) gọi `AddDomainEvent(new ...Event(...))`
   - Implement `ITenantEntity` nếu là multi-tenant
   - Thêm RowVersion/concurrency token property hoặc shadow property

5. **Tạo Repository Interface** tại `src/{BoundedContext}/Application/Interfaces/I{Aggregate}Repository.cs`:
   - Chỉ expose các method cần thiết: `GetByIdAsync`, `AddAsync`, `Update`, `Delete`
   - **KHÔNG** expose `IQueryable` hay `GetAllAsync` không filter

6. **Tạo EF Core Configuration** tại `src/{BoundedContext}/Infrastructure/Persistence/Configurations/{Aggregate}Configuration.cs`:
   - Implement `IEntityTypeConfiguration<{Aggregate}>`
   - Map Value Objects bằng `.HasConversion()`
   - Đặt `HasQueryFilter()` cho `TenantId` nếu multi-tenant
   - Đặt concurrency token: `IsRowVersion()` với `xmin` (PostgreSQL)
   - Tất cả column names theo `snake_case`

7. **Tạo Repository implementation** tại `src/{BoundedContext}/Infrastructure/Persistence/Repositories/{Aggregate}Repository.cs`:
   - Dùng DbContext tương ứng
   - Tuân thủ interface đã định nghĩa ở Application
   - Không để query read phức tạp trong repository này

8. **Kiểm tra lại trước khi hoàn thành:**
   - Domain layer không import bất cứ thứ gì từ Infrastructure, EF Core, Dapper, MassTransit
   - Tất cả mutable operations đi qua method, không set property trực tiếp từ bên ngoài
   - Factory method validate invariants trước khi tạo entity
   - Nếu multi-tenant: aggregate có `TenantId`, config có Global Query Filter

## Edge Cases

- Nếu aggregate có child entities nhưng không phải Aggregate Root riêng: tạo trong cùng file hoặc cùng namespace, dùng `private readonly List<ChildEntity> _children`.
- Nếu aggregate không cần multi-tenancy (shared data): bỏ `ITenantEntity` và `HasQueryFilter`, nhưng phải nêu rõ lý do.
- Nếu user yêu cầu scaffold cho Setting/MasterData: dừng và giải thích đây là over-engineering theo `ai-rules/01-clean-architecture.md`.
- Nếu aggregate có nhiều Value Objects nhưng chưa có base `ValueObject`: tạo base class trước hoặc reuse base hiện có.

## References

- `ai-rules/01-clean-architecture.md` — dependency rule, Domain thuần, không dùng EF annotations
- `ai-rules/03-security-tenancy.md` — ITenantEntity, Global Query Filter, không inject TenantContext vào Domain
- `ai-rules/08-efcore.md` — Fluent Configuration, concurrency token, transaction boundaries
- `ai-rules/07-testing.md` — cần unit test cho aggregate/value objects sau khi scaffold
