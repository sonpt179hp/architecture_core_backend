---
name: architecture-reference
description: >-
  Comprehensive CQRS/DDD architecture reference for a .NET 8 Clean Architecture solution
  using Mediator, EF Core, PostgreSQL, Redis, and MassTransit. Use when scaffolding
  new domain entities, CQRS commands/queries, API controllers, infrastructure layers
  (caching, DI, error handling, background jobs, messaging), or when setting up testing
  and MCP SQL Server integration for this codebase.
---

# Architecture Reference: .NET 8 CQRS/DDD Solution

This skill provides a complete skill ecosystem for building a .NET 8 Clean Architecture
application with CQRS, DDD, and event-driven messaging.

## Skill Map

Use these skills in the order that matches your workflow:

| Skill | When to Use |
|-------|-------------|
| [setup-mcp-sqlserver](setup-mcp-sqlserver/SKILL.md) | Configure MCP SQL Server for schema discovery and code generation |
| [setup-dependency-injection](setup-dependency-injection/SKILL.md) | Set up DI registration (Mediator, Validators, DbContext, Redis) |
| [setup-error-handling](setup-error-handling/SKILL.md) | Set up Result pattern, GlobalExceptionHandler, ProblemDetails |
| [setup-configuration](setup-configuration/SKILL.md) | Configure options pattern with validation |
| [generate-domain-entity](generate-domain-entity/SKILL.md) | Scaffold a new DDD Aggregate Root with ID, errors, EF config |
| [generate-command](generate-command/SKILL.md) | Scaffold a CQRS write command (create, update, delete) |
| [add-update-command](add-update-command/SKILL.md) | Add an update command to an existing aggregate |
| [add-delete-command](add-delete-command/SKILL.md) | Add a delete command (soft or hard delete) |
| [generate-query](generate-query/SKILL.md) | Scaffold a CQRS read query (EF Core or Dapper + Stored Procedure) |
| [setup-API](setup-API/SKILL.md) | Set up API layer: Program.cs, Controllers, Swagger, middleware |
| [setup-caching](setup-caching/SKILL.md) | Set up Redis caching with cache-aside pattern |
| [setup-background-job](setup-background-job/SKILL.md) | Set up Outbox pattern for reliable event publishing |
| [add-event-handler](add-event-handler/SKILL.md) | Scaffold MassTransit consumer for integration events |
| [setup-testing](setup-testing/SKILL.md) | Set up Unit Tests, Integration Tests with Testcontainers |

## Key Conventions

### Technology Stack
- **.NET 8** with C# 12 primary constructors
- **Mediator** (Arch.Ext) — `ISender`, `ICommandHandler`, `IQueryHandler`
- **EF Core** with PostgreSQL (Npgsql)
- **Dapper + Stored Procedure** — read path phức tạp
- **Redis** for caching (StackExchange.Redis)
- **MassTransit** for messaging
- **FluentValidation** for input validation
- **Result pattern** (`Result<T>`, `Error`, `ErrorType`) for error handling
- **xUnit** + **FluentAssertions** + **NSubstitute** for testing

### Dependency Rule
`Presentation → Application → Domain ← Infrastructure`

### Naming Conventions
- **Namespace base**: `{Namespace}.{Layer}.{Feature}`
- **Aggregate**: `Product`, `Document` — PascalCase, singular
- **DbSet**: `Products`, `Documents` — PascalCase, plural
- **Table name**: snake_case (`products`, `documents`)
- **Column name**: snake_case (`created_at`, `product_name`)
- **ID type**: `{EntityName}Id` — `readonly record struct`
- **Commands**: `{UseCase}Command` — `record`
- **Queries**: `{QueryName}Query` — `record`
- **Handlers**: `{Name}CommandHandler` / `{Name}QueryHandler`
- **Validators**: `{Name}CommandValidator`
- **Errors**: `{Entity}Errors` — static class with `Error` constants
- **Cache keys**: `tenant:{tenantId}:{entity}:{id}` — lowercase with colons

### CQRS Conventions
| Thao tác | Cách thực hiện |
|---|---|
| **Tạo mới (Create)** | EF Core |
| **Cập nhật (Update)** | EF Core |
| **Xóa (Delete)** | EF Core |
| **Đọc đơn giản** (1 entity) | EF Core `AsNoTracking()` |
| **Đọc phức tạp** (nhiều bảng, JOIN) | Dapper + **Stored Procedure** |

> **KHÔNG viết SQL thuần trong code C#.** Dùng Stored Procedure cho read path phức tạp.

### Project Conventions
- **Reference only adjacent layers** — Domain references nothing; Application references Domain; Infrastructure references Application + Domain; Api references all
- **Domain has zero infrastructure dependencies** — no EF Core, no MassTransit, no Redis imports
- **Primary constructors everywhere** — C# 12 `class X(IParam p)` syntax
- **ValueTask for handlers** — `async ValueTask<Result<T>>` not `Task<Result<T>>`
- **AsNoTracking for reads** — all query handlers use `.AsNoTracking()` (EF Core path)
- **Global query filters** — soft delete via `HasQueryFilter(e => !e.IsDeleted)`
- **Outbox pattern** — domain events stored in `outbox_messages` table, processed by `OutboxProcessor` BackgroundService
