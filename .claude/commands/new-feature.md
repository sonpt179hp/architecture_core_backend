# Command: new-feature

## Purpose
Scaffold a complete .NET Clean Architecture feature using CQRS.

## Expected inputs
- Bounded context/module name
- Use case name
- Operation type: create, update, delete, get-by-id, list, search
- Aggregate name
- Request fields
- Return type

## Workflow
1. Read `ai-rules/01-clean-architecture.md`, `ai-rules/02-cqrs-pattern.md`, `ai-rules/03-security-tenancy.md`, `ai-rules/04-api-contract.md`.
2. Inspect current solution structure with Glob.
3. Choose Command or Query based on operation type.
4. Generate use-case files under `Application/Features/{UseCase}/`.
5. Update or create Controller action in API layer.
6. Add validator with input-shape validation only.
7. Add tests when test projects exist.
8. Run build/tests when possible.

## Rules to apply
- `ai-rules/01-clean-architecture.md`
- `ai-rules/02-cqrs-pattern.md`
- `ai-rules/03-security-tenancy.md`
- `ai-rules/04-api-contract.md`
- `ai-rules/07-testing.md`

## Output
- Command/Query record
- Handler
- Validator
- DTOs when needed
- Controller action
- Unit/integration tests when test projects exist

## Verification
- No Infrastructure imports in Application
- CancellationToken propagated
- TenantId resolved from ITenantContext
- HTTP status codes are correct
- Build/test pass
