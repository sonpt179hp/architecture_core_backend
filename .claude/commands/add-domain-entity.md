# Command: add-domain-entity

## Purpose
Add a new DDD Aggregate Root or domain entity following project conventions.

## Expected inputs
- Entity/Aggregate name
- Bounded context
- Required invariants
- Value Objects
- Domain Events
- Multi-tenancy requirement

## Workflow
1. Read `ai-rules/01-clean-architecture.md`, `ai-rules/03-security-tenancy.md`, `ai-rules/07-testing.md`.
2. Inspect existing Domain model patterns.
3. Determine if full DDD is appropriate or simple CRUD is enough.
4. Generate entity/value object/domain event files.
5. Generate EF Core Fluent Configuration in Infrastructure.
6. Generate repository interface in Application and implementation in Infrastructure when needed.
7. Add Domain unit tests for invariants and events.

## Rules to apply
- `ai-rules/01-clean-architecture.md`
- `ai-rules/03-security-tenancy.md`
- `ai-rules/07-testing.md`
- `ai-rules/08-efcore.md`

## Output
- Aggregate Root/entity
- Value Objects
- Domain Events
- EF Core configuration
- Repository abstraction/implementation if needed
- Unit tests

## Verification
- No EF annotations in Domain
- Business invariants live inside Domain
- Domain does not depend on Infrastructure
