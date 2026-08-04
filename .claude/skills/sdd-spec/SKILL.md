---
name: sdd-spec
description: >
  Runs the spec-driven development (SDD) process to generate Plan.md, Constitution.md,
  Tests.md, and Tasks.md from an existing Spec.md. Use this skill whenever the user mentions
  "SDD", "spec documents", "generate the plan", "create the constitution", "run SDD for
  this feature", "spec-driven", or wants to document a feature before implementing it.
  Also trigger when the user says "I have a spec" or "I wrote the spec, now what?".
  Requires an existing, non-empty Spec.md in the target folder.
  Accepts an optional `use_context7=true` argument (set programmatically by the
  sdd-spec-create agent) to enrich Tasks.md task descriptions with current .NET library
  API signatures retrieved from context7 MCP. Do not set this argument manually.
---

# sdd-spec — Spec-Driven Development Document Generator

Generates four documents in mandatory sequence from an existing `Spec.md`. Each document
uses all preceding ones as context, so the order is non-negotiable:

| Order | File              | Context used                                             |
| ----- | ----------------- | -------------------------------------------------------- |
| 1     | `Plan.md`         | `Spec.md`                                                |
| 2     | `Constitution.md` | `Spec.md` + `Plan.md`                                    |
| 3     | `Tests.md`        | `Spec.md` + `Plan.md` + `Constitution.md`                |
| 4     | `Tasks.md`        | `Spec.md` + `Plan.md` + `Constitution.md` + `Tests.md`   |

Tests.md comes before Tasks.md so that test specifications exist before implementation
tasks are written. This enforces the TDD contract: test code is always written before
production code.

Skip any file that already exists and is non-empty — warn the user and continue to the next.

---

## Step 0 — Parse arguments

Before doing anything else, parse the raw argument string passed to this skill:

- **`SPEC_PATH`** — everything in the arg string that precedes the token `use_context7`, trimmed of whitespace. If no `use_context7` token is present, the entire arg string is `SPEC_PATH`.
- **`USE_CONTEXT7`** — `true` if and only if the arg string contains the exact token `use_context7=true`; otherwise `false`.

Carry `SPEC_PATH` forward as the effective path argument for Step 1, and `USE_CONTEXT7` as a boolean flag for Step 6.

---

## Step 1 — Locate the spec folder

**If a path was passed** as `SPEC_PATH` (e.g. `/sdd-spec specs/JL.Commerce.Tecnology.Service/Features/ProcessRefund`):

- Resolve it to an absolute path and use it as the spec folder.

**If `SPEC_PATH` is empty**, ask:

> "Which feature are you speccing? Tell me:
>
> 1. The service name (e.g. `JL.Commerce.Tecnology.Service`)
> 2. The feature folder name (e.g. `ProcessRefund`)
>
> Or paste the full path to the spec folder."

The canonical folder pattern is:

```
<repo-root>/specs/{ServiceName}/Features/{FeatureName}/
```

---

## Step 2 — Verify prerequisite

Read `Spec.md` from the resolved folder.

- **Does not exist** → stop. Tell the user: "Spec.md not found at `{path}`. Create it first — it must describe the feature in business terms before SDD can proceed."
- **Exists but empty** → stop with the same message.
- **Exists with content** → extract the feature name from the `# Spec: {FeatureName}` heading (or derive it from the folder name) and proceed.

---

## Step 3 — Generate Plan.md

Read `Spec.md` and produce a concrete implementation plan following this project's
Hexagonal Architecture + DDD + CQRS conventions (see `CLAUDE.md` for the full rule set).
Plan.md is the place where all implementation detail lives — class names, file paths,
namespaces, method signatures, code snippets. Nothing in Spec.md should be repeated
verbatim; Plan.md translates business intent into engineering decisions.

**Structure to follow:**

