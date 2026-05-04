---
name: project-setup
description: >
  Interactive project setup, health check, and migration workflows for .NET 8 LTS.
  Guides developers through project initialization with Clean Architecture as the default,
  customized CLAUDE.md generation, codebase health analysis using MCP tools, and .NET version migration.
  Load when: "init project", "setup project", "new project", "health check",
  "analyze project", "project report", "migrate", "upgrade dotnet",
  "upgrade .NET", "generate CLAUDE.md".
---

# Project Setup & Workflows

## Core Principles

1. **Interactive over passive** — Don't dump a generic template. Ask questions, gather context, then generate a customized result tailored to the specific project.
2. **MCP-driven analysis** — Use Roslyn MCP tools for health checks and migration analysis instead of reading files manually. Token-efficient and semantically accurate.
3. **Generate, don't template** — CLAUDE.md files should be fully populated with specific choices (not `[PLACEHOLDER]` values). Every section must reflect the actual project decisions.
4. **Architecture-first: default to Clean Architecture** — Every workflow starts by understanding or selecting the project's architecture. For complex or multi-tenant domains, recommend Clean Architecture (4-layer: Domain, Application, Infrastructure, Api) as the default. Present alternatives only when the domain is clearly simple (< 5 entities, no business rules) or the project is a small solo API.
5. **Verify after action** — After any workflow completes (init, migration, health check), verify the result. Run builds, tests, or health checks to confirm success.

## Patterns

### Project Init Workflow

Interactive conversational flow for new projects. Execute steps in order, waiting for user input at each decision point.

**Step 1: Project Identity**
Ask:
- Project name (used for solution, namespaces, CLAUDE.md)
- Project type: API, Blazor, Worker Service, Class Library, or Modular Monolith
- Is this multi-tenant? (determines whether ITenantEntity/Global Query Filter pattern is needed)

**Step 2: Architecture Selection**

For most API projects, recommend **Clean Architecture** directly and explain why. Run the full questionnaire only if there is ambiguity:

| Signal | Recommendation | Rationale |
|--------|---------------|-----------|
| Multi-tenant domain with complex rules | **Clean Architecture** (default) | Layers enforce separation; Global Query Filter fits naturally in Infrastructure |
| Team of 3+ developers | **Clean Architecture** | Layer boundaries prevent stepping on each other |
| Simple CRUD-heavy API, 1-2 devs | VSA or minimal CA | Overhead not justified |
| Highly complex business domain | DDD + Clean Architecture | Aggregates + domain events needed |
| Multiple independent subsystems | Modular Monolith | Module-per-context with own DbContext |

**Step 3: Tech Stack Selection**

Ask about each dimension with a recommended default for this project stack:

| Dimension | Options | Default |
|-----------|---------|---------|
| Runtime | .NET 8 LTS | **.NET 8 LTS** |
| Database | PostgreSQL, SQL Server, SQLite | PostgreSQL |
| ORM (write) | EF Core 8 + Change Tracking | EF Core 8 |
| Read path | Dapper + raw SQL | **Dapper** |
| Auth | JWT Bearer, OIDC (Keycloak), None | JWT Bearer |
| Caching | IDistributedCache + Redis (Cache-Aside + Decorator) | **Redis (IDistributedCache)** |
| Messaging | MassTransit + RabbitMQ, None | **MassTransit + RabbitMQ** |
| Observability | Serilog + OpenTelemetry | **Serilog + OpenTelemetry** |
| Resilience | Polly v8 pipelines | **Polly v8** |
| API surface | Controllers with ApiController | **[ApiController] Controllers** |
| CQRS | MediatR | **MediatR** |

**Step 4: Generate CLAUDE.md**

Generate a customized CLAUDE.md with all choices baked in. Use the Clean Architecture structure below as the template when CA is selected:

