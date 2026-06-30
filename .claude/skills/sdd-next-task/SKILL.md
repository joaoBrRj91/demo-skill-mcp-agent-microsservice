---
name: sdd-next-task
description: >
  Implements all unchecked implementation tasks from a feature's Tasks.md sequentially,
  verifying the build after each task before continuing. Stops when all tasks are complete
  or when a build failure requires manual intervention. Invoke with
  /sdd-next-task <FeatureName> (e.g. /sdd-next-task CreateOrderDomainBusiness).
---

# sdd-next-task — Sequential Task Implementation Executor

Implements **all** unchecked implementation tasks from `Tasks.md` in order, driven by the
`Plan.md` spec for each file. Enforces all `CON-*` rules referenced in each task. Verifies
the build after every task before continuing to the next.

Stops only when: all implementation tasks are complete, or a build failure requires manual
intervention.

## Hardcoded project constants (do not ask the user for these)

- Service root: `C:\Users\joaon\Projetos\IA\Study Projects\demo-skill-mcp-server-net-core\JL.Commerce.Tecnology.Service`
- Spec root: `C:\Users\joaon\Projetos\IA\Study Projects\demo-skill-mcp-server-net-core\specs`
- Build command: `dotnet build --no-restore -v q`

---

## Step 1 — Resolve feature folder

Use the argument passed to the skill (e.g. `/sdd-next-task CreateOrderDomainBusiness`
→ `CreateOrderDomainBusiness`).

If no argument was given, ask:

> "Which feature should I work on? Provide the feature folder name
> (e.g. `CreateOrderDomainBusiness`)."

Construct the spec folder path:

```
<repo-root>/specs/JL.Commerce.Tecnology.Service/Features/{FeatureName}/
```

If the folder does not exist, stop:

> "Spec folder not found at `{path}`. Check the feature name or run `/spec-draft`
> to create it."

---

## Step 2 — Load SDD documents

Read all four files from the resolved spec folder. All are required:

| File | Error if missing |
|------|-----------------|
| `Spec.md` | "Spec.md not found. Write it first or run `/spec-draft`." |
| `Plan.md` | "Plan.md not found. Run `/sdd-spec` first." |
| `Constitution.md` | "Constitution.md not found. Run `/sdd-spec` first." |
| `Tasks.md` | "Tasks.md not found. Run `/sdd-spec` first." |

Stop immediately with the matching message if any file is missing.

Initialize a session counter: `completedThisSession = 0`.

---

## Step 3 — Find the next implementation task

Scan `Tasks.md` top-to-bottom for the **first** `- [ ]` entry that is an
**implementation task**.

### Skip these (operational / verification — not source code):

- Every task under the `## Verification` section heading (behavioral tests, require a
  running app).
- Any task whose text matches one of these patterns:
  - Starts with `Run EF migration`
  - Contains `dotnet ef migrations`
  - Contains `dotnet build`
  - Contains `dotnet test`

### Implement everything else, including tasks that modify existing files:

- `Create ...` — new source file
- `Add DbSet<...> to AppDbContext` — edits `Infrastructure.Data/Context/AppDbContext.cs`
- `Register DI in Program.cs` — edits `Presentation/Program.cs`
- `Register ... in MassTransit configuration in Program.cs` — edits `Program.cs`
- `Add global exception handler middleware in Program.cs` — edits `Program.cs`
- `Add security headers to ... endpoints` — edits an existing endpoints file
- `Call app.Map...Endpoints() in Program.cs` — edits `Program.cs`

### If no unchecked implementation task is found — Final Report:

```
All implementation tasks complete. {completedThisSession} task(s) implemented this session.

Verification tasks remain — run them manually, then invoke:
  validate-guardrails-implementation
```

Stop here.

### Extract from the selected task line:

- **Full task description** — the complete `- [ ] ...` text including sub-bullets
- **Target file path** — from parentheses `(...)` at end of task line, or from a
  sub-bullet containing a file path like `Domain/Aggregates/Order/Order.cs`; for
  tasks that modify existing files (AppDbContext, Program.cs), derive from the task text
- **CON-* IDs** — all identifiers matching `CON-[A-Z]+-\d+` in the task line or its
  indented sub-bullets

---

## Step 4 — Implement the task

### 4a — Load the Plan.md section for this file

Find the section in `Plan.md` that corresponds to the target file path identified in
Step 3.

- Match by relative file path (e.g. `Domain/Aggregates/Order/Order.cs`)
- The Plan.md section contains the authoritative spec: class/record name, namespace,
  properties, method signatures, interfaces to implement, constructor parameters

**Plan.md is the single source of truth.** If Plan.md and Tasks.md conflict on a
detail, follow Plan.md — Tasks.md is a checklist, not a spec.

### 4b — Load CON-* rules

For each CON-* ID from Step 3, find its full rule definition in `Constitution.md`.
Extract the rule text and apply it strictly during implementation.

### 4c — Write or modify the target file

Follow all project conventions from CLAUDE.md:

**Type rules:**

| Artifact | C# type |
|----------|---------|
| Aggregate | `sealed class : AggregateRoot<TId>` |
| Strongly-typed ID | `sealed record {Name}Id(Guid Value)` |
| Domain event | `sealed record : IDomainEvent` |
| Domain exception | `sealed class : Exception` |
| Command / Query | `sealed record : IRequest<T>` |
| DTO | `record` |
| Handler | `sealed class` |
| Validator | `sealed class : AbstractValidator<T>` |
| Repository impl | `sealed class` |
| EF configuration | `sealed class : IEntityTypeConfiguration<T>` |
| AutoMapper profile | `sealed class : Profile` |
| Value object (EF owned) | **`class`** — never `record` (EF Core constraint) |

**Aggregate constraint:** every aggregate must have `private {Name}() {}` parameterless
constructor for EF Core.

**For tasks that modify existing files** (AppDbContext, Program.cs, endpoints files):

1. Read the current file content first.
2. Insert the required lines at the correct location (e.g. `DbSet` after existing
   `DbSet` properties; DI registrations with related registrations in `Program.cs`).
3. Never remove or overwrite existing lines.

---

## Step 5 — Build verification

Run from the service root:

```bash
dotnet build --no-restore -v q
```

(Full path: `C:\Users\joaon\Projetos\IA\Study Projects\demo-skill-mcp-server-net-core\JL.Commerce.Tecnology.Service`)

- **Build passes (exit code 0):** proceed to Step 6.
- **Build fails:** read the error output, fix the compilation errors in the
  implemented file(s), re-run the build. Repeat up to **2 fix rounds**.
- **Still failing after 2 rounds:** stop and report:

> "Build failed after implementing task {completedThisSession + 1}: `{task description}`.
> Errors:
>
> ```
> {last build output — last 20 lines}
> ```
>
> Task NOT marked complete. Fix the build manually, then re-invoke
> `/sdd-next-task {FeatureName}` to resume from this task."

Do **not** update `Tasks.md` if the build does not pass.

---

## Step 6 — Update Tasks.md

In `Tasks.md`, replace the `- [ ]` of the completed task with `- [x]`.

```
- [ ] Create `OrderId` strongly-typed ID …
```
→
```
- [x] Create `OrderId` strongly-typed ID …
```

Update only that single line. Do not modify any other checkboxes, text, or formatting.

Increment `completedThisSession` by 1.

---

## Step 7 — Print progress and continue

Print a one-line status for the completed task:

```
[{completedThisSession}] Completed: {task description, first line only} — Build: PASS
```

Then **immediately go back to Step 3** to find and implement the next task. Do not pause
or wait for user input.
