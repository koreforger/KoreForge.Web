

Below is a copy‑paste LLM prompt template and a step‑by‑step SOP for creating a new API module. It locks the model into your required structure, enforces External‑layer rules, and outlines the full process—from scaffolding to NGINX config generation.

LLM Prompt (Template)

Use this prompt after you run the scaffold script and have a fresh KoreForge.RestApi.External.<ApiName> module created.

You are a senior C# engineer. Work ONLY inside the scaffolded project:

- KoreForge.RestApi.External.<APINAME> (Refit interfaces + DTOs)

- DO NOT change project structure, visibility, namespaces, or add new projects.

- DO NOT add retries/Polly. External is transport-only with hooks already wired.

- Ensure compatibility with analyzers: External types must be internal; Refit methods return Task<ApiResponse<T>>.

 

Context:

- API name: <<APINAME>>

- OpenAPI source: <<OPENAPIURLORINLINEJSON>>

- Options section in appsettings: "Apis:<<APINAME>>" with BaseUrl, TimeoutSeconds, UserAgent, BearerToken.

- System.Text.Json with camelCase policy.

- CancellationToken parameters are required for all methods.

 

Tasks:

1) Generate DTO classes from the OpenAPI schemas under:

    KoreForge.RestApi.External.<<APINAME>>/Models

   - Preserve required/optional members per schema; use C# types with nullability.

   - Map enums to C# enums; include [JsonConverter] only if necessary for string enums.

   - Add XML docs from schema descriptions (summary + remarks when present).

 

2) Implement Refit interface:

    KoreForge.RestApi.External.<<APINAME>>/IMyApi.cs

   - One method per operation in the OpenAPI (GET/POST/PUT/PATCH/DELETE).

   - Use Refit attributes: [Get], [Post], [Put], [Patch], [Delete], [Head], [Options].

   - Method signature MUST be: Task<ApiResponse<ReturnDto>> MethodName(args…, CancellationToken ct = default).

   - For operations with no body (204, etc.), return ApiResponse<Unit> (or ApiResponse<object> if Unit not available).

   - Correctly map query parameters, route segments, headers, and body payloads.

 

3) Keep all External code 'internal'. Do NOT make the interface or DTOs public.

4) Do NOT add business logic, guard checks, EF, or retries here. ONLY transport surface + models.

5) Add XML docs for each interface method including summaries and response codes from the spec.

6) Compile mentally and SELF-CHECK against these gates:

   - All methods return Task<ApiResponse<T>>.

   - Every method has a CancellationToken.

   - No use of Polly or AddPolicyHandler.

    - Namespace starts with KoreForge.RestApi.External.<<APINAME>>.

   - Types and interfaces are internal.

   - DTO property names align with System.Text.Json camelCase.

 

Output:

