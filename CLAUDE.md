# CLAUDE.md

## Purpose

This repository is the **shared Claude Code foundation for future .NET projects**.
Its purpose is to centralize reusable:
- technical rules
- implementation skills
- slash-command workflows
- specialized review/design agents
- sample architecture references

When working here, prioritize improving the shared foundation over making GovDocs-only assumptions.
`GovDocs/` is a sample solution and reference implementation, not the only target shape.

## What Claude should treat as authoritative

### Single source of truth for technical standards
- `ai-rules/`

Every architectural, implementation, testing, security, caching, configuration, and background-job rule must ultimately come from `ai-rules/`.
Do not create a second competing rules source.

### Claude-native runtime assets
- `.claude/skills/`
- `.claude/commands/`
- `.claude/agents/`
- `.claude/settings.json`
- `.mcp.json`
- `CLAUDE.md`

These files/folders exist to help Claude Code operate effectively, but they do **not** replace `ai-rules/`.

## Priority order when reasoning

1. Follow `ai-rules/` as the canonical technical rule source
2. Follow `CLAUDE.md` for repository-level operating instructions
3. Reuse existing skills, commands, and agents before inventing new patterns
4. Use `GovDocs/` as a reference implementation, not as the rule source
5. Keep outputs reusable across multiple .NET projects

## Expected architecture defaults

Unless the user clearly asks otherwise, assume the preferred architecture is:
- Clean Architecture
- Pragmatic DDD for core domains
- CQRS
- Multi-tenancy
- ASP.NET Core Web API
- EF Core for writes
- Dapper for complex reads
- Redis for distributed caching
- Outbox/Inbox pattern for reliable integration events
- xUnit + Testcontainers + Architecture Tests

## How to use the repository contents

### `ai-rules/`
Use as the canonical source for:
- architecture constraints
- CQRS behavior
- tenancy/security boundaries
- API contracts
- resilience
- observability
- testing strategy
- EF Core usage
- error handling
- dependency injection
- configuration
- caching
- background jobs

### `.claude/skills/`
Use when the task is a **specific implementation/scaffolding task**.
Examples:
- create a command
- create a query
- create a domain entity
- add an event handler
- set up caching
- set up configuration

Skills should:
- be reusable
- stay concise
- point to `ai-rules/`
- avoid duplicating full rules

### `.claude/commands/`
Use for **repeatable workflows** that a developer should be able to trigger with a short slash command.
Examples:
- `/new-feature`
- `/add-domain-entity`
- `/add-api-endpoint`
- `/review-clean-architecture`
- `/write-unit-tests`
- `/write-integration-tests`

Commands should:
- define workflow, not full standards
- tell Claude which `ai-rules/` files to read
- stay shorter than the rules they depend on

### `.claude/agents/`
Use for **specialized reviewer/designer personas**.
Examples:
- clean architecture reviewer
- EF Core reviewer
- security reviewer
- test engineer
- backend architect

Agents should:
- focus on one concern
- review through the lens of `ai-rules/`
- not become a second rule system

### `.mcp.json`
Use for project-level MCP server configuration.
At present, this repository expects Roslyn MCP for .NET-aware exploration when available.

### `.claude/settings.json`
Use for project-level Claude Code settings.
If hooks are needed later, configure them here rather than inventing a parallel hooks directory.

## Rules for maintaining this foundation

- `ai-rules/` is the only canonical technical rule source
- Do not re-create `rules/` folders that duplicate `ai-rules/`
- Do not copy full rule content into commands or agents
- If a pattern is broadly reusable, promote it into:
  - `.claude/skills/`
  - `.claude/commands/`
  - `.claude/agents/`
  - or a new/generalized file under `ai-rules/`
- If a pattern is only useful for GovDocs, keep it clearly scoped to `GovDocs/`

## How Claude should respond in this repository

When asked to scaffold or modify a .NET backend project:
- read the relevant `ai-rules/` files first
- prefer existing commands when the workflow already exists
- prefer existing skills when the task maps to a known scaffolding pattern
- use agents when the task is a specialized review/design concern
- keep Domain pure
- keep Application free from Infrastructure dependencies
- keep Controllers thin
- keep business invariants in Domain
- use Dapper for complex read paths
- use `ProblemDetails` for API errors
- propagate `CancellationToken`
- enforce tenant isolation everywhere relevant

## Review expectations

For specialized review tasks, prefer these agents:
- `.claude/agents/clean-architecture-reviewer.md`
- `.claude/agents/dotnet-backend-architect.md`
- `.claude/agents/ef-core-reviewer.md`
- `.claude/agents/test-engineer.md`
- `.claude/agents/security-reviewer.md`

## Applying this foundation to a new .NET project

Preferred approach:
1. Copy `CLAUDE.md`
2. Copy `.claude/settings.json`
3. Copy `.claude/skills/`
4. Copy `.claude/commands/`
5. Copy `.claude/agents/`
6. Copy `.mcp.json`
7. Optionally copy `ai-rules/` for full technical documentation

Avoid assuming git submodules unless the user explicitly requests that workflow.
