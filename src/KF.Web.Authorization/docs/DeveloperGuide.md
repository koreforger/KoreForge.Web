# KhaosKode.Web.Authorization Developer Guide

## Solution Overview

The repository is split into three major areas:

| Project | Description |
| --- | --- |
| `src/KhaosKode.Web.Authorization` | Core authorization library (attribute + dynamic rule engines) published as the NuGet package. |
| `src/KhaosKode.Web.Authorization.Sample` | ASP.NET Core sample API used for manual experimentation and docs demos. |
| `tests/KhaosKode.Web.Authorization.Tests` | Central xUnit test project (unit + integration) exercising both the core library and the sample API. |

Shared build metadata (versioning, package assets, documentation inclusion) lives in `Directory.Build.props` and flows to every packable project.

## Building and Testing

```powershell
# Clean, build, and test using helper scripts
PS> ./scripts/Clean.ps1
PS> ./scripts/Build.ps1
PS> ./scripts/Test.ps1

# Run coverage-enabled tests + HTML report
PS> ./scripts/Test-Coverage.ps1

# Manual dotnet commands remain available if you prefer
PS> dotnet pack src/KhaosKode.Web.Authorization/KhaosKode.Web.Authorization.csproj -c Release
```

Artifacts are written to `artifacts/packages` (NuGet) and `TestResults` (test/coverage output). `Test-Coverage.ps1` pipes Coverlet output into ReportGenerator, producing HTML + Cobertura reports under `TestResults/coverage`.

## Attribute Mode (RolesAuthorize)

1. Add the service registrations inside your ASP.NET Core host:

   ```csharp
   builder.Services.AddRoleAuthorizationCore();
   builder.Services.AddScoped<BusinessHoursCondition>();
   ```

2. Decorate controllers/actions with `[RolesAuthorize]`:

   ```csharp
   [RolesAuthorize(RoleRuleKind.AnyOf, new[] { "Admin", "Support" })]
   public IActionResult AdminOrSupport() => Ok();

   [RolesAuthorize(RoleRuleKind.AnyOf, new[] { "User", "Admin" }, typeof(BusinessHoursCondition))]
   public IActionResult BusinessHoursOnly() => Ok();
   ```

3. Any `conditionType` argument must be registered with DI and implement `IContextAuthorizationCondition`. Conditions are evaluated only after the role rule succeeds.

## Dynamic Mode (Middleware + Rule Store)

1. Build a set of `MethodPermissionRule` entries that map a controller action to its role semantics and optional condition:

   ```csharp
   var rules = new List<MethodPermissionRule>
   {
       new(
           typeFullName: typeof(DynamicDemoController).FullName!,
           methodName: nameof(DynamicDemoController.CreateOrder),
           ruleKind: RoleRuleKind.AllOf,
           roles: new[] { "Admin", "Sales" },
           condition: static (ctx, user, ct) =>
           {
               var headerValue = ctx.Request.Headers["X-Request-Source"].ToString();
               return ValueTask.FromResult(
                   string.Equals(headerValue, "Internal", StringComparison.OrdinalIgnoreCase));
           })
   };
   ```

2. Register the rule store + evaluator and insert the middleware:

   ```csharp
   builder.Services
       .AddRoleAuthorizationCore()
       .AddDynamicMethodAuthorization(rules);

   app.UseAuthentication();
   app.UseDynamicMethodAuthorization();
   app.UseAuthorization();
   ```

3. The middleware derives the controller/method metadata from the `ControllerActionDescriptor`. Endpoints without rules remain open.

## Adding Custom Conditions

* Attribute mode conditions implement `IContextAuthorizationCondition`.
* Dynamic mode conditions are inline `PermissionConditionDelegate` instances.
* Use `TimeProvider` or other injectable services instead of `DateTime.Now` to keep logic testable (see `BusinessHoursCondition`).

## Testing Guidance

* Add unit tests for new role semantics or condition helpers inside `tests/KhaosKode.Web.Authorization.Tests`.
* Integration tests should reuse `WebApplicationFactory<Program>` to run the sample API and hit the actual HTTP pipeline.
* Coverage tooling will be wired via the upcoming PowerShell scripts; meanwhile you can gather coverage with `dotnet test --collect:"XPlat Code Coverage"`.

## Documentation Sources

* `docs/Specification.md` — requirements document
* `docs/DeveloperGuide.md` — this guide
* `docs/UserGuide.md` — walkthrough for running the sample API
* `docs/Versioning.md` — explains MinVer + release cadence

All files under `docs/` are added to the NuGet package under `buildTransitive/docs/` so downstream consumers can open them from the package contents.
