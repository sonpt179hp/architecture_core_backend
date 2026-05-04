# Command: review-clean-architecture

## Purpose
Review code for Clean Architecture, CQRS, tenancy, and testing compliance.

## Expected inputs
- Target file, folder, project, or solution
- Optional focus area: dependency rule, CQRS, API contract, security, tests, EF Core

## Workflow
1. Read all relevant `ai-rules/*.md` files.
2. Inspect project references and using statements.
3. Check dependency direction: Domain ← Application ← Infrastructure ← API.
4. Check use-case structure and handler responsibilities.
5. Check tenant isolation and authorization rules.
6. Check API semantics and ProblemDetails usage.
7. Check tests and architecture-test coverage.
8. Report findings by severity.

## Rules to apply
- `ai-rules/01-clean-architecture.md`
- `ai-rules/02-cqrs-pattern.md`
- `ai-rules/03-security-tenancy.md`
- `ai-rules/04-api-contract.md`
- `ai-rules/07-testing.md`
- `ai-rules/08-efcore.md`

## Output
```md
## Findings

### Critical
- ...

### Warning
- ...

### Suggestions
- ...

## Files reviewed
- ...
```

## Verification
If the repo has Architecture Tests, run them after fixes.