```markdown
# Implementation Plan: {FeatureName}

## Architecture Overview

<bullet list of affected layers and the key types in each>

---

## Domain Layer

### Aggregate: {Name}

`Domain/Aggregates/{Name}/{Name}.cs`
<properties, factory method signature, mutation methods, domain events raised, EF parameterless ctor>

### Strongly-Typed ID

`Domain/Aggregates/{Name}/{Name}Id.cs` — sealed record(Guid Value) with static New() and ToString()

### Value Objects (if any)

<file path, fields — use classes not records when EF owns them>

### Enumerations (if any)

<file path, values>

### Domain Events

`Domain/Events/{Name}{Verb}Event.cs` — sealed record : IDomainEvent

### Domain Exceptions

`Domain/Exceptions/{Name}NotFoundException.cs` etc.

---

## Application Layer

### Ports

`Application/Ports/I{Name}Repository.cs` — method signatures

### Command: {Verb}{Name}Command

`Application/Commands/{Verb}{Name}/`

- **Command** — sealed record : IRequest<Guid> (or IRequest for void)
- **Handler** — numbered steps showing the handler algorithm
- **Validator** — FluentValidation rules, one per property

### Query: Get{Name}...Query (repeat per query)

`Application/Queries/Get{Name}.../`

- **Query** — sealed record : IRequest<{Dto}?>
- **Handler** — numbered steps

### DTOs

`Application/DTOs/{Name}Dto.cs` — fields

### AutoMapper Profile

`Application/Mappings/{Name}MappingProfile.cs` — mapping pairs

---

## Infrastructure.Data

### EF Configuration

`Infrastructure.Data/Configurations/{Name}Configuration.cs`
<Ignore DomainEvents, HasConversion for enums, OwnsOne/OwnsMany for value objects>

### Repository

`Infrastructure.Data/Repositories/{Name}Repository.cs`

### AppDbContext

Add `DbSet<{Name}> {PluralName} { get; set; }` to AppDbContext.cs

### EF Migration
```

dotnet ef migrations add Add{Name} --project src/Infrastructure.Data --startup-project src/Presentation

```

---

## Infrastructure.Integration (if applicable)

### External Adapters
<file path, interface implemented, algorithm>

### MassTransit Consumers
<file path, event consumed, MediatR command dispatched>

---

## Presentation Layer

### Endpoints
`Presentation/Endpoints/{Name}Endpoints.cs` — route table with HTTP method, path, command/query, return type

### Program.cs Additions
<numbered list: DI registrations, consumer registrations, endpoint map call>

---

## File Checklist

### Domain ({N} files)
- `Domain/Aggregates/{Name}/{Name}Id.cs`
- `Domain/Aggregates/{Name}/{Name}.cs`
- ...

### Application ({N} files)
- ...

### Infrastructure.Data ({N} files + {N} edits)
- ...

### Infrastructure.Integration ({N} files)
- ...

### Presentation ({N} files + {N} edits)
- ...
```

**Rules to follow when writing Plan.md:**

- Naming conventions from `CLAUDE.md` are mandatory — aggregates, commands, queries, repos, endpoints must follow the exact suffix and casing rules.
- Root namespace is always `JL.Commerce.Tecnology.Service`.
- Never include `Id`, `CreatedAt`, or `UpdatedAt` in property lists — auto-generated.
- MassTransit is pinned to **8.5.5** — do not reference 9+ APIs.
- POST endpoints that trigger async processing return **HTTP 202**, not 201.
- Value objects that EF Core owns must be classes, not records.

---

## Step 4 — Generate Constitution.md

Read `Spec.md` + `Plan.md`. Elevate the feature's business rules into immutable constraints.
The Constitution is stricter than the Spec — it encodes what the system must never do,
not just what it should do. Use RFC 2119 conformance language throughout: **MUST**,
**MUST NOT**, **SHALL**, **SHOULD**, **MAY**.

**Structure to follow:**

