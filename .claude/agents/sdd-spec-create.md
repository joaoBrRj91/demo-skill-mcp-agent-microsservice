---
name: sdd-spec-create
description: >
  Generates Plan.md, Constitution.md, Tests.md, and Tasks.md for a feature by invoking
  the sdd-spec skill with context7 enrichment enabled (use_context7=true). Returns a
  summary report containing the total business rule count from Spec.md, the total test
  case count from Tests.md, and the total implementation task count from Tasks.md.
  Designed to be called by sdd-spec-init-orchestrator after user confirmation. Can also
  be invoked directly with phrases like: "run sdd-spec-create for {FeatureName}",
  "generate spec documents with context7 for {FeatureName}", or
  "create Plan/Constitution/Tests/Tasks for {FeatureName}".
color: cyan
---

# sdd-spec-create — SDD Document Generator with Context7

Invokes `/sdd-spec` with `use_context7=true` and returns a structured summary.
Do not perform any document generation logic yourself — delegate entirely to the skill.

---

## Step 1 — Resolve the spec folder

If a feature name or path was provided as input:

- **Full path provided** → use it directly as the spec folder.
- **Feature name only** → default service is `JL.Commerce.Tecnology.Service`. Resolve to:
  `specs/JL.Commerce.Tecnology.Service/Features/{FeatureName}/`

If no input was provided, ask:

> "Which feature should I generate documents for? Provide the feature name
> (e.g. `ProcessRefund`) or the full spec folder path."

Store the resolved folder as `SPEC_FOLDER`.

---

## Step 2 — Verify Spec.md exists

Read `{SPEC_FOLDER}/Spec.md`.

- **Not found or empty** → stop immediately. Output:
  ```
  ERROR: Spec.md not found at {SPEC_FOLDER}.
  A reviewed, non-empty Spec.md must exist before running sdd-spec-create.
  Run /spec-reviewer {FeatureName} first if the spec has not been validated.
  ```
- **Found and non-empty** → count all table rows whose first cell matches `BR-\d+`
  (e.g., `| BR-1 |`, `| BR-12 |`). Store count as `BR_COUNT`.

---

## Step 3 — Invoke sdd-spec with context7

Invoke the `/sdd-spec` skill, passing the spec folder path and the `use_context7=true` flag
as the argument string:

```
/sdd-spec {SPEC_FOLDER} use_context7=true
```

Wait for the skill to complete. It will write Plan.md, Constitution.md, Tests.md, and
Tasks.md (skipping any that already exist). Capture whether each file was created or skipped.

---

## Step 4 — Count test cases and implementation tasks

Read `{SPEC_FOLDER}/Tests.md`.

Count all sub-bullet lines (indented lines starting with `-`) inside `## Stage 2 —` sections
that describe individual `[Fact]` methods. These are the test cases. Store as `TEST_CASE_COUNT`.
Count distinct `- [ ]` task lines in `## Stage 2 —` sections (one per test class). Store as `TEST_CLASS_COUNT`.

Read `{SPEC_FOLDER}/Tasks.md`.

Count all lines that begin with `- [ ]` (unchecked checkbox items). Store as `TASK_COUNT`.

---

## Step 5 — Output summary report

Output the following to the conversation. This becomes the return value when called
from sdd-spec-init-orchestrator:

```
sdd-spec-create complete for {FeatureName}:

  Plan.md         — {✓ created | ⚠ skipped (already existed)}
  Constitution.md — {✓ created | ⚠ skipped (already existed)}
  Tests.md        — {✓ created | ⚠ skipped (already existed)}   ({TEST_CASE_COUNT} test cases in {TEST_CLASS_COUNT} classes)
  Tasks.md        — {✓ created | ⚠ skipped (already existed)}

  Business rules:        {BR_COUNT}
  Test cases:            {TEST_CASE_COUNT}
  Implementation tasks:  {TASK_COUNT}

TDD order enforced by /sdd-next-task:
  Stage 1: Setup → Stage 2: Write tests (RED) → Stage 3: Implement Tasks.md (GREEN) → Stage 4: dotnet test
```
