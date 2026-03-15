# KhaosKode.Web.Authorization User Guide

This guide walks through running the sample API, issuing JWTs, and exercising both attribute-based and dynamic authorization scenarios.

## Prerequisites

* .NET 9 SDK
* PowerShell 5.1+ (bundled on Windows)
* (Optional) `httprepl`, `curl`, or any REST client

## Running the Sample API

```powershell
PS> dotnet run --project src/KhaosKode.Web.Authorization.Sample/KhaosKode.Web.Authorization.Sample.csproj
```

The app hosts Swagger UI at `https://localhost:7242/swagger` (port varies). HTTPS redirection is enabled, so prefer the `https://` endpoint.

## Issuing Tokens

1. POST to `/auth/token` with a JSON payload:

   ```http
   POST https://localhost:7242/auth/token
   Content-Type: application/json

   {
     "userName": "alice",
     "roles": ["Admin", "Supervisor"],
     "additionalClaims": {
       "tenant_id": "contoso"
     }
   }
   ```

2. The response includes `access_token` and `expires_in` (3600 seconds). Copy the token for subsequent calls.

## Attribute Demo Endpoints

| Endpoint | Rule | Notes |
| --- | --- | --- |
| `GET /api/attr/admin-or-support` | `AnyOf(Admin, Support)` | Requires Admin **or** Support role. |
| `GET /api/attr/admin-and-supervisor` | `AllOf(Admin, Supervisor)` | Requires both roles. |
| `GET /api/attr/everyone-except-suspended` | `NotAnyOf(Suspended)` | Denies only Suspended users. |
| `GET /api/attr/not-trader-and-auditor` | `NotAllOf(Trader, Auditor)` | Denies the exact Trader+Auditor combo. |
| `GET /api/attr/business-hours-only` | `AnyOf(User, Admin)` + `BusinessHoursCondition` | Allows during 08:00–17:00 UTC. |

### Calling From curl

```powershell
$token = '<copy JWT>'

curl https://localhost:7242/api/attr/admin-or-support ^
  -H "Authorization: Bearer $token"
```

The business-hours endpoint will return `403` if called outside 08:00–17:00 UTC because the injected `TimeProvider` is set to UTC.

## Dynamic Demo Endpoints

| Endpoint | Rule | Extra Requirements |
| --- | --- | --- |
| `GET /api/dyn/orders/view` | `AnyOf(Admin, Sales)` | None |
| `POST /api/dyn/orders/create` | `AllOf(Admin, Sales)` | Header `X-Request-Source: Internal` |
| `DELETE /api/dyn/orders/{id}` | `AnyOf(Admin)` | Business hours (08:00–17:00 UTC) |
| `GET /api/dyn/reports/sensitive?tenantId=...` | `NotAnyOf(Suspended, Blacklisted)` | `tenantId` query must match `tenant_id` claim |

Example request with the custom header:

```powershell
curl https://localhost:7242/api/dyn/orders/create ^
  -H "Authorization: Bearer $token" ^
  -H "X-Request-Source: Internal" ^
  -X POST
```

## Troubleshooting

* **403 Forbidden** — verify the required roles, query parameters, headers, and time window described above.
* **401 Unauthorized** — ensure the `Authorization: Bearer` header is present and the token is not expired.
* **500 JWT configuration missing** — set `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` in `appsettings.Development.json` before running.

For deeper customization or integration instructions, see `docs/DeveloperGuide.md` and `docs/Specification.md`.
