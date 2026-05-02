
# 3. Concrete Technical Specification – Multi-API Integration Framework

> **Scope:** This document defines the exact technical contracts needed to implement:
>
> * The `KoreForge.RestApi.External.*`, `KoreForge.RestApi.Domain.*`, `KoreForge.RestApi.Internal.*`, and `KoreForge.RestApi.Client.*` libraries
> * Roslyn analyzers enforcing architecture rules
> * EF Core audit schema
> * NGINX config generator
> * `dotnet new` / repo           
> * CI/CD pipelines and versioning

All names use a placeholder **`<ApiName>`** – e.g. `KoreForge.RestApi.External.Foo`, `KoreForge.RestApi.Domain.Foo`, etc.

---

## 1. Solution & Project Layout (Mono-Repo)

```text
/ApiGatewayFramework
  /src
    /KoreForge.RestApi.KoreForge.RestApi.Common.Abstractions
    /KoreForge.RestApi.KoreForge.RestApi.Common.Observability
    /KoreForge.RestApi.KoreForge.RestApi.Common.Persistence
    /KoreForge.RestApi.KoreForge.RestApi.Common.Analyzers
    /KoreForge.RestApi.External.<ApiName>
    /KoreForge.RestApi.Domain.<ApiName>
    /KoreForge.RestApi.Internal.<ApiName>
    /KoreForge.RestApi.Client.<ApiName>
    /KoreForge.RestApi.Host.Internal
  /templates
    /dotnet-new-external
    /dotnet-new-domain
    /dotnet-new-internal
    /dotnet-new-client
  /tools
    /KoreForge.RestApi.NginxConfigGen
  /.editorconfig
  /Directory.Build.props
  /Directory.Build.targets
  /.github/workflows
    /ci.yml
    /publish-nuget.yml
```

Constraints:

* All projects target **`net9.0`** (or `net8.0` if you decide later).
* Warnings as errors by default.
* Nullable reference types enabled.
* Roslyn analyzers enabled solution-wide via `Directory.Build.props`.

---

## 2. OpenAPI Expectations & External Layer Contracts

### 2.1 OpenAPI Requirements

For each external provider, you must have a machine-readable OpenAPI spec, either:

* `openapi.json` in `KoreForge.RestApi.External.<ApiName>/openapi/openapi.json`, or
* `openapi.yaml` in `KoreForge.RestApi.External.<ApiName>/openapi/openapi.yaml`.

The spec:

* MUST describe only the subset of endpoints you intend to support.
* SHOULD be pruned to avoid huge surfaces:

  * Only resources used by your product
  * Only stable/GA operations

### 2.2 DTO & Refit Generation Rules

Generated code lives under `KoreForge.RestApi.External.<ApiName>`:

* All **DTOs and Refit interfaces are `internal`**.
* Namespace: `KoreForge.RestApi.External.<ApiName>.Generated`.
* DTO naming: PascalCase, 1:1 with schema object IDs (or a stable, deterministic mapping).
* Enum values: PascalCase names, `string` or `int` backing according to OpenAPI.
* Every Refit operation:

```csharp
namespace KoreForge.RestApi.External.<ApiName>.Generated;

using System.Threading;
using System.Threading.Tasks;
using Refit;

internal interface I<ApiName>RefitClient
{
    [Get("/v1/resources/{id}")]
    Task<ApiResponse<ResourceResponseDto>> GetResourceAsync(
        string id,
        CancellationToken cancellationToken = default);
}
```

Rules:

* Return type is always `Task<ApiResponse<T>>` or `Task<ApiResponse>` for no body.
* **`CancellationToken` is the last parameter** and must be present on every method.
* All DTOs are **immutable** (init-only properties) to simplify mapping:

```csharp
internal sealed class ResourceResponseDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; }
}
```

### 2.3 External Layer DI Registration

`KoreForge.RestApi.External.<ApiName>` exposes a single extension method:

```csharp
namespace KoreForge.RestApi.External.<ApiName>;

using KoreForge.RestApi.External.<ApiName>.Generated;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

public static class External<ApiName>ServiceCollectionExtensions
{
    public static IServiceCollection Add<ApiName>External(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        const string httpClientName = "<ApiName>External";

        var section = configuration.GetSection("ExternalApis:<ApiName>");
        services.Configure<ApiNameExternalOptions>(section);

        services.AddRefitClient<I<ApiName>RefitClient>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp
                    .GetRequiredService<IOptions<ApiNameExternalOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            });

        return services;
    }
}

public sealed class ApiNameExternalOptions
{
    public string BaseUrl { get; init; } = default!;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}
```

