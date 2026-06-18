# security-reviewer Memory

> Managed by the security-reviewer subagent. Do not edit manually unless correcting an error.
> Last updated: 2026-06-18

---

## Last Scan State

| Field | Value |
|-------|-------|
| Date | 2026-06-18 |
| Git SHA | 93ab2fe128d908147431074c7de83e89b3b39fde |
| Scanner used | Semgrep 1.166.0 (MCP plugin) + Static fallback cross-validation |
| Total findings | 9 |
| Critical | 4 |
| High | 1 |
| Medium | 4 |
| Low | 0 |

---

## Open Findings

| ID | Severity | File | Line | Title | Status | Fix Ref |
|----|----------|------|------|-------|--------|---------|
| S001 | Critical | src/Presentation/appsettings[dot]json | 10 | Hardcoded database password in connection string | OPEN | F001 |
| S002 | Critical | src/Presentation/appsettings[dot]json | 13 | Placeholder JWT Authority value | OPEN | F002 |
| S003 | Critical | src/Presentation/Endpoints/CatalogProductEndpoints.cs | 23-51 | All CatalogProduct mutation endpoints lack RequireAuthorization | OPEN | F003 |
| S004 | Critical | src/Presentation/Endpoints/EntityEndpoints.cs | 19-23 | Entity POST mutation endpoint lacks RequireAuthorization | OPEN | F004 |
| S005 | High | src/Presentation/Endpoints/CatalogProductEndpoints.cs | 101-119 | Exception message leaked in HTTP 404 response body | OPEN | F005 |
| S006 | Medium | src/Presentation/Program.cs | — | Missing rate limiter (AddRateLimiter / UseRateLimiter) | OPEN | F006 |
| S007 | Medium | src/Presentation/appsettings[dot]json | 8 | AllowedHosts wildcard permits any Host header | OPEN | F007 |
| S008 | Medium | src/Presentation/Program.cs | — | Missing UseExceptionHandler middleware | OPEN | F008 |
| S009 | Medium | src/Presentation/Program.cs | 70 | MassTransit using InMemory transport (not production-safe) | OPEN | F009 |

> Status values: `OPEN` | `RESOLVED` | `ACCEPTED_RISK` | `FALSE_POSITIVE`
> Fix Ref: links to the **Cached Fix Suggestions** section below. Security reviewer must use cached text instead of calling context7 for existing findings.

---

## Cached Fix Suggestions

> Written by security-reviewer after Phase 4. Reuse these instead of calling context7 for existing findings.

### F001 — S001: Hardcoded database password

Remove the password from `appsettings.json`. Use `dotnet user-secrets set "ConnectionStrings:Database" "<full-connection-string>"` for local development. In production, override via environment variable `ConnectionStrings__Database=<value>` — environment variables are loaded after appsettings and take precedence per the ASP.NET Core configuration hierarchy.

### F002 — S002: Placeholder JWT Authority

Replace `https://your-identity-provider` with the real authority URL. Set explicit validation parameters in `AddAuthentication().AddJwtBearer(...)`:
```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateIssuerSigningKey = true,
    ValidIssuers = ["https://real-authority.example.com"]
};
```

### F003 — S003: CatalogProduct endpoints lack RequireAuthorization

Chain `.RequireAuthorization()` on the `MapGroup` call so all endpoints in the group inherit the policy. GET endpoints that must stay public can opt out individually with `.AllowAnonymous()`:
```csharp
var group = app.MapGroup("/api/v1/catalog-products").RequireAuthorization();
group.MapGet("/{id:guid}", GetByIdAsync).AllowAnonymous();
```

### F004 — S004: Entity POST endpoint lacks RequireAuthorization

Apply group-level `.RequireAuthorization()` to the Entity route group. The GET endpoint can opt out with `.AllowAnonymous()` if public reads are required.

### F005 — S005: Exception message leaked in HTTP 404 response

Remove `ex.Message` from the response body. Return either a bare `TypedResults.NotFound()` or a static RFC 7807 problem-details object:
```csharp
// before: TypedResults.NotFound(new { ex.Message })
// after:
return TypedResults.Problem(statusCode: 404, title: "Product not found.");
```

### F006 — S006: Missing rate limiter

Register a fixed-window rate limiter and apply it:
```csharp
builder.Services.AddRateLimiter(options =>
    options.AddFixedWindowLimiter("api", o => { o.PermitLimit = 100; o.Window = TimeSpan.FromMinutes(1); }));
// ...
app.UseRateLimiter();
// on the route group:
group.RequireRateLimiting("api");
```

### F007 — S007: AllowedHosts wildcard

Replace `"AllowedHosts": "*"` in `appsettings.json` with an explicit allow-list:
```json
"AllowedHosts": "api.example.com;localhost"
```

### F008 — S008: Missing UseExceptionHandler middleware

Add problem-details error handling to the pipeline:
```csharp
builder.Services.AddProblemDetails();
// ...
app.UseExceptionHandler();
app.UseStatusCodePages();
```

### F009 — S009: MassTransit InMemory transport

Replace `x.UsingInMemory(...)` with `x.UsingRabbitMq(...)` for production deployments. Store RabbitMQ credentials in environment variables or a secrets manager, not in `appsettings.json`.

---

## Resolved / Accepted Findings

| ID | Severity | File | Title | Resolution | Date |
|----|----------|------|-------|------------|------|

---

## Project-Specific Notes

- 2026-06-14: Initial scan completed at commit a54d1ca. MassTransit is pinned to 8.5.5 (9+ requires paid license). InMemory transport is intentional for local dev but must be replaced with RabbitMQ (UsingRabbitMq) for any production deployment.
- 2026-06-18: Re-scan at commit 93ab2fe (Entity.cs added UpdatedAt field). No new security findings introduced by the change. All 9 prior findings remain OPEN.
