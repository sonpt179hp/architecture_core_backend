---
name: project-structure
description: >
  .NET 8 solution and project structure conventions for Clean Architecture.
  Covers the 4-layer layout (Domain, Application, Infrastructure, Api),
  4-category test structure (Unit, Integration, Architecture, Contract),
  .slnx format, Directory.Build.props targeting net8.0, Directory.Packages.props
  for central package management, global usings, and naming conventions.
  Load this skill when setting up a new solution, adding projects, configuring
  build properties, or when the user mentions "solution structure", ".slnx",
  "Directory.Build.props", "central package management", "Directory.Packages.props",
  "global usings", ".editorconfig", "project layout", or "naming conventions".
---

# Project Structure

## Core Principles

1. **Clean Architecture layer order** — Domain → Application → Infrastructure → Api. Dependencies only point inward. Each layer is a separate project; the compiler enforces the rule via project references.
2. **Central package management** — Use `Directory.Packages.props` to manage NuGet package versions in one place. No version numbers in individual `.csproj` files.
3. **Shared build properties** — Use `Directory.Build.props` for common settings (target framework `net8.0`, nullable, implicit usings). Don't repeat in every project.
4. **.slnx for solutions** — The XML-based solution format is cleaner and more merge-friendly than the legacy `.sln` format.
5. **4-category testing** — Unit (Domain + Application, no DB), Integration (Testcontainers real PostgreSQL), Architecture (NetArchTest layer rules), Contract (Pact/JSON schema).
6. **Each bounded context = one module** — Separate project set + its own migrations. Avoids cross-context coupling at the DB layer.

## Patterns

### Solution Layout

```
MyApp/
├── MyApp.slnx
├── Directory.Build.props             # Shared MSBuild properties (net8.0, C# 12)
├── Directory.Packages.props          # Central package versions
├── .editorconfig
├── .gitignore
├── global.json                       # SDK version pinning
│
├── src/
│   ├── MyApp.Domain/                 # Entities, ValueObjects, Events, Exceptions, Interfaces
│   │   └── MyApp.Domain.csproj      # No external NuGet deps (pure C#)
│   │
│   ├── MyApp.Application/            # Features/{UseCase}/Command+Handler+Validator, Abstractions, Behaviors
│   │   └── MyApp.Application.csproj  # Refs: Domain. NuGet: MediatR, FluentValidation
│   │
│   ├── MyApp.Infrastructure/         # Persistence/Migrations, Caching, BackgroundJobs, Messaging
│   │   └── MyApp.Infrastructure.csproj # Refs: Application, Domain. NuGet: EF Core, Dapper, MassTransit, Redis
│   │
│   └── MyApp.Api/                    # Controllers, Middleware, Extensions, OpenApi
│       └── MyApp.Api.csproj          # Refs: Application, Infrastructure. NuGet: Asp.Versioning
│
└── tests/
    ├── Unit/
    │   ├── MyApp.Domain.Tests/       # Pure domain logic — no DB, no mocks of infra
    │   └── MyApp.Application.Tests/  # Handler tests with mocked repositories
    │
    ├── Integration/
    │   └── MyApp.Integration.Tests/  # Real PostgreSQL via Testcontainers
    │
    ├── Architecture/
    │   └── MyApp.Architecture.Tests/ # NetArchTest layer dependency rules
    │
    └── Contract/
        └── MyApp.Contract.Tests/     # Pact / JSON schema consumer-driven contracts
```

### Directory.Build.props

Targets .NET 8 LTS with C# 12. All projects inherit without repeating these properties.

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisMode>All</AnalysisMode>
  </PropertyGroup>

  <!-- Global usings shared across all projects -->
  <ItemGroup>
    <Using Include="System.Threading" />
    <Using Include="System.Threading.Tasks" />
  </ItemGroup>
</Project>
```

### Directory.Packages.props (Central Package Management)

All package versions defined once. `.csproj` files reference packages without `Version` attribute.

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- CQRS + Validation -->
    <PackageVersion Include="MediatR" Version="12.4.1" />
    <PackageVersion Include="MediatR.Extensions.Microsoft.DependencyInjection" Version="11.1.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.2" />

    <!-- Data — Write path -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.11" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11" />

    <!-- Data — Read path -->
    <PackageVersion Include="Dapper" Version="2.1.35" />
    <PackageVersion Include="Npgsql" Version="8.0.6" />

    <!-- Caching -->
    <PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.11" />

    <!-- Messaging -->
    <PackageVersion Include="MassTransit" Version="8.3.6" />
    <PackageVersion Include="MassTransit.RabbitMQ" Version="8.3.6" />

    <!-- Resilience -->
    <PackageVersion Include="Polly" Version="8.5.2" />
    <PackageVersion Include="Microsoft.Extensions.Http.Polly" Version="8.0.11" />

    <!-- API -->
    <PackageVersion Include="Asp.Versioning.Mvc" Version="8.1.0" />
    <PackageVersion Include="Asp.Versioning.Mvc.ApiExplorer" Version="8.1.0" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="8.0.11" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="6.9.0" />

    <!-- Observability -->
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.3" />
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.10.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.10.1" />

    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.11" />
    <PackageVersion Include="Testcontainers.PostgreSql" Version="3.10.0" />
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="FluentAssertions" Version="6.12.2" />
  </ItemGroup>
</Project>
```

