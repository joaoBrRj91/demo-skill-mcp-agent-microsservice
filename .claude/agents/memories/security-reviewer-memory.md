# security-reviewer Memory

> Managed by the security-reviewer subagent. Do not edit manually unless correcting an error.
> Last updated: 2026-06-14

---

## Last Scan State

| Field | Value |
|-------|-------|
| Date | 2026-06-14 |
| Git SHA | fcea111e11dd748dcb705aa9b67cb9ec314a212c |
| Scanner used | Static fallback (Semgrep 1.166.0 installed; cloud rules returned 0 results without login) |
| Total findings | 9 |
| Critical | 3 |
| High | 1 |
| Medium | 5 |
| Low | 0 |

---

## Open Findings

| ID | Severity | File | Line | Title | Status |
|----|----------|------|------|-------|--------|
| S001 | Critical | src/Presentation/appsettings[dot]json | 10 | Hardcoded database password in connection string | OPEN |
| S002 | Critical | src/Presentation/appsettings[dot]json | 13 | Placeholder JWT Authority value | OPEN |
| S003 | Critical | src/Presentation/Endpoints/CatalogProductEndpoints.cs | 23-51 | All CatalogProduct mutation endpoints lack RequireAuthorization | OPEN |
| S004 | Critical | src/Presentation/Endpoints/EntityEndpoints.cs | 19-23 | Entity POST mutation endpoint lacks RequireAuthorization | OPEN |
| S005 | High | src/Presentation/Endpoints/CatalogProductEndpoints.cs | 101-119 | Exception message leaked in HTTP 404 response body | OPEN |
| S006 | Medium | src/Presentation/Program.cs | — | Missing rate limiter (AddRateLimiter / UseRateLimiter) | OPEN |
| S007 | Medium | src/Presentation/appsettings[dot]json | 8 | AllowedHosts wildcard permits any Host header | OPEN |
| S008 | Medium | src/Presentation/Program.cs | — | Missing UseExceptionHandler middleware | OPEN |
| S009 | Medium | src/Presentation/Program.cs | 70 | MassTransit using InMemory transport (not production-safe) | OPEN |

> Status values: `OPEN` | `RESOLVED` | `ACCEPTED_RISK` | `FALSE_POSITIVE`

---

## Resolved / Accepted Findings

| ID | Severity | File | Title | Resolution | Date |
|----|----------|------|-------|------------|------|

---

## Project-Specific Notes

- 2026-06-14: Initial scan completed at commit a54d1ca. MassTransit is pinned to 8.5.5 (9+ requires paid license). InMemory transport is intentional for local dev but must be replaced with RabbitMQ (UsingRabbitMq) for any production deployment.