- Provide the COMPLETE updated contents of:

    - KoreForge.RestApi.External.<<APINAME>>/Models/*.cs

    - KoreForge.RestApi.External.<<APINAME>>/IMyApi.cs

- Do not output any other files.

Variants:

    If the OpenAPI is huge, scope the prompt to a subset of paths (e.g., “only /cases, /comments and /health”), and run multiple passes.

Standard Operating Procedure (SOP) – New API Module

Use this checklist whenever you add an API (repeatable across 20–50 APIs).

0) Pre‑requisites

    Ensure the template pack is installed (dotnet new install Company.ApiModule.Templates::1.0.0).
    Confirm shared governance exists in the repo: Directory.Build.props/targets, analyzers, architecture tests, and OpenTelemetry baseline.

1) Scaffold the module

# PowerShell

./scr/new-api.ps1 -ApiName NedCase -DbSchema Audit -TableMode Single -EnableAuditing $true

 

# or Bash

./scr/new-api.sh NedCase Audit Single true

This creates:

    src/KoreForge.RestApi.External.NedCase/ (internal Refit layer, hooks handler already wired)
    src/KoreForge.RestApi.Domain.NedCase/ (orchestration + EF audit stubs with {{DB_SCHEMA}})
    src/KoreForge.RestApi.Internal.NedCase/ (controller/minimal APIs)
    src/KoreForge.RestApi.Client.NedCase/ (SDK)
    tests/KoreForge.RestApi.Architecture.NedCase.Tests/ (layering checks)

2) Configure appsettings

Update Internal host appsettings.json:

{

  "ConnectionStrings": { "Audit": "Server=.;Database=AuditDb;Trusted_Connection=True;TrustServerCertificate=True" },

  "Apis": {

    "NedCase": {

      "BaseUrl": "https://provider.example.com",

      "TimeoutSeconds": 60,

      "UserAgent": "Company-NedCase/1.0",

      "BearerToken": "REDACTED"

    }

  },

  "Domains": {

    "NedCase": { "EnableAuditing": true }

  }

}

3) Run the LLM

Use the prompt template above with the OpenAPI source.\ Outputs must be limited to:

    KoreForge.RestApi.External.NedCase/Models/*.cs
    KoreForge.RestApi.External.NedCase/IMyApi.cs

Analyzers will fail the build if External types are public or methods return Task<T> instead of Task<ApiResponse<T>>.

4) Build & quick test

dotnet restore

dotnet build -warnaserror

dotnet test tests/KoreForge.RestApi.Architecture.NedCase.Tests

Fix any analyzer violations immediately (visibility, return types, no Polly).

5) Implement Domain orchestration & auditing

    Fill KoreForge.RestApi.Domain.NedCase/Services/NedCaseService.cs methods.
    Map Refit ApiResponse<T> → typed exceptions (ExternalSystemFaultException, etc.).
    Write redacted RequestJson/ResponseJson into per‑API audit tables.
    Enforce guards/predicates (e.g., whitelist logic) before calling External.

6) Expose Internal API

    Add curated endpoints in KoreForge.RestApi.Internal.NedCase controllers/minimal APIs.
    Return ProblemDetails with correlation id on errors.

7) Client SDK wiring

    Confirm KoreForge.RestApi.Client.NedCase calls KoreForge.RestApi.Internal.NedCase routes and surfaces nested error details.

8) NGINX config generation

    Update configs/nginx.servers.json (servers, algorithm, passive health).
    Build generates artifacts/nginx/<Config>/nginx.conf via Directory.Build.targets.
    Deploy/load‑balance your InternalHost using this file.

9) CI/CD gates

    Build with analyzers & architecture tests.
    Publish per‑API packages (Domain, Client).
    Deploy InternalHost.
    Archive nginx.conf.

Quality Gates (LLM Self‑Check List)

The model must ensure:

    External: all types internal; IMyApi methods return Task<ApiResponse<T>>; every method has CancellationToken; no Polly; correct Refit attribute usage.
    DTOs: schema‑accurate types with nullability; enums mapped; System.Text.Json camelCase property names.
    No structural changes: do NOT add projects or modify DI extensions beyond External intent.

Quick Example (LLM instruction snippet)

Implement only these OpenAPI paths for <<APINAME>>:

- POST /cases

- GET  /cases/{id}

- POST /cases/{id}/comments

 

Use DTOs: CreateCaseRequest, CreateCaseResponse, GetCaseResponse, CreateCommentRequest, CreateCommentResponse.

Place them under KoreForge.RestApi.External.<<APINAME>>/Models.

Update KoreForge.RestApi.External.<<API_NAME>>/IMyApi.cs with 3 methods:

- Task<ApiResponse<CreateCaseResponse>> CreateCase([Body] CreateCaseRequest req, CancellationToken ct = default)

- Task<ApiResponse<GetCaseResponse>> GetCase(string id, CancellationToken ct = default)

- Task<ApiResponse<CreateCommentResponse>> CreateComment(string id, [Body] CreateCommentRequest req, CancellationToken ct = default)

All code must be internal; add XML docs with response codes 200/201/400/404/500 from the spec.

Notes & Pitfalls

    Do not put retries (Polly) into External; resilience belongs in Domain/Client if needed.
    Always include CancellationToken in Refit method signatures.
    Keep External non‑public: InternalsVisibleTo already configured for Domain access.
    Redact secrets/PII before storing audit JSON.
    For GETs, prefer Minimal audit unless sensitive payloads.

Acceptance Criteria (per API)

    External compiles with analyzer rules (internal types; ApiResponse; no Polly).
    Domain maps all non‑2xx to typed exceptions and records audit with correlation id.
    Internal returns ProblemDetails with correlation id.
    Client invokes Internal and surfaces nested error context.
    nginx.conf generated and deployable.


