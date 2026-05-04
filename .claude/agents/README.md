# Agents

Specialized personas for deep, focused work on a specific .NET Clean Architecture concern.
Spawn with `@<agent-name>` in a Claude Code thread.

## Available Agents

| Agent | Responsibility |
|---|---|
| `clean-architecture-reviewer` | Enforces dependency rule, flags cross-layer imports |
| `dotnet-backend-architect` | Designs bounded contexts, event storming, solution structure |
| `ef-core-reviewer` | Reviews EF Core configuration, migrations, query performance |
| `test-engineer` | Designs test strategy, scaffolds unit/integration/architecture tests |
| `security-reviewer` | Audits tenant isolation, authorization, secrets management |

## Usage

```bash
@clean-architecture-reviewer  # Review current file/folder
@dotnet-backend-architect     # Design new bounded context
@test-engineer                # Plan test coverage
```

## Design Guidelines

Each agent defines:
- **Responsibility** — what it OWNS and is accountable for
- **Review scope** — what it MUST and MUST NOT check
- **Rules consumed** — references to `ai-rules/`
- **Output format** — structured feedback or code
