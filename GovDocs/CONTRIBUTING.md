# Contributing

Thank you for considering contributing to this project.

## Branch Naming

| Type | Pattern | Example |
|---|---|---|
| Feature | `feat/<name>` | `feat/add-order-aggregate` |
| Bug fix | `fix/<name>` | `fix/product-validation` |
| Chore | `chore/<name>` | `chore/update-packages` |

## Pull Request Requirements

- One concern per PR (don't mix features and refactors)
- All new behaviour must have tests
- `dotnet format --verify-no-changes` must pass before merge
- No new TreatWarningsAsErrors violations

## Code Style

- File-scoped namespaces (`namespace Foo;`)
- Braces always, even for single-line bodies
- `sealed` on concrete classes where inheritance isn't intended
- `internal` handlers/services — public only on contracts

See `.editorconfig` for detailed style rules.

## Running Tests Locally

```bash
# Unit tests only (no Docker required)
dotnet test --filter "Category!=Integration"

# All tests (requires Docker)
docker compose up -d postgres redis
dotnet test
```

## Reference

See [pull_request_template.md](.github/pull_request_template.md) for the PR checklist.