Config schema (host app):

```jsonc
{
  "ExternalApis": {
    "<ApiName>": {
      "BaseUrl": "https://api.provider.com",
      "Timeout": "00:00:30"
    }
  }
}
```

---

## 3. Domain Layer Contract (`KoreForge.RestApi.Domain.<ApiName>`)

### 3.1 Public Domain Interfaces

The domain layer hides the external DTOs and exposes **domain models and use cases**.

Example:

```csharp
namespace KoreForge.RestApi.Domain.<ApiName>.Models;

public sealed class <ApiName>Case
{
    public string Id { get; init; } = default!;
    public string Title { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; }
    public string Provider { get; init; } = default!;
}
```

Use case interface:

```csharp
namespace KoreForge.RestApi.Domain.<ApiName>.UseCases;

public interface IGet<ApiName>CaseUseCase
{
    Task<<ApiName>Case> ExecuteAsync(
        string id,
        CancellationToken cancellationToken = default);
}
```

Implementation depends on:

* `I<ApiName>RefitClient` (from `KoreForge.RestApi.External.<ApiName>`)
* `IApiAuditRepository` (from `KoreForge.RestApi.KoreForge.RestApi.Common.Persistence`)
* Logging and tracing abstractions from `KoreForge.RestApi.KoreForge.RestApi.Common.Observability`.

```csharp
namespace KoreForge.RestApi.Domain.<ApiName>.UseCases;

using KoreForge.RestApi.Common.Observability;
using KoreForge.RestApi.Common.Persistence;
using KoreForge.RestApi.External.<ApiName>.Generated;

internal sealed class Get<ApiName>CaseUseCase : IGet<ApiName>CaseUseCase
{
    private readonly I<ApiName>RefitClient _client;
    private readonly IApiAuditRepository _auditRepository;
    private readonly ITracer _tracer;

    public Get<ApiName>CaseUseCase(
        I<ApiName>RefitClient client,
        IApiAuditRepository auditRepository,
        ITracer tracer)
    {
        _client = client;
        _auditRepository = auditRepository;
        _tracer = tracer;
    }

    public async Task<<ApiName>Case> ExecuteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        using var span = _tracer.StartSpan("<ApiName>.GetCase");

        var response = await _client.GetCaseAsync(id, cancellationToken);

        await _auditRepository.SaveAsync(
            new ApiCallAudit
            {
                ApiName = "<ApiName>",
                Operation = "GetCase",
                StatusCode = (int)response.StatusCode,
                CorrelationId = span.CorrelationId,
                RequestPayload = null,  // or captured separately
                ResponsePayload = response.Content is null
                    ? null
                    : JsonSerializer.Serialize(response.Content),
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var dto = response.Content!;
        return new <ApiName>Case
        {
            Id = dto.Id,
            Title = dto.Title,
            CreatedAt = dto.CreatedAt,
            Provider = "<ApiName>"
        };
    }
}
```

### 3.2 Domain Layer DI Extension

```csharp
namespace KoreForge.RestApi.Domain.<ApiName>;

using KoreForge.RestApi.Domain.<ApiName>.UseCases;
using Microsoft.Extensions.DependencyInjection;

public static class Domain<ApiName>ServiceCollectionExtensions
{
    public static IServiceCollection Add<ApiName>Domain(this IServiceCollection services)
    {
        services.AddScoped<IGet<ApiName>CaseUseCase, Get<ApiName>CaseUseCase>();
        // Register other use cases here
        return services;
    }
}
```

---

## 4. Internal Layer Contract (`KoreForge.RestApi.Internal.<ApiName>`)

The internal layer hosts **your HTTP API** for channels like mobile apps, other internal services, etc.

### 4.1 ASP.NET Core Minimal API

The project is a **class library**, not a host. It exposes route registration:

```csharp
namespace KoreForge.RestApi.Internal.<ApiName>;

using KoreForge.RestApi.Domain.<ApiName>.UseCases;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public static class <ApiName>EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder Map<ApiName>Endpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/<apiname>"); // lowercase path

        group.MapGet("/cases/{id}", async (
                string id,
                IGet<ApiName>CaseUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("<ApiName>GetCase")
            .WithOpenApi();

        return endpoints;
    }
}
```

### 4.2 Internal Host

`KoreForge.RestApi.Host.Internal` is the only **real host**:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCommonInfrastructure(builder.Configuration)
    .Add<ApiName>External(builder.Configuration)
    .Add<ApiName>Domain();

var app = builder.Build();

app.Map<ApiName>Endpoints();
// Map endpoints for all APIs here

app.Run();
```

---

## 5. Client Layer Contract (`KoreForge.RestApi.Client.<ApiName>`)

This is the **SDK** for other internal services.

* Public DTOs and client interfaces.
* It calls Internal APIs over HTTP (NOT directly `External`).

```csharp
namespace KoreForge.RestApi.Client.<ApiName>;

public sealed class <ApiName>ClientOptions
{
    public string BaseUrl { get; init; } = default!;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}
```

```csharp
namespace KoreForge.RestApi.Client.<ApiName>;

public interface I<ApiName>Client
{
    Task<<ApiName>CaseDto> GetCaseAsync(
        string id,
        CancellationToken cancellationToken = default);
}

public sealed class <ApiName>CaseDto
{
    public string Id { get; init; } = default!;
    public string Title { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; }
}
```

Implementation uses `HttpClient`:

```csharp
namespace KoreForge.RestApi.Client.<ApiName>;

internal sealed class <ApiName>Client : I<ApiName>Client
{
    private readonly HttpClient _httpClient;

    public <ApiName>Client(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<<ApiName>CaseDto> GetCaseAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/<apiname>/cases/{id}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var dto = await response.Content
            .ReadFromJsonAsync<<ApiName>CaseDto>(cancellationToken: cancellationToken);

        return dto!;
    }
}
```

DI extension:

```csharp
namespace KoreForge.RestApi.Client.<ApiName>;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

public static class <ApiName>ClientServiceCollectionExtensions
{
    public static IServiceCollection Add<ApiName>Client(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<<ApiName>ClientOptions>(
            configuration.GetSection("InternalApis:<ApiName>"));

        services.AddHttpClient<I<ApiName>Client, <ApiName>Client>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<<ApiName>ClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = options.Timeout;
        });

        return services;
    }
}
```

---

## 6. EF Core Models & Auditing Schema

All audit persistence lives in `KoreForge.RestApi.KoreForge.RestApi.Common.Persistence`.

### 6.1 EF Entity: `ApiCallAudit`

```csharp
namespace KoreForge.RestApi.Common.Persistence.Entities;

public sealed class ApiCallAudit
{
    public long Id { get; set; }

    public string ApiName { get; set; } = default!;
    public string Operation { get; set; } = default!;
    public string Direction { get; set; } = default!; // "Outbound"|"Inbound"
    public int StatusCode { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public string? CorrelationId { get; set; }
    public DateTimeOffset RequestTimestampUtc { get; set; }
    public DateTimeOffset ResponseTimestampUtc { get; set; }
    public long DurationMs { get; set; }

    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }

    public string? HttpMethod { get; set; }
    public string? RequestPath { get; set; }
    public string? CallerSystem { get; set; }
}
```

### 6.2 DbContext

```csharp
namespace KoreForge.RestApi.Common.Persistence;