```markdown
# Constitution — {FeatureName}

> **Status:** Ratified  
> **Scope:** `{ServiceName}` — {one-line scope description}

---

## Preamble

<explains the role of this document and how it relates to Spec.md and Plan.md>

---

## Article I — {Primary Domain Concern}

(e.g., "Order State Machine", "Payment Lifecycle", "Refund Eligibility Rules")

### § 1.1 Valid States (if stateful)

<state table with terminal flag>

### § 1.2 Valid Transitions (if stateful)

<ASCII state diagram>

### § 1.3 Workflow Laws

| ID       | Rule |
| -------- | ---- |
| CON-WF-1 | ...  |

---

## Article II — Domain Invariants

Trace each rule to its Spec.md business rule (e.g., "Source: BR-1").

| ID       | Source | Rule |
| -------- | ------ | ---- |
| CON-DI-1 | BR-1   | ...  |

---

## Article III — Idempotency & Concurrency Laws (if applicable)

| ID       | Rule |
| -------- | ---- |
| CON-IC-1 | ...  |

---

## Article IV — Data Security Mandates

### § 4.1 Request and Response Sanitization

| ID        | Rule                                            |
| --------- | ----------------------------------------------- |
| CON-SEC-1 | All incoming string fields MUST be sanitized... |

### § 4.2 Sensitive Data at Rest (include only if feature handles PII/PCI)

<classification table: field, classification, storage rule, API display rule>
| ID | Rule |
|----|------|
| CON-SEC-5 | ... |

---

## Article V — Governance and Compliance

| ID        | Rule |
| --------- | ---- |
| CON-GOV-1 | ...  |

---

## Article VI — API Contract Invariants

| ID        | Rule |
| --------- | ---- |
| CON-API-1 | ...  |

---

## Appendix — Constitution Rule Index

| Article | ID Range            | Domain |
| ------- | ------------------- | ------ |
| I       | CON-WF-1 – CON-WF-N | ...    |
```

**Rules to follow when writing Constitution.md:**

- Every constraint gets a unique `CON-{DOMAIN}-{N}` ID. Domains: `WF` (workflow), `DI` (domain invariants), `IC` (idempotency/concurrency), `SEC` (security), `GOV` (governance), `API` (API contract).
- Omit articles that are not applicable (e.g., skip § 4.2 if no PII/PCI is involved), but always include Articles II, IV § 4.1, and VI.
- Security article IV § 4.1 is mandatory for every feature — all string input must be sanitized and error responses must never expose stack traces.
- Constitution rules must go further than the Spec. "Items must not be empty" (Spec) becomes "An order with zero items MUST be rejected before persistence" (Constitution).

---

## Step 5 — Generate Tests.md

Read `Spec.md` + `Plan.md` + `Constitution.md`. Produce a TDD test specification
organized as a checkbox task list. Each item maps to one test class file. Each item
specifies the individual `[Fact]` method names, what they assert, and which BR-N /
CON-* IDs they trace back to.

Skip if Tests.md already exists and is non-empty.

Before writing, check whether the test project `.csproj` files already exist:
- `tests/UnitTests/JL.Commerce.Tecnology.Service.UnitTests.csproj`
- `tests/IntegrationTests/JL.Commerce.Tecnology.Service.IntegrationTests.csproj`

Include the `## Stage 1 — Setup` section only for projects that do **not** yet exist.

**Structure to follow:**

