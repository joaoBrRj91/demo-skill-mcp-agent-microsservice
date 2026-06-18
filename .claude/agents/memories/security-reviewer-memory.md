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
- 2026-06-18: Re-scan at commit 93ab2fe (Entity.cs added UpdatedAt field). No new security findings introduced by the change. All 9 prior findings remain OPEN.
