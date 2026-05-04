# Commands

Reusable slash-command shortcuts for common .NET Clean Architecture workflows.
Invoke with `/<command-name>` inside Claude Code.

## Available Commands

| Command | Purpose |
|---|---|
| `new-feature` | Scaffold a complete new CQRS feature (Command + Query + Handler + Validator + Controller) |
| `add-domain-entity` | Add a new Domain entity (Aggregate Root + Value Objects + Events) |
| `add-api-endpoint` | Add a new API endpoint to an existing Controller |
| `review-clean-architecture` | Review current directory for Clean Architecture compliance |
| `write-unit-tests` | Scaffold unit tests for the current file or feature |
| `write-integration-tests` | Scaffold integration tests using Testcontainers |
| `add-background-job` | Scaffold a new background job with Outbox/Inbox pattern |
| `setup-redis-cache` | Scaffold Redis caching layer with Decorator pattern |

## Usage

```bash
/new-feature               # Scaffold a full CQRS feature
/add-domain-entity         # Add a new domain entity
/review-clean-architecture # Review current directory
/write-unit-tests          # Generate unit tests
```

## Design Guidelines

Each command must specify:
- **Inputs** — what the user needs to provide
- **Workflow** — ordered steps the AI should follow
- **Rules to apply** — which `ai-rules/` files to reference
- **Output** — file structure produced
- **Verification** — how to validate the generated code