```markdown
# Tests — {FeatureName}

> TDD: implement ALL items in this file before opening Tasks.md.
> Stage 1 → Setup | Stage 2 → Write test files (RED) | Stage 3 → implement Tasks.md (GREEN) | Stage 4 → dotnet test

## Stage 1 — Setup (skip tasks whose .csproj already exists on disk)

- [ ] Create unit test project (`tests/UnitTests/JL.Commerce.Tecnology.Service.UnitTests.csproj`)
  - Target: net10.0; packages: xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, Moq, coverlet.collector
  - Project references: Domain, Application
- [ ] Create integration test project (`tests/IntegrationTests/JL.Commerce.Tecnology.Service.IntegrationTests.csproj`)
  - Target: net10.0; same packages + Microsoft.AspNetCore.Mvc.Testing
  - Project reference: Presentation
- [ ] Add both projects to the solution file

## Stage 2 — Unit Tests: Domain Layer

- [ ] [TR-1] `{Name}Id` value object (`tests/UnitTests/Domain/Aggregates/{Name}/{Name}IdTests.cs`)
  - `New_Returns_Valid_NonEmpty_Guid` — Value is not Guid.Empty
  - `Two_New_Calls_Produce_Unique_Ids` — consecutive calls differ
  - `ToString_Returns_Guid_String` — matches Value.ToString()

- [ ] [TR-2] `{Name}` aggregate (`tests/UnitTests/Domain/Aggregates/{Name}/{Name}Tests.cs`)
  - One `[Fact]` per test scenario from Spec.md — method name encodes the scenario
  - Each method includes `// [BR-N] [CON-*]` traceability on the assertion line
  - …

## Stage 2 — Unit Tests: Application Layer

- [ ] [TR-N] `{Verb}{Name}CommandHandler` (`tests/UnitTests/Application/Commands/{Verb}{Name}CommandHandlerTests.cs`)
  - Constructor sets up Moq mocks for all port interfaces (IRepository, IEventBus, etc.)
  - One `[Fact]` per handler behaviour described in Plan.md
  - …

- [ ] [TR-N] `{Verb}{Name}CommandValidator` (`tests/UnitTests/Application/Commands/{Verb}{Name}CommandValidatorTests.cs`)
  - One `[Fact]` per FluentValidation rule from Constitution.md
  - …

- [ ] [TR-N] `Get{Name}...QueryHandler` (`tests/UnitTests/Application/Queries/…Tests.cs`)
  - …

## Stage 2 — Integration Tests

- [ ] [TR-N] `{Name}Endpoints` integration tests (`tests/IntegrationTests/Endpoints/{Name}EndpointsTests.cs`)
  - Uses `WebApplicationFactory<Program>`
  - One `[Fact]` per API contract invariant from Constitution.md Article VI
  - …

## Stage 4 — Verification

- [ ] `dotnet test` — zero failures
```

**Rules when writing Tests.md:**
- Every test scenario from Spec.md must map to at least one `[Fact]`.
- Every CON-* constraint must be traceable to at least one test.
- Omit Integration Tests section if the feature adds no API-facing endpoints.
- TR-N IDs are sequential across the whole document.

---

## Step 6 — Generate Tasks.md

Read `Spec.md` + `Plan.md` + `Constitution.md` + `Tests.md`. Produce a checkbox list of every
concrete implementation task, ordered by dependency (Domain first, Presentation last).
Each task maps to one file or one edit from Plan.md's file checklist, plus any
Constitution-mandated additions not in the plan.

Add the following reminder at the top of the generated Tasks.md, directly after the `# Tasks` heading:

```markdown
> TDD: all Tests.md tasks (Stages 1–2) MUST be complete before implementing any task below.
```

**If `USE_CONTEXT7=true` — enrich task descriptions before writing:**

After reading all four source documents but before writing Tasks.md, identify the .NET
libraries this feature uses from Plan.md (look for MediatR handlers, FluentValidation
validators, AutoMapper profiles, MassTransit consumers). For each identified library
(up to 4):

1. Call `mcp__context7__resolve-library-id` with the library display name
   (e.g., `"MediatR"`, `"FluentValidation"`, `"AutoMapper"`, `"MassTransit"`).
2. Call `mcp__context7__query-docs` with the resolved library ID and a targeted query
   (e.g., `"IRequestHandler implementation"`, `"AbstractValidator RuleFor"`).