```markdown
# [ProjectName] — Development Instructions

## Architecture
This project uses **Clean Architecture** with CQRS.

Layer structure:
- `Domain/` — Entities, ValueObjects, Events, Exceptions, ITenantEntity
- `Application/` — Features/{UseCase}/Command+Handler+Validator, Abstractions, Behaviors
- `Infrastructure/` — Persistence (EF Core + Dapper), Caching, Messaging (MassTransit)
- `Api/` — Controllers with [ApiController], Middleware, Extensions

Dependency direction: Domain ← Application ← Infrastructure ← Api.
Cross-layer dependencies must flow inward only. Enforce via project references.

## Tech Stack
- **Runtime**: .NET 8 LTS / C# 12
- **Database**: PostgreSQL
- **Write path**: EF Core 8 + Change Tracking + Repository pattern
- **Read path**: Dapper + raw SQL → DTO (no EF Include on reads)
- **Auth**: JWT Bearer — TenantId extracted from JWT claim, injected via ICurrentUser
- **Caching**: IDistributedCache (Redis) with Cache-Aside + Decorator pattern
- **Messaging**: MassTransit + RabbitMQ — Outbox/Inbox pattern for guaranteed delivery
- **Resilience**: Polly v8 pipelines
- **Observability**: Serilog + OpenTelemetry

## CQRS Rules
- Command handlers: load aggregate via IRepository → call domain method → IUnitOfWork.SaveChangesAsync
- Query handlers: IDbConnectionFactory → Dapper → return DTO. No EF Core, no .Include()
- Never use EF Core .Include() chains in query handlers

## Multi-Tenancy Rules
- Tenant-scoped entities implement ITenantEntity (TenantId property)
- TenantId is set from ICurrentUser on entity creation — never from the request DTO
- EF Core Global Query Filter automatically scopes all ITenantEntity queries
- ICurrentUser and ITenantContext live in Application — never inject into Domain

## Controllers
- Route pattern: `[Route("api/v{version:apiVersion}/[controller]")]`
- All controllers inherit ControllerBase, decorated with [ApiController] and [Authorize]
- Controllers dispatch to MediatR — no business logic in controllers

## Exception Hierarchy
- DomainException → 422 Unprocessable Entity
- NotFoundException → 404 Not Found
- ConflictException → 409 Conflict
- InfrastructureException → 502 Bad Gateway

## Testing Strategy
- Unit tests (tests/Unit/): Domain entities + Application handlers with mocked repositories — no DB
- Integration tests (tests/Integration/): Testcontainers real PostgreSQL, full HTTP stack
- Architecture tests (tests/Architecture/): NetArchTest layer dependency rules
- Contract tests (tests/Contract/): Pact / JSON schema

## Conventions
- Strongly-typed IDs: `readonly record struct DocumentId(Guid Value)` for all aggregate root IDs
- No data annotations on entities — all EF config in IEntityTypeConfiguration<T>
- All handler classes are `internal sealed`
- Records for all DTOs and commands/queries
- Primary constructors for all handlers and controllers
```

**Step 5: Next Steps**
Suggest:
- `dotnet new` commands for each layer project
- Directory.Build.props targeting `net8.0`, Directory.Packages.props with package versions
- First feature scaffold (Command + Query + Controller + tests)
- EF Core migration setup

### Health Check Workflow

Automated codebase analysis that produces a graded report card. Run when asked to "check health", "analyze the project", or "how's the codebase".

**Step 1: Solution Analysis**
```
→ get_project_graph
  Analyze: project count, dependency direction, target frameworks, naming consistency
  Check: do dependencies flow inward (Domain ← Application ← Infrastructure ← Api)?
```

**Step 2: Anti-pattern Scan**
```
→ detect_antipatterns (scope: solution)
  Priority violations for this stack:
  - EF Include chains in query handlers (should be Dapper)
  - ICurrentUser / ITenantContext injected into Domain
  - TenantId taken from request DTO (not from ICurrentUser)
  - async void, sync-over-async, DateTime.Now
```

**Step 3: Compiler Diagnostics**
```
→ get_diagnostics (severity: warning, scope: solution)
  Count warnings by category: CS8600 (nullability), CS0219 (unused vars), etc.
```

**Step 4: Dead Code Detection**
```
→ find_dead_code (scope: solution)
  Identify unused types, methods, and properties.
```

**Step 5: Test Coverage Assessment**
```
→ get_test_coverage_map
  Check: Unit tests for Domain entities, Integration tests for API endpoints.
  Flag: any controller action without a corresponding integration test.
```

**Step 6: Report Card**