using KoreForge.RestApi.Common.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class ApiGatewayDbContext : DbContext
{
    public DbSet<ApiCallAudit> ApiCallAudits => Set<ApiCallAudit>();

    public ApiGatewayDbContext(DbContextOptions<ApiGatewayDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var audit = modelBuilder.Entity<ApiCallAudit>();

        audit.ToTable("ApiCallAudit", "gateway");

        audit.HasKey(x => x.Id);

        audit.Property(x => x.ApiName).IsRequired().HasMaxLength(100);
        audit.Property(x => x.Operation).IsRequired().HasMaxLength(200);
        audit.Property(x => x.Direction).IsRequired().HasMaxLength(20);
        audit.Property(x => x.StatusCode).IsRequired();

        audit.Property(x => x.CorrelationId).HasMaxLength(64);
        audit.Property(x => x.ErrorCode).HasMaxLength(100);
        audit.Property(x => x.ErrorMessage).HasMaxLength(2000);

        audit.Property(x => x.RequestTimestampUtc).IsRequired();
        audit.Property(x => x.ResponseTimestampUtc).IsRequired();

        audit.HasIndex(x => x.ApiName);
        audit.HasIndex(x => x.CorrelationId);
        audit.HasIndex(x => new { x.ApiName, x.Operation, x.RequestTimestampUtc });
    }
}
```

### 6.3 Migration Example (first migration)

> This is representative; you’ll generate it via `dotnet ef migrations add InitialGatewayAudit`.

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.EnsureSchema(name: "gateway");

    migrationBuilder.CreateTable(
        name: "ApiCallAudit",
        schema: "gateway",
        columns: table => new
        {
            Id = table.Column<long>()
                .Annotation("SqlServer:Identity", "1, 1"),
            ApiName = table.Column<string>(maxLength: 100, nullable: false),
            Operation = table.Column<string>(maxLength: 200, nullable: false),
            Direction = table.Column<string>(maxLength: 20, nullable: false),
            StatusCode = table.Column<int>(nullable: false),
            ErrorCode = table.Column<string>(maxLength: 100, nullable: true),
            ErrorMessage = table.Column<string>(maxLength: 2000, nullable: true),
            CorrelationId = table.Column<string>(maxLength: 64, nullable: true),
            RequestTimestampUtc = table.Column<DateTimeOffset>(nullable: false),
            ResponseTimestampUtc = table.Column<DateTimeOffset>(nullable: false),
            DurationMs = table.Column<long>(nullable: false),
            RequestPayload = table.Column<string>(nullable: true),
            ResponsePayload = table.Column<string>(nullable: true),
            HttpMethod = table.Column<string>(maxLength: 10, nullable: true),
            RequestPath = table.Column<string>(maxLength: 500, nullable: true),
            CallerSystem = table.Column<string>(maxLength: 100, nullable: true)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_ApiCallAudit", x => x.Id);
        });

    migrationBuilder.CreateIndex(
        name: "IX_ApiCallAudit_ApiName",
        schema: "gateway",
        table: "ApiCallAudit",
        column: "ApiName");

    migrationBuilder.CreateIndex(
        name: "IX_ApiCallAudit_CorrelationId",
        schema: "gateway",
        table: "ApiCallAudit",
        column: "CorrelationId");

    migrationBuilder.CreateIndex(
        name: "IX_ApiCallAudit_ApiName_Operation_RequestTimestampUtc",
        schema: "gateway",
        table: "ApiCallAudit",
        columns: new[] { "ApiName", "Operation", "RequestTimestampUtc" });
}
```

---

## 7. Roslyn Analyzer Suite (`KoreForge.RestApi.KoreForge.RestApi.Common.Analyzers`)

> The IDs, categories, and messages below are **proposed** and can be adjusted before implementation.

### 7.1 Diagnostic List

| ID     | Category           | Severity | Summary                                                                 |
| ------ | ------------------ | -------- | ----------------------------------------------------------------------- |
| API001 | Architecture       | Error    | External layer types must be `internal`.                                |
| API002 | Architecture       | Error    | External layer must not reference Domain/Internal/Client projects.      |
| API003 | Architecture       | Error    | Domain layer must not reference Internal or Client.                     |
| API004 | Architecture       | Error    | Internal layer must not reference External directly.                    |
| API005 | HttpContract       | Error    | Refit methods in External must return `Task<ApiResponse<T>>`.           |
| API006 | HttpContract       | Error    | Refit methods in External must include a `CancellationToken` parameter. |
| API007 | HttpContract       | Warning  | HTTP client usage outside approved locations.                           |
| API010 | TemplateCompliance | Warning  | Project missing required assembly attributes/options.                   |

### 7.2 Diagnostic Definition Example

**API001 – External types must be internal**

* **Title:** External types must be internal
* **Message:** `Type '{0}' in namespace '{1}' must be declared 'internal' in KoreForge.RestApi.External.* projects.`
* **Description:** Enforces that all publicly exposed types in `KoreForge.RestApi.External.*` are internal to prevent leaking provider DTOs outside the boundary.
* **Category:** `Architecture`
* **Severity:** `DiagnosticSeverity.Error`