3. Use the retrieved current API signatures in task text where they are referenced —
   e.g., a handler task cites the exact `IRequestHandler<TRequest, TResponse>` signature
   from docs rather than from training memory.

This enrichment does not change which tasks are generated, their order, or their
CON-* references — it only improves the accuracy of task descriptions.

**Structure to follow:**

```markdown
# Tasks — {FeatureName}

## Domain

- [ ] Create `{Name}Id` strongly-typed ID (`Domain/Aggregates/{Name}/{Name}Id.cs`)
- [ ] Create `{Name}` aggregate with factory method and mutation methods (`Domain/Aggregates/{Name}/{Name}.cs`)
- [ ] Create value objects: {list} (`Domain/Aggregates/{Name}/...`)
- [ ] Create enumerations: {list} (`Domain/Aggregates/{Name}/...`)
- [ ] Create domain events: {list} (`Domain/Events/...`)
- [ ] Create domain exceptions: {list} (`Domain/Exceptions/...`)

## Application

- [ ] Define `I{Name}Repository` port (`Application/Ports/I{Name}Repository.cs`)
- [ ] Define `I{ExternalAdapter}` port if applicable (`Application/Ports/...`)
- [ ] Create `{Verb}{Name}Command` + handler + validator (`Application/Commands/{Verb}{Name}/`)
- [ ] (repeat per command)
- [ ] Create `Get{Name}...Query` + handler (`Application/Queries/...`)
- [ ] (repeat per query)
- [ ] Create DTOs: {list} (`Application/DTOs/...`)
- [ ] Create `{Name}MappingProfile` (`Application/Mappings/...`)

## Infrastructure.Data

- [ ] Create `{Name}Configuration` EF config — ignore DomainEvents, configure owned types (`Infrastructure.Data/Configurations/...`)
- [ ] Create `{Name}Repository` (`Infrastructure.Data/Repositories/...`)
- [ ] Add `DbSet<{Name}> {PluralName}` to `AppDbContext`
- [ ] Run EF migration: `dotnet ef migrations add Add{Name} --project src/Infrastructure.Data --startup-project src/Presentation`

## Infrastructure.Integration

- [ ] Create `{Adapter}` implementing `I{ExternalAdapter}` (`Infrastructure.Integration/...`)
- [ ] Create `{Name}CreatedConsumer` MassTransit consumer (`Infrastructure.Integration/Messaging/Consumers/...`)

## Presentation

- [ ] Create `{Name}Endpoints` with route registrations (`Presentation/Endpoints/{Name}Endpoints.cs`)
- [ ] Register DI in `Program.cs`: `AddScoped<I{Name}Repository, {Name}Repository>()`
- [ ] Register external adapter in `Program.cs` if applicable
- [ ] Register MassTransit consumer in `Program.cs`
- [ ] Call `app.Map{Name}Endpoints()` in `Program.cs`

## Verification

- [ ] `dotnet build` — zero errors
- [ ] `dotnet test` — no regressions
```

Omit sections with no tasks (e.g., skip Infrastructure.Integration if the feature has no consumers or adapters). Add Constitution-mandated tasks that are not already covered (e.g., audit log table, encryption setup, idempotency unique constraint in the EF config).

---

## Completion

After all files are written (or skipped), report:

```
SDD complete for {FeatureName}:

  Plan.md         — ✓ created  (or ⚠ skipped, already exists and not empty)
  Constitution.md — ✓ created  (or ⚠ skipped, already exists and not empty)
  Tests.md        — ✓ created  ({N} test cases across {M} test classes)  (or ⚠ skipped)
  Tasks.md        — ✓ created  (or ⚠ skipped, already exists and not empty)

TDD order enforced by /sdd-next-task:
  Stage 1: Setup → Stage 2: Write tests (RED) → Stage 3: Implement Tasks.md (GREEN) → Stage 4: dotnet test
```
