---
name: sdd-next-task
description: >
  Implements a feature's full TDD cycle from Tests.md and Tasks.md: first writes all test
  files (RED), then all production files (GREEN), then runs dotnet test (VERIFY). Tracks
  progress by stage across sessions via checkbox state in Tests.md and Tasks.md. Stops when
  all tasks are complete or a build failure requires manual intervention. Invoke with
  /sdd-next-task <FeatureName> (e.g. /sdd-next-task CreateOrderDomainBusiness).
---

# sdd-next-task — TDD Sequential Implementation Executor

Implements a feature's full TDD cycle in four fixed stages. The stage order is
non-negotiable:

| Stage | Name   | Source     | What happens                                                    |
| ----- | ------ | ---------- | --------------------------------------------------------------- |
| 1     | SETUP  | Tests.md   | Create .csproj test projects and add to solution                |
| 2     | RED    | Tests.md   | Write all test `.cs` files — no build (types don't exist yet)  |
| 3     | GREEN  | Tasks.md   | Write all production files — per-task build check              |
| 4     | VERIFY | Tests.md   | Run `dotnet test` — report pass/fail                           |

Stages are determined automatically from checkbox state across sessions. The skill
resumes exactly where the previous session left off.

## Hardcoded project constants (do not ask the user for these)

- Service root: `C:\Users\joaon\Projetos\IA\Study Projects\demo-skill-mcp-server-net-core\JL.Commerce.Tecnology.Service`
- Spec root: `C:\Users\joaon\Projetos\IA\Study Projects\demo-skill-mcp-server-net-core\specs`
- Build command: `dotnet build --no-restore -v q`
- Test command: `dotnet test --no-build -v q`

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

Read all five files from the resolved spec folder:

| File            | Error if missing                                                                        |
| --------------- | --------------------------------------------------------------------------------------- |
| `Spec.md`       | "Spec.md not found. Write it first or run `/spec-draft`."                               |
| `Plan.md`       | "Plan.md not found. Run `/sdd-spec` first."                                             |
| `Constitution.md` | "Constitution.md not found. Run `/sdd-spec` first."                                  |
| `Tests.md`      | "Tests.md not found. Run `/sdd-spec` first — TDD requires test specifications before implementation." |
| `Tasks.md`      | "Tasks.md not found. Run `/sdd-spec` first."                                            |

Stop immediately with the matching message if any file is missing. All five are required.

Initialize counters: `completedSetup = 0`, `completedTests = 0`, `completedTasks = 0`.

---

## Step 2.5 — Determine current TDD stage

Inspect Tests.md and Tasks.md to determine where to resume:

```
hasUncheckedSetup  = Tests.md has any unchecked "- [ ]" task in "## Stage 1" section
hasUncheckedTests  = Tests.md has any unchecked "- [ ]" task in any "## Stage 2" section
hasUncheckedTasks  = Tasks.md has any unchecked "- [ ]" implementation task
                     (excluding EF migration, dotnet ef, dotnet build, dotnet test lines)
hasUncheckedVerify = Tests.md has unchecked "- [ ]" task in "## Stage 4" section

currentStage =
  if hasUncheckedSetup  → SETUP
  if hasUncheckedTests  → RED
  if hasUncheckedTasks  → GREEN
  if hasUncheckedVerify → VERIFY
  else                  → ALL_DONE
```

Print the current stage at session start:
```
TDD for {FeatureName} — resuming at Stage: {SETUP | RED | GREEN | VERIFY}
```

---

## Step 3 — Find the next task (by stage)

### SETUP stage

Scan `## Stage 1 — Setup` in Tests.md for the first unchecked `- [ ]` task.

Skip any task whose `.csproj` file already exists on disk.

If no unchecked Setup task remains → advance to RED (re-enter Step 3 with `currentStage = RED`).

### RED stage

Scan all `## Stage 2 —` sections in Tests.md top-to-bottom for the first unchecked `- [ ]` task.

If no unchecked Stage 2 task remains → **RED→GREEN transition**:
1. Run one build to document the RED state:
   ```bash
   dotnet build --no-restore -v q
   ```
   Label the output `=== RED STATE (expected — production types not yet implemented) ===`.
   Do **not** abort on failure — these errors are expected.
2. Set `currentStage = GREEN` and re-enter Step 3.

### GREEN stage

Scan `Tasks.md` top-to-bottom for the first unchecked `- [ ]` **implementation task**.

Skip these (operational — not source code):
- Every task under the `## Verification` section heading.
- Any task whose text matches one of these patterns:
  - Starts with `Run EF migration`
  - Contains `dotnet ef migrations`
  - Contains `dotnet build`
  - Contains `dotnet test`

If no unchecked implementation task remains → advance to VERIFY (set `currentStage = VERIFY`, re-enter Step 3).

### VERIFY stage

The single `dotnet test` task in `## Stage 4 — Verification` of Tests.md. Execute it in Step 5 directly.

### ALL_DONE

```
TDD cycle complete for {FeatureName}.
  {completedSetup} test project(s) set up
  {completedTests} test class(es) written
  {completedTasks} production file(s) implemented

All tasks are checked off. Review the final dotnet test results above, then invoke:
  validate-guardrails-implementation
```

Stop here.

### Extract from the selected task line:

- **Full task description** — the complete `- [ ] ...` text including sub-bullets
- **Target file path** — from parentheses `(...)` at end of task line, or from a
  sub-bullet containing a file path; for tasks that modify existing files (AppDbContext,
  Program.cs), derive from the task text
- **CON-* IDs** — all identifiers matching `CON-[A-Z]+-\d+` in the task line or its indented sub-bullets
- **TR-N ID** — the `[TR-N]` identifier in the task line (RED stage only)
- **BR-N IDs** — for traceability comments in test files (RED stage only)

---

## Step 4 — Implement the task

### SETUP stage — create test projects

Write the `.csproj` XML file at the path specified in the task. Use this template for
the unit test project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.*" />
    <PackageReference Include="coverlet.collector" Version="6.*">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Domain\JL.Commerce.Tecnology.Service.Domain.csproj" />
    <ProjectReference Include="..\..\src\Application\JL.Commerce.Tecnology.Service.Application.csproj" />
  </ItemGroup>
</Project>
```

For the integration test project, also add:
```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />
```
And replace the project references with a reference to the Presentation project:
```xml
<ProjectReference Include="..\..\src\Presentation\JL.Commerce.Tecnology.Service.Presentation.csproj" />
```

For the "Add both projects to the solution file" task:
Run from the service root:
```bash
dotnet sln add tests/UnitTests/JL.Commerce.Tecnology.Service.UnitTests.csproj
dotnet sln add tests/IntegrationTests/JL.Commerce.Tecnology.Service.IntegrationTests.csproj
```

### RED stage — write test class files

For each test task in Tests.md:

1. Read the Plan.md section(s) for the production type(s) this test class covers.
2. Read the CON-* and BR-N rules referenced in the task and its sub-bullets.
3. Write the test `.cs` file:
   - Namespace: `JL.Commerce.Tecnology.Service.UnitTests.{layer path}` or `IntegrationTests.{layer path}`
   - Class: `public sealed class {Name}Tests`
   - For Application handler/validator tests: constructor creates `Mock<IPort>()` for each dependency
   - One `[Fact]` method per sub-bullet listed in the Tests.md task
   - Method name directly encodes the scenario (e.g., `Create_WithEmptyItems_Throws_OrderDomainException`)
   - Each method has `// Arrange`, `// Act`, `// Assert` comments
   - Assertion line includes `// [BR-N] [CON-*]` traceability comment
   - Methods reference production types from Plan.md — these types do NOT exist yet; this is intentional (RED state)

**No build is run after writing a test file.** Write the file and go to Step 6.

### GREEN stage — write production files

Existing production-code implementation logic:

Follow all project conventions from CLAUDE.md.

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

**Plan.md is the single source of truth.** If Plan.md and Tasks.md conflict on a detail, follow Plan.md.

**For tasks that modify existing files** (AppDbContext, Program.cs, endpoints files):
1. Read the current file content first.
2. Insert the required lines at the correct location.
3. Never remove or overwrite existing lines.

**Aggregate constraint:** every aggregate must have `private {Name}() {}` parameterless constructor for EF Core.

**CON-* rules:** for each CON-* ID from the task, find its full definition in Constitution.md and enforce it strictly.

### VERIFY stage

Execute `dotnet test` — handled in Step 5.

---

## Step 5 — Build / test verification (by stage)

### SETUP
Run `dotnet build --no-restore -v q` after the solution-add task. Must pass (exit 0).

If a `.csproj` creation task fails the build after 2 fix rounds, stop and report the error.

### RED
No per-test build. Skip directly to Step 6.

When ALL Stage 2 tasks are checked off (transition to GREEN):
```bash
dotnet build --no-restore -v q
```
Print output prefixed with `=== RED STATE (expected — production types not yet implemented) ===`.
Do NOT abort on failure. Immediately proceed to GREEN stage.

### GREEN
Run from the service root:
```bash
dotnet build --no-restore -v q
```

- **Build passes (exit 0):** proceed to Step 6.
- **Build fails:** fix compilation errors in the implemented file(s), re-run. Repeat up to **2 fix rounds**.
- **Still failing after 2 rounds:** stop and report:

> "Build failed after implementing: `{task description}`.
>
> ```
> {last build output — last 20 lines}
> ```
>
> Task NOT marked complete. Fix the build manually, then re-invoke
> `/sdd-next-task {FeatureName}` to resume from this task."

Do not update Tasks.md if the build does not pass.

### VERIFY
Run from the service root:
```bash
dotnet test --no-build -v q
```

Proceed to Step 8 with the test results.

---

## Step 6 — Update checklist

- **SETUP / RED**: mark `- [x]` in **Tests.md** for the completed task.
  Increment `completedSetup` (SETUP) or `completedTests` (RED).
- **GREEN**: mark `- [x]` in **Tasks.md** for the completed task.
  Increment `completedTasks`.
- **VERIFY**: mark `- [x]` in **Tests.md** Stage 4 task only on a full pass (exit 0 from dotnet test).

Update only the single matched `- [ ]` line. Do not modify any other checkboxes, text, or formatting.

---

## Step 7 — Print progress and continue

Print a one-line status, then immediately go back to Step 3 with no user input:

**SETUP:**
```
[SETUP {completedSetup}] Created: {description} — Build: PASS
```

**RED:**
```
[RED {completedTests}] Written: {test class name} ({TR-N}) — queued (build deferred)
```

**GREEN:**
```
[GREEN {completedTasks}] Completed: {task description, first line only} — Build: PASS
```

---

## Step 8 — Final VERIFY report

After running `dotnet test` in VERIFY stage:

**On full pass:**
```
TDD cycle complete for {FeatureName}:
  {completedSetup} test project(s) set up
  {completedTests} test class(es) written (RED → GREEN)
  {completedTasks} production file(s) implemented

dotnet test: PASS — {N} test(s) passed ✓

Next: invoke validate-guardrails-implementation
```
Mark the Stage 4 Verification task `- [x]` in Tests.md.

**On failure:**
```
TDD cycle complete for {FeatureName}:
  {completedSetup} test project(s) set up
  {completedTests} test class(es) written
  {completedTasks} production file(s) implemented

dotnet test: FAIL — {N} test(s) failed

Failed tests:
  {test class}.{method name}: {assertion message}
  …

Fix the listed tests, then re-invoke /sdd-next-task {FeatureName} to re-run VERIFY.
```
Do NOT mark the Stage 4 task complete on failure.