Pseudo-code for the analyzer:

* Trigger on `ClassDeclarationSyntax`, `InterfaceDeclarationSyntax`, `RecordDeclarationSyntax` in assemblies whose **default namespace** starts with `External.`.
* If `decl.Modifiers` contains `public` → report diagnostic.
* Code fix: replace `public` modifier with `internal`.

**Unit test pattern** (xUnit):

```csharp
[Fact]
public async Task API001_External_Public_Class_Should_Report_Diagnostic()
{
    const string source = @"
namespace KoreForge.RestApi.External.Foo;

public class BadDto
{
}";

    var expected = Verify.Diagnostic("API001")
        .WithSpan(4, 14, 4, 20); // line/column of 'BadDto'

    await Verify.VerifyAnalyzerAsync(source, expected);
}
```

---

## 8. Shared Template Files

### 8.1 `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0-preview.1" />
    <PackageReference Include="Refit" Version="7.0.0" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.9.0" />
    <PackageReference Include="KoreForge.RestApi.KoreForge.RestApi.Common.Analyzers" Version="1.0.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

*(Versions are placeholders – you’ll pick real ones.)*

### 8.2 `Directory.Build.targets`

```xml
<Project>
  <Target Name="EnsureAnalyzerRules" BeforeTargets="CoreCompile">
    <Message Text="Running common analyzer rules..." Importance="Low" />
  </Target>
</Project>
```

### 8.3 Example `.csproj` – `KoreForge.RestApi.External.<ApiName>`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>KoreForge.RestApi.External.<ApiName></RootNamespace>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Refit" Version="7.0.0" />
  </ItemGroup>

</Project>
```

Similar minimal csproj files for Domain, Internal, Client – all inherit common settings from `Directory.Build.props`.

---

## 9. NGINX Config Generator

### 9.1 Input JSON Schema

**File:** `/tools/KoreForge.RestApi.NginxConfigGen/config/apis.json`

```jsonc
{
  "apis": [
    {
      "name": "<ApiName>",
      "pathPrefix": "/api/<apiname>",
      "upstream": {
        "serviceName": "host-internal",
        "port": 8080
      },
      "rateLimit": {
        "enabled": true,
        "requestsPerMinute": 600
      },
      "timeouts": {
        "connectSeconds": 5,
        "sendSeconds": 60,
        "readSeconds": 60
      },
      "methods": [ "GET", "POST", "PUT", "DELETE" ],
      "auth": {
        "forwardHeaders": [ "Authorization", "X-Correlation-Id" ]
      }
    }
  ]
}
```

**C# model in `KoreForge.RestApi.NginxConfigGen`:**

```csharp
public sealed class NginxConfigRoot
{
    public List<ApiConfig> Apis { get; init; } = new();
}

public sealed class ApiConfig
{
    public string Name { get; init; } = default!;
    public string PathPrefix { get; init; } = default!;
    public UpstreamConfig Upstream { get; init; } = new();
    public RateLimitConfig RateLimit { get; init; } = new();
    public TimeoutConfig Timeouts { get; init; } = new();
    public IReadOnlyList<string> Methods { get; init; } = Array.Empty<string>();
    public AuthConfig Auth { get; init; } = new();
}

public sealed class UpstreamConfig
{
    public string ServiceName { get; init; } = default!;
    public int Port { get; init; }
}

public sealed class RateLimitConfig
{
    public bool Enabled { get; init; }
    public int RequestsPerMinute { get; init; }
}

public sealed class TimeoutConfig
{
    public int ConnectSeconds { get; init; }
    public int SendSeconds { get; init; }
    public int ReadSeconds { get; init; }
}

public sealed class AuthConfig
{
    public IReadOnlyList<string> ForwardHeaders { get; init; } = Array.Empty<string>();
}
```

### 9.2 NGINX Template (Output)

For each API, generator emits something like:

```nginx
upstream <apiname>_upstream {
    server host-internal:8080;
}

server {
    listen 80;
    server_name gateway.local;

    location /api/<apiname>/ {
        proxy_pass         http://<apiname>_upstream;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   X-Correlation-Id $request_id;

        proxy_connect_timeout   5s;
        proxy_send_timeout      60s;
        proxy_read_timeout      60s;

        limit_req zone=<apiname>_ratelimit burst=10 nodelay;
    }
}

