# GovDocs

![CI](https://github.com/Son Pham/gov-docs/workflows/CI/badge.svg)
![.NET 8](https://img.shields.io/badge/.NET-8.0-blueviolet)
![License](https://img.shields.io/badge/License-MIT-green)
![template](https://img.shields.io/badge/dotnet_new-clean--arch-blue?logo=dotnet)

Production-ready .NET 8 Clean Architecture template. No ceremony, no AutoMapper, no MediatR licence risk — just a fast, auditable, AOT-friendly foundation for backend services.

---

## Create a New Project

> **One command to scaffold a fully-configured .NET 8 backend.**
> The template renames namespaces, files, badges, and runs `dotnet restore` automatically.

### Option A — Claude Code (recommended)

```powershell
# Download and install the skill (no clone needed)
New-Item -Force -ItemType Directory ~/.claude/skills/gov-docs | Out-Null
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/Son Pham/gov-docs/main/.claude/skills/gov-docs/SKILL.md" -OutFile ~/.claude/skills/gov-docs/SKILL.md
```

Then open Claude Code and say:

```
/gov-docs
```

> **What happens:** Claude checks/installs the dotnet template, asks for your
> project name, scaffolds the solution, inits git, verifies the build, and
> prints next steps — all hands-free.

### Option B — CLI only

```bash
# 1. Install the dotnet template (once per machine)
git clone https://github.com/Son Pham/gov-docs.git ~/src/gov-docs
dotnet new install ~/src/gov-docs

# 2. Scaffold
dotnet new clean-arch \
  --name OrderService \
  --githubOwner acme-corp \
  --output ./OrderService

# 3. Init git
cd OrderService && git init -b main && git add -A && git commit -m "init"
```

<details>
<summary><strong>NuGet install (when published)</strong></summary>

```bash
dotnet new install GovDocs.Template
```
</details>

> See [`.claude/skills/gov-docs/README.md`](./.claude/skills/gov-docs/README.md)
> for full skill install / verify / update / uninstall flows.

---

## Architecture

```
Domain  <--  Application  <--  Infrastructure  <--  API
  ^               ^
UnitTests     IntegTests (WebApplicationFactory + Testcontainers)
```

```
+--------------------------------------------------------------+
|                          API Layer                           |
|  Controllers -> ISender (Mediator)                           |
|  Program.cs  |  Middleware  |  Swagger  |  HealthChecks      |
+---------------------------+----------------------------------+
                            |
          +-----------------+------------------+
          |                                    |
          v                                    v
+--------------------+          +----------------------------+
|  Application Layer |          |    Infrastructure Layer    |
|                    |          |                            |
|  Commands/Queries  |          |  ApplicationDbContext      |
|  ValidationBehav.  |          |  (EF Core + Npgsql)        |
|  LoggingBehavior   |          |                            |
|  ICacheService ----+--------> |  CacheService (Redis)      |
|  IAppDbContext ----+--------> |  ApplicationDbContext      |
|                    |          |  JwtOptions / AddJwtAuth   |
+--------+-----------+          +----------------------------+
         |
         v
+--------------------+
|    Domain Layer    |
|                    |
|  Result<T>/Error   |
|  Entity base       |
|  AggregateRoot     |
|  IDomainEvent      |
|  Product aggregate |
|  ProductId (VO)    |
|  ProductErrors     |
+--------------------+
```

---

## Quick Start

```bash
# 1. Clone and configure
git clone https://github.com/Son Pham/gov-docs.git
cp .env.example .env          # Fill in secrets

# 2. Start dependencies
docker compose up -d postgres redis

# 3. Run the API
dotnet run --project src/GovDocs.Api
# Swagger: http://localhost:5000/swagger
```

---

## Environment Variables

Postgres uses the standard `ConnectionStrings:Default` key.
Redis is bound to a typed `Redis` section so individual fields (host/port/password/ssl) can be overridden separately.

| Variable | Description | Default |
|---|---|---|
| `ConnectionStrings__Default` | Postgres connection string | `Host=localhost;Port=5432;...` |
| `Redis__Host` | Redis host | `localhost` |
| `Redis__Port` | Redis port | `6379` |
| `Redis__Password` | Redis password (optional) | _(empty)_ |
| `Redis__Ssl` | Use TLS | `false` |
| `Jwt__Issuer` | JWT token issuer | `gov-docs` |
| `Jwt__Audience` | JWT token audience | `gov-docs` |
| `Jwt__SecretKey` | JWT signing key (32+ chars) | _(required)_ |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` |

---

## Project Structure

| Project | Purpose |
|---|---|
| `GovDocs.Domain` | Entities, value objects, domain errors, Result types. Zero NuGet deps. |
| `GovDocs.Application` | CQRS handlers, validation, pipeline behaviors, interfaces. |
| `GovDocs.Infrastructure` | EF Core + Npgsql, Redis cache, JWT config, interceptors. |
| `GovDocs.Api` | Controllers, Swagger, health checks, exception handler. |
| `GovDocs.UnitTests` | xUnit + FluentAssertions + NSubstitute unit tests. |
| `GovDocs.IntegrationTests` | Testcontainers-backed end-to-end tests. |

---

## Adding a New Feature

1. **Domain** — Add entity/value object in `src/GovDocs.Domain/<Feature>/`
2. **Application** — Add Command or Query + Handler + Validator in `src/GovDocs.Application/<Feature>/`
3. **Infrastructure** — Add EF entity configuration in `src/GovDocs.Infrastructure/Persistence/Configurations/`
4. **API** — Add controller endpoints in `src/GovDocs.Api/Controllers/`
5. **Tests** — Add unit tests and integration tests

---

## Key Technology Choices

- **Mediator** (martinothamar) — MIT, source-generated CQRS, zero reflection, AOT-safe
- **FluentValidation** — validation pipeline via `ValidationBehavior`
- **EF Core 8 + Npgsql** — PostgreSQL with snake_case conventions
- **Swashbuckle** — Swagger UI with JWT bearer auth
- **Serilog** — structured logging with Seq sink in development
- **Testcontainers** — real Postgres + Redis in integration tests

---

## License

MIT License. See [LICENSE](LICENSE).
