# Skill: Setup Testing Infrastructure

## Purpose

Dựng đầy đủ hạ tầng test: Unit Tests (Domain + Application), Integration Tests với Testcontainers, và Architecture Tests để enforce dependency rules.

## Convention Mapping

| Artifact | Convention | Pattern |
|---|---|---|
| **Test Framework** | xUnit + FluentAssertions | |
| **Mocking** | NSubstitute | |
| **Test DB** | `Microsoft.EntityFrameworkCore.InMemory` | Unit Tests |
| **Integration DB** | Testcontainers.PostgreSql + Testcontainers.Redis | Integration Tests |
| **GlobalUsings** | `global using Xunit;` | |

## Project Structure

```
tests/
├── {Solution}.UnitTests/
│   ├── Domain/
│   │   └── {Entity}Tests.cs
│   ├── Application/
│   │   ├── {UseCase}CommandHandlerTests.cs
│   │   └── {QueryName}QueryHandlerTests.cs
│   └── GlobalUsings.cs
├── {Solution}.IntegrationTests/
│   ├── Infrastructure/
│   │   └── WebAppFactory.cs
│   ├── {Feature}/
│   │   └── {Feature}EndpointTests.cs
│   └── GlobalUsings.cs
└── {Solution}.ArchitectureTests/
    └── DependencyRuleTests.cs
```

## Instructions

**Input:** Tên bounded context, features cần test.

### Step 1: Create Unit Test Project

`tests/{Solution}.UnitTests/{Solution}.UnitTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\{Solution}.Domain\{Solution}.Domain.csproj" />
    <ProjectReference Include="..\..\src\{Solution}.Application\{Solution}.Application.csproj" />
    <ProjectReference Include="..\..\src\{Solution}.Infrastructure\{Solution}.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

### Step 2: Create GlobalUsings.cs

`tests/{Solution}.UnitTests/GlobalUsings.cs`:

```csharp
global using Xunit;
```

### Step 3: Create Unit Test Helpers

`tests/{Solution}.UnitTests/TestDbContextFactory.cs`:

```csharp
using {Namespace}.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace {Solution}.UnitTests;

internal static class TestDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
```

### Step 4: Create Domain Tests

`tests/{Solution}.UnitTests/Domain/{Entity}Tests.cs`:

```csharp
using {Namespace}.Domain.{Feature};
using {Namespace}.Domain.{Feature}.Errors;
using FluentAssertions;

namespace {Solution}.UnitTests.Domain;

public class {Entity}Tests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var result = {Entity}.Create("Test Name");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Name");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldReturnError()
    {
        var result = {Entity}.Create(string.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({Entity}Errors.NameEmpty);
    }
}
```

### Step 5: Create Application Handler Tests

`tests/{Solution}.UnitTests/Application/{UseCase}CommandHandlerTests.cs`:

```csharp
using {Namespace}.Application.{Feature}.Commands.{UseCase};
using {Namespace}.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace {Solution}.UnitTests.Application;

public class {UseCase}CommandHandlerTests
{
    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccess()
    {
        await using var context = CreateContext();
        var handler = new {UseCase}CommandHandler(context);
        var command = new {UseCase}Command("Test", 9.99m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.{Entities}.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithInvalidData_ShouldReturnFailure()
    {
        await using var context = CreateContext();
        var handler = new {UseCase}CommandHandler(context);
        var command = new {UseCase}Command(string.Empty, -1m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
```

### Step 6: Create Integration Test Project

`tests/{Solution}.IntegrationTests/{Solution}.IntegrationTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\{Solution}.Api\{Solution}.Api.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Testcontainers" />
    <PackageReference Include="Testcontainers.PostgreSql" />
    <PackageReference Include="Testcontainers.Redis" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

### Step 7: Create WebAppFactory

`tests/{Solution}.IntegrationTests/Infrastructure/WebAppFactory.cs`:

```csharp
using {Namespace}.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace {Solution}.IntegrationTests.Infrastructure;

public sealed class WebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());

        var redisConn = _redis.GetConnectionString();
        var parts = redisConn.Split(':');
        builder.UseSetting("Redis:Host", parts[0]);
        builder.UseSetting("Redis:Port", parts.Length > 1 ? parts[1] : "6379");

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_postgres.GetConnectionString());
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}
```

### Step 8: Create Integration Endpoint Tests

`tests/{Solution}.IntegrationTests/{Feature}/{Feature}EndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using {Namespace}.Application.{Feature}.Commands.Create{Entity};
using {Namespace}.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace {Solution}.IntegrationTests.{Feature};

[Trait("Category", "Integration")]
public sealed class {Feature}EndpointTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;

    public {Feature}EndpointTests(WebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValid{Entity}_ShouldReturn201Created()
    {
        var command = new Create{Entity}Command("Test", 29.99m);

        var response = await _client.PostAsJsonAsync("/api/{entities}", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_NonExistent{Entity}_ShouldReturn404()
    {
        var response = await _client.GetAsync($"/api/{entities}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

## Checklist

- [ ] Unit Tests dùng InMemory database
- [ ] Integration Tests dùng Testcontainers với real PostgreSQL + Redis
- [ ] Unit Tests không reference `Api` project
- [ ] Integration Tests reference `Api` và extend `WebApplicationFactory<Program>`
- [ ] Tests có `[Fact]` từ xUnit
- [ ] Assertions dùng `FluentAssertions` (.Should())

## References

- `{Solution}.UnitTests/{Solution}.UnitTests.csproj`
- `{Solution}.IntegrationTests/{Solution}.IntegrationTests.csproj`
- `{Solution}.IntegrationTests/Infrastructure/WebAppFactory.cs`