### Project File (.csproj) Examples

```xml
<!-- MyApp.Domain.csproj — no external NuGet deps -->
<Project Sdk="Microsoft.NET.Sdk">
  <!-- TargetFramework inherited from Directory.Build.props -->
</Project>

<!-- MyApp.Application.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="MediatR" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <ProjectReference Include="..\MyApp.Domain\MyApp.Domain.csproj" />
  </ItemGroup>
</Project>

<!-- MyApp.Infrastructure.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Dapper" />
    <PackageReference Include="Npgsql" />
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" />
    <PackageReference Include="MassTransit.RabbitMQ" />
    <PackageReference Include="Polly" />
    <ProjectReference Include="..\MyApp.Application\MyApp.Application.csproj" />
  </ItemGroup>
</Project>

<!-- MyApp.Api.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <PackageReference Include="Asp.Versioning.Mvc" />
    <PackageReference Include="Serilog.AspNetCore" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
    <ProjectReference Include="..\MyApp.Application\MyApp.Application.csproj" />
    <ProjectReference Include="..\MyApp.Infrastructure\MyApp.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### global.json (SDK Pinning)

```json
{
  "sdk": {
    "version": "8.0.404",
    "rollForward": "latestFeature"
  }
}
```

### .slnx Solution Format

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/MyApp.Domain/MyApp.Domain.csproj" />
    <Project Path="src/MyApp.Application/MyApp.Application.csproj" />
    <Project Path="src/MyApp.Infrastructure/MyApp.Infrastructure.csproj" />
    <Project Path="src/MyApp.Api/MyApp.Api.csproj" />
  </Folder>
  <Folder Name="/tests/Unit/">
    <Project Path="tests/Unit/MyApp.Domain.Tests/MyApp.Domain.Tests.csproj" />
    <Project Path="tests/Unit/MyApp.Application.Tests/MyApp.Application.Tests.csproj" />
  </Folder>
  <Folder Name="/tests/Integration/">
    <Project Path="tests/Integration/MyApp.Integration.Tests/MyApp.Integration.Tests.csproj" />
  </Folder>
  <Folder Name="/tests/Architecture/">
    <Project Path="tests/Architecture/MyApp.Architecture.Tests/MyApp.Architecture.Tests.csproj" />
  </Folder>
</Solution>
```

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Solution | `CompanyName.AppName` or `AppName` | `MyApp.slnx` |
| Project | `AppName.Layer` | `MyApp.Api`, `MyApp.Domain` |
| Namespace | Matches folder path | `MyApp.Application.Features.Orders` |
| Feature folder | PascalCase, noun phrase | `Features/Orders/Commands/CreateOrder/` |
| Test project | `ProjectName.Tests` | `MyApp.Domain.Tests` |
| Controller | `{Entity}sController` | `OrdersController` |
| Command | `{Verb}{Noun}Command` | `CreateOrderCommand` |
| Query | `Get{Noun}Query` | `GetOrderQuery` |
| Repository interface | `I{Aggregate}Repository` | `IOrderRepository` |

## Anti-patterns

### Wrong Dependency Direction

```csharp
// BAD — Application references Infrastructure (wrong direction)
// MyApp.Application.csproj
<ProjectReference Include="..\MyApp.Infrastructure\MyApp.Infrastructure.csproj" />

// BAD — Domain references Application
// MyApp.Domain.csproj
<ProjectReference Include="..\MyApp.Application\MyApp.Application.csproj" />

// GOOD — Dependencies only point inward: Api → Infrastructure → Application → Domain
```

### Don't Scatter Package Versions

```xml
<!-- BAD — version in every .csproj, version drift across projects -->
<PackageReference Include="MediatR" Version="11.0.0" />  <!-- in Application -->
<PackageReference Include="MediatR" Version="12.4.1" />  <!-- in Api -->

<!-- GOOD — central management, one version for the entire solution -->
<!-- Directory.Packages.props: <PackageVersion Include="MediatR" Version="12.4.1" /> -->
<!-- .csproj: <PackageReference Include="MediatR" /> -->
```

### Don't Repeat Build Properties

```xml
<!-- BAD — same properties in every .csproj -->
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
</PropertyGroup>

<!-- GOOD — once in Directory.Build.props, inherited everywhere -->
```

### Don't Mix Source and Test Projects

```
# BAD — tests mixed with source
src/
  MyApp.Domain/
  MyApp.Domain.Tests/

# GOOD — clear separation by category
src/  MyApp.Domain/
tests/
  Unit/     MyApp.Domain.Tests/
  Integration/  MyApp.Integration.Tests/
  Architecture/ MyApp.Architecture.Tests/
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| New solution | `.slnx` format + Directory.Build.props + Directory.Packages.props |
| Target framework | `net8.0` (LTS) |
| Language version | `12` (ships with .NET 8) |
| Package version management | `Directory.Packages.props` (central) |
| Shared build settings | `Directory.Build.props` |
| SDK version pinning | `global.json` |
| Complex domain + team | Full 4-layer CA: Domain, Application, Infrastructure, Api |
| Simple API (1-2 devs) | Single project or Api + Infrastructure only |
| Multiple bounded contexts | Separate project sets per context, each with own migrations |
