# KhaosKode.Web.Authorization

Role semantics for ASP.NET Core without hand-rolling policies. KhaosKode.Web.Authorization ships two complementary authorization modes, complete documentation, and a runnable sample so you can harden APIs faster.

## Why Choose Khaos?
- **Two battle-tested modes** – Attribute-based decorators for per-endpoint clarity, plus dynamic middleware for centrally managed rules.
- **Condition-aware** – Plug in custom business predicates (business hours, tenant checks, headers) per rule.
- **NuGet-ready docs** – Specs, developer notes, and user guides are bundled into the package for downstream teams.
- **Scripts + coverage** – PowerShell helpers handle clean/build/test/coverage so CI/CD is trivial.

## Installation

```powershell
dotnet add package KhaosKode.Web.Authorization
```

> Targets .NET 9 (works on .NET 8+). Symbols and docs ship with every release.

## Attribute Mode (Decorators)

Perfect for teams that prefer authorization close to controllers.

```csharp
[RolesAuthorize(RoleRuleKind.AnyOf, new[] { "Admin", "Support" })]
public IActionResult AdminOrSupport() => Ok();

[RolesAuthorize(RoleRuleKind.AnyOf, new[] { "User", "Admin" }, typeof(BusinessHoursCondition))]
public IActionResult BusinessHoursOnly() => Ok();
```

**Benefits**
- Readable by default – each endpoint advertises its access semantics.
- Conditions resolved via DI, so business logic stays testable and reusable.
- Honors ASP.NET Core filters, logging, diagnostics.

**Risks / Things to watch**
- Forgetting to register a condition type returns `403`; add every `IContextAuthorizationCondition` to DI.
- Attribute mode still requires standard authentication (`AddAuthentication`) – anonymous users are auto-denied.

## Dynamic Mode (Rule Store + Middleware)

Ideal when security needs to be data-driven (database/config-driven) or shared across services.

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

builder.Services
	.AddRoleAuthorizationCore()
	.AddDynamicMethodAuthorization(rules);

app.UseAuthentication();
app.UseDynamicMethodAuthorization();
app.UseAuthorization();
```

**Benefits**
- Centralized control – update role requirements without recompiling controllers.
- Multiple rules per action (all must pass) for layered security.
- Works alongside attribute mode; mix and match per endpoint.

**Risks / Things to watch**
- No rule defined? Request is allowed (by design). Cover every sensitive action with explicit rules.
- Conditions run inside the request pipeline; ensure they’re fast and handle cancellation.

## Sample API & Documentation

Clone the repo and run:

```powershell
dotnet run --project src/KhaosKode.Web.Authorization.Sample/KhaosKode.Web.Authorization.Sample.csproj
```

Swagger UI demonstrates every semantics combination. The sample also provides JWT issuance endpoints so you can test role combinations quickly.

Documentation lives under `docs/` (also packed into the NuGet package):

- `docs/Specification.md` – full requirements.
- `docs/DeveloperGuide.md` – build/test instructions, scripts, extensibility notes.
- `docs/UserGuide.md` – how to run the sample, issue tokens, and probe endpoints.
- `docs/Versioning.md` – MinVer & release workflow (tags like `KhaosKode.Web.Authorization/v1.2.0`).

## Build, Test, Coverage

PowerShell helpers keep the workflow consistent on every machine:

```powershell
./scripts/Clean.ps1            # dotnet clean + removes artifacts/TestResults
./scripts/Build.ps1            # restore + build (Release)
./scripts/Test.ps1             # full test suite (unit + integration)
./scripts/Test-Coverage.ps1    # dotnet test --collect + ReportGenerator HTML/Cobertura
```

Coverage artifacts land in `TestResults/coverage`, while NuGet packages drop into `artifacts/packages`.

## Contributing & Support

Issues and PRs are welcome! Please include reproduction steps and reference the spec section you’re addressing. For feature requests, describe whether the need fits attribute mode, dynamic mode, or both.

Licensed under MIT (see `LICENSE.md`).