limit_req_zone $binary_remote_addr zone=<apiname>_ratelimit:10m rate=600r/m;
```

The generator writes to `./artifacts/nginx/<apiname>.conf`.

---

## 10. `dotnet new` Template Content

Each template folder under `/templates` contains:

```text
/templates/dotnet-new-external
  /.template.config/template.json
  /KoreForge.RestApi.External.<ApiName>.csproj
  /openapi/openapi.json
  /Generated/Placeholder.cs
```

Example `template.json` for External:

```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "YourTeam",
  "classifications": [ "ExternalApi" ],
  "identity": "ApiGatewayFramework.External",
  "name": "External API project",
  "shortName": "external-api",
  "sourceName": "External.ApiTemplate",
  "preferNameDirectory": true,
  "symbols": {
    "ApiName": {
      "type": "parameter",
      "description": "The name of the API",
      "datatype": "text"
    }
  }
}
```

Similar structure for `domain-api`, `internal-api`, `client-api` templates, each pre-wired with:

* `ServiceCollectionExtensions.cs`
* A placeholder use case / endpoint / client class
* References to `KoreForge.RestApi.Common.*` packages

---

## 11. CI/CD Pipelines & Versioning

### 11.1 CI – Build & Test (`.github/workflows/ci.yml`)

* Trigger: `pull_request`, `push` to `main`.
* Steps:

  1. Checkout
  2. `dotnet restore`
  3. `dotnet build --configuration Release`
  4. `dotnet test --configuration Release`
  5. `dotnet format --verify-no-changes`
  6. Generate Internal OpenAPI spec from `KoreForge.RestApi.Host.Internal` (for docs).

Skeleton:

```yaml
name: CI

on:
  push:
    branches: [ main ]
  pull_request:

jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - run: dotnet restore
      - run: dotnet build --configuration Release --no-restore
      - run: dotnet test --configuration Release --no-build
      - run: dotnet format --verify-no-changes
      - run: dotnet tool restore
      - run: dotnet run --project ./tools/KoreForge.RestApi.NginxConfigGen/KoreForge.RestApi.NginxConfigGen.csproj
```

### 11.2 NuGet Publishing (`publish-nuget.yml`)

* Trigger: tag push `v*.*.*`.
* Packs and publishes:

  * `KoreForge.RestApi.KoreForge.RestApi.Common.Abstractions`
  * `KoreForge.RestApi.KoreForge.RestApi.Common.Observability`
  * `KoreForge.RestApi.KoreForge.RestApi.Common.Persistence`
  * `KoreForge.RestApi.KoreForge.RestApi.Common.Analyzers`
  * `KoreForge.RestApi.Client.*` (if you decide they are reusable libs)

```yaml
name: Publish NuGet

on:
  push:
    tags:
      - 'v*.*.*'

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - run: dotnet restore
      - run: dotnet pack src/KoreForge.RestApi.KoreForge.RestApi.Common.Abstractions/KoreForge.RestApi.KoreForge.RestApi.Common.Abstractions.csproj -c Release -o ./artifacts
      - run: dotnet pack src/KoreForge.RestApi.KoreForge.RestApi.Common.Analyzers/KoreForge.RestApi.KoreForge.RestApi.Common.Analyzers.csproj -c Release -o ./artifacts
      - name: Push packages
        run: dotnet nuget push "./artifacts/*.nupkg" --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

### 11.3 Versioning

* **Semantic Versioning** per package (`MAJOR.MINOR.PATCH`).
* Tag format: `v<major>.<minor>.<patch>` on repo.
* Analyzer ruleset changes that break builds → bump **MINOR** or **MAJOR** based on severity.

---

## 12. What Is Still Intentional Design vs. Fixed

* Analyzer IDs, messages, and exact rule set are **design proposals** here. You can adjust naming/wording before implementing.
* Exact NGINX options (timeouts, rate limits) will depend on your environment and non-functional requirements.
* Package versions in `Directory.Build.props` are placeholders; you’ll pick the real versions you want.

---

If you want, next step I can:

* Turn this into **actual template folders** (full contents for each `dotnet new` template), or
* Start from one concrete API (e.g. “Cases”) and generate the full External/Domain/Internal/Client set using this spec.