```
## Codebase Health Report

### Grade: B+ (82/100)

| Category | Score | Issues |
|----------|-------|--------|
| Architecture | 18/20 | Clean dependency direction, 1 questionable Infrastructure→Domain reference |
| Anti-patterns | 14/20 | 2 EF Include in query handlers, 1 DateTime.Now |
| Diagnostics | 20/20 | 0 warnings |
| Dead Code | 16/20 | 3 unused methods found |
| Test Coverage | 14/20 | 70% of types have test coverage |

### Priority Actions
1. **Replace EF Include with Dapper** in GetOrderHandler, GetProductHandler
2. **Replace DateTime.Now with TimeProvider** (2 locations)
3. **Remove dead code** — 3 unused methods in OrderService
4. **Add integration tests** for ShippingController, NotificationController
```

Grading scale:
- **A (90-100)**: Production-ready, well-maintained
- **B (75-89)**: Good shape, minor improvements needed
- **C (60-74)**: Needs attention, several areas to improve
- **D (40-59)**: Significant issues, prioritize cleanup
- **F (<40)**: Critical problems, stop feature work and fix

### Migration Workflow (.NET version upgrade)

> For complete migration workflows (EF Core, NuGet, .NET version upgrades), see the **migration-workflow** skill.

This project targets **.NET 8 LTS** (supported until November 2026). Migration to a future LTS (e.g., .NET 10 LTS) should be planned when .NET 9 STS reaches end-of-life.

```bash
# GOOD — Systematic migration with verification at each step
# Phase 1: Update TFM in Directory.Build.props → build → fix breaking changes
# Phase 2: Update all package versions in Directory.Packages.props → build → fix
# Phase 3: Adopt new platform features → build → run all tests
# Phase 4: Full verification (Unit + Integration + Architecture + Contract)
```

## Anti-patterns

### Default to VSA or Minimal API Without Asking

```
# DON'T — generate VSA / Minimal API structure for a multi-tenant complex domain
"Here's your CLAUDE.md with Vertical Slice Architecture and IEndpointGroup..."

# DO — recommend Clean Architecture + Controllers for multi-tenant, complex domain projects
"Based on your multi-tenant domain with complex rules, I recommend Clean Architecture.
 The 4-layer structure keeps EF Global Query Filters, Dapper reads, and MassTransit
 in Infrastructure — isolated from your domain logic."
```

### Generic CLAUDE.md with Placeholders

```markdown
<!-- DON'T — User has to fill in everything manually -->
## Architecture
This project uses [ARCHITECTURE].
Database: [DATABASE]
Auth: [AUTH_METHOD]

<!-- DO — Fully populated from the conversation -->
## Architecture
This project uses Clean Architecture with CQRS.
Write path: EF Core 8 + Repository + Change Tracking
Read path: Dapper + raw SQL → DTO
```

### Recommending HybridCache or Wolverine

```csharp
// DON'T — this stack uses IDistributedCache + Redis (Cache-Aside + Decorator)
services.AddHybridCache();  // Not used in this stack

// DON'T — this stack uses MassTransit (not Wolverine)
services.AddWolverine();  // Not used in this stack

// DO
services.AddStackExchangeRedisCache(o => o.Configuration = config["Redis:ConnectionString"]);
services.AddMassTransit(x => { x.UsingRabbitMq(...); });
```

### Running Migration Without a Plan

```bash
# DON'T — Just changing the TFM and hoping for the best
sed -i 's/net8.0/net10.0/g' **/*.csproj
dotnet build  # 47 errors

# DO — Systematic: update TFM → build → fix → update packages → build → fix → test
```

## Decision Guide

| Scenario | Workflow | Key Tool |
|----------|----------|----------|
| New greenfield project | Project Init | architecture-advisor (default: Clean Architecture) |
| Joining existing project | Health Check → Init (for CLAUDE.md) | get_project_graph |
| "How's our codebase?" | Health Check | detect_antipatterns, get_diagnostics |
| "Upgrade to .NET 10 LTS" | Migration | get_project_graph, breaking-changes.md |
| "Generate CLAUDE.md for this project" | Project Init (skip new project steps) | get_project_graph |
| Multi-tenant API with business rules | Project Init | Recommend Clean Architecture + MassTransit + Redis |
| Simple CRUD API | Project Init | Consider VSA or minimal CA before recommending full DDD |
| Code quality declining | Health Check → set baseline → periodic re-check | All MCP tools |
