---
name: openapi
description: >
  Swashbuckle OpenAPI/Swagger documentation for .NET 8 applications. Covers
  AddSwaggerGen setup, JWT bearer security scheme, XML comments, operation filters,
  versioned documents, and Swagger UI configuration.
  Load this skill when setting up API documentation, adding security schemes to docs,
  or when the user mentions "OpenAPI", "Swagger", "Swashbuckle", "SwaggerGen",
  "AddSwaggerGen", "API documentation", "JWT in Swagger", "XML comments",
  "operation filter", "versioned docs", or "Swagger UI".
---

# OpenAPI (Swashbuckle — .NET 8)

## Core Principles

1. **Swashbuckle is the standard for .NET 8** — `Swashbuckle.AspNetCore` is the default OpenAPI library for .NET 8 LTS. The .NET 10 built-in `Microsoft.AspNetCore.OpenApi` is NOT available in .NET 8.
2. **XML docs feed Swagger** — Enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` and reference the XML file in `AddSwaggerGen`. Every controller action and model property gets docs.
3. **JWT security scheme on every secured endpoint** — Add a `SecurityRequirementsOperationFilter` so Swagger UI shows the lock icon and allows test calls with a Bearer token.
4. **Version per document** — With `Asp.Versioning`, create one Swashbuckle document per API version (`v1`, `v2`). Never serve all versions in a single document.

## Patterns

### Basic Setup

```csharp
// Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "API documentation"
    });

    // XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

// ...
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

```xml
<!-- .csproj -->
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

### JWT Bearer Security Scheme

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token. Example: eyJhbGci..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

### Versioned Documents with Asp.Versioning

```csharp
// ConfigureSwaggerOptions.cs (IConfigureOptions<SwaggerGenOptions>)
public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title   = $"My API {description.ApiVersion}",
                Version = description.ApiVersion.ToString()
            });
        }
    }
}

// Program.cs
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen();

// SwaggerUI shows all versions
app.UseSwaggerUI(options =>
{
    foreach (var desc in app.DescribeApiVersions())
        options.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json", desc.GroupName);
});
```

### XML-Documented Controller Action

```csharp
/// <summary>Gets a document by ID.</summary>
/// <param name="id">Document identifier.</param>
/// <response code="200">Returns the document.</response>
/// <response code="404">Document not found.</response>
[HttpGet("{id:guid}")]
[Authorize(Policy = "document:read")]
[ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
{
    var result = await _sender.Send(new GetDocumentByIdQuery(id), ct);
    return result is not null ? Ok(result) : NotFound();
}
```

## Anti-patterns

```csharp
// DON'T — use .NET 10 built-in OpenAPI on .NET 8 (not available)
builder.Services.AddOpenApi();   // WRONG for .NET 8
app.MapOpenApi();                // WRONG for .NET 8

// DO — Swashbuckle for .NET 8
builder.Services.AddSwaggerGen();
app.UseSwagger();
app.UseSwaggerUI();
```

```csharp
// DON'T — skip GenerateDocumentationFile
// Results in no XML comments in Swagger UI

// DO — enable in .csproj
// <GenerateDocumentationFile>true</GenerateDocumentationFile>
// <NoWarn>$(NoWarn);1591</NoWarn>
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| .NET 8 LTS API | Swashbuckle.AspNetCore |
| JWT auth in Swagger UI | AddSecurityDefinition + AddSecurityRequirement |
| Multiple API versions | One SwaggerDoc per version + ConfigureSwaggerOptions |
| Richer Swagger UI | Swagger UI (default) or Scalar as alternative |
| XML documentation | GenerateDocumentationFile + IncludeXmlComments |
