# Agent Routing & Orchestration

> This file defines how Claude Code routes queries to specialist agents and how agents coordinate.

## Agent Roster

| Agent | File | Primary Domain |
|-------|------|---------------|
| dotnet-architect | `agents/dotnet-architect.md` | Architecture, project structure, module boundaries |
| api-designer | `agents/api-designer.md` | Controllers, OpenAPI, versioning, rate limiting |
| ef-core-specialist | `agents/ef-core-specialist.md` | Database, queries, migrations, EF Core patterns |
| test-engineer | `agents/test-engineer.md` | Test strategy, xUnit, WebApplicationFactory, Testcontainers |
| security-auditor | `agents/security-auditor.md` | Authentication, authorization, OWASP, secrets |
| performance-analyst | `agents/performance-analyst.md` | Benchmarks, memory, async patterns, caching |
| devops-engineer | `agents/devops-engineer.md` | Docker, CI/CD, Aspire, deployment |
| code-reviewer | `agents/code-reviewer.md` | Multi-dimensional code review |
| build-error-resolver | `agents/build-error-resolver.md` | Autonomous build error fixing |
| refactor-cleaner | `agents/refactor-cleaner.md` | Systematic dead code removal and cleanup |

## Routing Table

Match user intent to agent. When multiple agents could handle a query, the first match wins.

| User Intent Pattern | Primary Agent | Support Agent |
|---|---|---|
| "set up project", "folder structure", "architecture" | dotnet-architect | — |
| "add module", "split into modules", "bounded context" | dotnet-architect | — |
| "create endpoint", "API route", "controller", "OpenAPI", "swagger" | api-designer | — |
| "versioning", "rate limiting", "CORS", "ProblemDetails", "api contract" | api-designer | — |
| "database", "migration", "query", "DbContext", "EF" | ef-core-specialist | — |
| "write tests", "test strategy", "coverage" | test-engineer | — |
| "WebApplicationFactory", "Testcontainers", "xUnit" | test-engineer | — |
| "security", "authentication", "JWT", "OIDC", "authorize", "tenant", "TenantId", "multi-tenant" | security-auditor | — |
| "performance", "benchmark", "memory", "profiling" | performance-analyst | — |
| "caching", "Redis", "IDistributedCache", "cache-aside", "cache key" | performance-analyst | — |
| "Docker", "container", "CI/CD", "pipeline", "deploy" | devops-engineer | — |
| "Aspire", "orchestration", "service discovery" | devops-engineer | — |
| "outbox", "inbox", "MassTransit", "RabbitMQ", "event", "message bus", "consumer" | dotnet-architect | ef-core-specialist |
| "background job", "Hangfire", "Quartz", "outbox processor", "scheduled" | dotnet-architect | ef-core-specialist |
| "TenantId", "multi-tenant", "ITenantEntity", "global filter" | security-auditor | dotnet-architect |
| "Serilog", "OpenTelemetry", "CorrelationId", "tracing", "health/live", "health/ready" | devops-engineer | — |
| "review this code", "PR review", "code quality" | code-reviewer | — |
| "choose architecture", "which architecture", "architecture decision" | dotnet-architect | — |
| "scaffold", "scaffold feature", "create feature", "add endpoint", "generate feature" | dotnet-architect | api-designer, ef-core-specialist |
| "create command", "create query", "create handler", "generate use case" | dotnet-architect | api-designer, ef-core-specialist |
| "new entity", "add entity", "scaffold entity", "domain entity" | dotnet-architect | ef-core-specialist |
| "init project", "setup project", "new project", "generate CLAUDE.md" | dotnet-architect | — |
| "health check", "analyze project", "project report" | code-reviewer | dotnet-architect |
| "review PR", "review changes", "code review", "PR review" | code-reviewer | — |
| "add migration", "ef migration", "update packages", "upgrade nuget" | ef-core-specialist | — |
| "conventions", "coding style", "detect patterns", "code consistency" | code-reviewer | — |
| "add feature" (architecture-appropriate) | dotnet-architect | api-designer, ef-core-specialist |
| "refactor" | code-reviewer | dotnet-architect |
| "build errors", "fix build", "won't compile" | build-error-resolver | — |
| "clean up", "dead code", "unused code" | refactor-cleaner | — |

