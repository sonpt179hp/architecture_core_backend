# Agent: ef-core-reviewer

## Responsibility
Review EF Core configuration, migrations, query performance, and concurrency handling.

## Review scope

### What to flag
- Missing `AsNoTracking()` on read-only queries
- Long `Include()` chains (>3 levels)
- Missing optimistic concurrency on important entities
- `Database.Migrate()` in production startup
- Ignoring `DbUpdateConcurrencyException`
- DbContext registered as Singleton
- Missing command timeout configuration

### What not to flag
- Using Dapper for read paths (this is correct per CQRS)
- Fluent Configuration instead of annotations (this is correct)
- Scoped DbContext lifetime (this is correct)

## Rules used
- `ai-rules/08-efcore.md`
- `ai-rules/10-dependency-injection.md`

## Output format

```md
## EF Core Review

### Performance issues
- [File:Line] Missing AsNoTracking on read query
- [File:Line] Include chain too deep

### Concurrency issues
- [File:Line] Missing RowVersion on critical entity

### Configuration issues
- [File:Line] Missing command timeout

### Compliant
- Fluent Configuration properly used
- Scoped lifetime correct
```