## Skill Loading Order

Agents load skills in dependency order. Core skills load first.

### Default Load Order (All Agents)
1. `modern-csharp` — Always loaded, baseline C# knowledge
2. Agent-specific skills (see agent files)

### Per-Agent Skill Maps

| Agent | Skills |
|-------|--------|
| dotnet-architect | modern-csharp, clean-architecture, project-structure, scaffolding, project-setup, ddd |
| api-designer | modern-csharp, authentication, error-handling, openapi |
| ef-core-specialist | modern-csharp, ef-core, configuration, migration-workflow |
| test-engineer | modern-csharp |
| security-auditor | modern-csharp, authentication, configuration |
| performance-analyst | modern-csharp, caching |
| devops-engineer | modern-csharp |
| code-reviewer | modern-csharp, convention-learner + contextual (loads relevant skills incl. clean-architecture, ddd based on files under review) |
| build-error-resolver | modern-csharp + contextual: ef-core, dependency-injection |
| refactor-cleaner | modern-csharp + contextual: ef-core |

## MCP Tool Preferences

Agents should **prefer Roslyn MCP tools over file scanning** to reduce token consumption.

| Task | Use MCP Tool | Instead Of |
|------|-------------|-----------|
| Find where a type is defined | `find_symbol` | Grep/Glob across all .cs files |
| Find all usages of a type | `find_references` | Grep for the type name |
| Find implementations of an interface | `find_implementations` | Searching for `: IInterface` |
| Understand inheritance | `get_type_hierarchy` | Reading multiple files |
| Understand project dependencies | `get_project_graph` | Parsing .csproj files manually |
| Review a type's API surface | `get_public_api` | Reading the full source file |
| Check for compilation errors | `get_diagnostics` | Running `dotnet build` and parsing output |
| Find unused code for cleanup | `find_dead_code` | Manual inspection of all files |
| Check for circular dependencies | `detect_circular_dependencies` | Manually tracing project references |
| Understand method call chains | `get_dependency_graph` | Reading multiple files and tracing calls |
| Check which types have tests | `get_test_coverage_map` | Manually searching for test files |

## Slash Commands

Commands map to skills and agents. Use these as shortcuts for common workflows.

| Command | Primary Skill | Primary Agent | Purpose |
|---------|--------------|---------------|---------|
| `/dotnet-init` | project-setup | dotnet-architect | Interactive project initialization |
| `/scaffold` | scaffolding | dotnet-architect | Architecture-aware feature scaffolding |
| `/security-scan` | security-scan | security-auditor | OWASP + secrets + dependency audit |
| `/migrate` | migration-workflow | ef-core-specialist | Safe EF Core migration workflow |
| `/health-check` | health-check | code-reviewer | Graded project health report |

## Conflict Resolution

When two agents could handle a query:

1. **Architecture questions win over implementation** — "How should I structure the payment module?" → dotnet-architect, even though api-designer could handle the endpoint part
2. **Specific beats general** — "How do I optimize this EF query?" → ef-core-specialist, not performance-analyst
3. **Security concerns are always surfaced** — Even when another agent is primary, flag security issues for the security-auditor
4. **Code review is holistic** — The code-reviewer loads skills contextually based on what's in the PR

## Token Budget Guidance

- **Small queries** (single pattern/fix): Load 1-2 skills, use MCP tools for context
- **Medium queries** (feature implementation): Load 3-4 skills, use MCP tools to understand existing code
- **Large queries** (architecture review): Load all relevant skills, use `get_project_graph` first to understand the solution shape

## Response Patterns

All agents should:
1. **Start with the recommended approach** — Don't enumerate all options equally
2. **Show code first, explain after** — Developers prefer seeing the solution, then understanding why
3. **Flag anti-patterns proactively** — If the user's existing code has issues, mention them
4. **Reference skills** — Point to relevant skills for deeper reading
5. **Use MCP tools before reading files** — Reduce token consumption
