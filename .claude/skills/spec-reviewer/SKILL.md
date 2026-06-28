---
name: spec-reviewer
description: >
  Validates a Spec.md for completeness before /sdd-spec runs. Checks required
  sections are present and non-empty, business rules are numbered (BR-N), the
  API contract covers request/response tables per endpoint, and test scenarios
  cover every BR-N rule. Auto-corrects structural gaps (missing or empty
  sections) directly in Spec.md while displaying a correction log to the user,
  then re-validates and outputs READY or INCOMPLETE. Invoke as
  /spec-reviewer <FeatureName> or
  /spec-reviewer specs/JL.Commerce.Tecnology.Service/Features/<FeatureName>.
  Also triggered by phrases like "review the spec", "is the spec ready",
  "validate spec before SDD".
---

# spec-reviewer — Spec.md Quality Gate

Validates a `Spec.md`, **auto-corrects any structural gaps it finds**, then
outputs **READY** or **INCOMPLETE** before any SDD documents are generated.

---

## Step 1 — Locate the spec folder

**If a path argument was given** (e.g., `/spec-reviewer specs/JL.Commerce.Tecnology.Service/Features/CreateOrderDomainBusiness`):
- Resolve to an absolute path and use it as the spec folder.

**If only a feature name was given** (e.g., `/spec-reviewer CreateOrderDomainBusiness`):
- Resolve to: `<repo-root>/specs/JL.Commerce.Tecnology.Service/Features/{FeatureName}/`

**If nothing was given**, ask:

> "Which feature do you want to review? Tell me:
>
> 1. The service name (e.g. `JL.Commerce.Tecnology.Service`)
> 2. The feature folder name (e.g. `CreateOrderDomainBusiness`)
>
> Or paste the full path to the spec folder."

---

## Step 2 — Read Spec.md

Read `Spec.md` from the resolved folder.

- **Missing or empty** → stop:
  > "Spec.md not found at `{path}`. Create it first — it must describe the feature
  > in business terms before spec-reviewer can run."

---

## Step 3 — Section presence checks (S1–S7)

For each check below, determine whether the section **exists and is non-empty**
(has at least one paragraph or table row beyond the heading itself).

| ID | Required section | Pass condition |
|----|-----------------|----------------|
| S1 | `# Spec: {FeatureName}` heading | Present at the very top of the file |
| S2 | `## Overview` | At least one non-empty paragraph |
| S3 | `## Domain Concepts` | At least one named concept with a definition |
| S4 | `## Business Rules` | Markdown table with at least one `BR-N` row (BR-1, BR-2 …) |
| S5 | `## API Contract` | At least one endpoint block that contains both a request fields table AND at least one response table (any HTTP status) |
| S6 | `## Test Scenarios` | Numbered table with at least one scenario row |
| S7 | `## Async Processing Flow` | **Only required** if any section contains the words `async`, `asynchronous`, `background`, `polling`, or `queue` (case-insensitive). Skip this check and report "skipped: no async keywords" if none found. |

---

## Step 4 — Cross-reference checks (X1–X4)

| ID | Rule |
|----|------|
| X1 | Every `BR-N` rule listed in `## Business Rules` has at least one Test Scenario that explicitly references that rule number or its subject matter. |
| X2 | Every Test Scenario row references a domain behaviour that is traceable to at least one BR or one API endpoint. |
| X3 | Every HTTP status code expected in `## Test Scenarios` (e.g., 202, 422, 404) appears in the `## API Contract` response tables. |
| X4 | No C# class names, namespaces, file paths, or fenced code blocks appear in `Spec.md` outside of a clearly labelled example fence (implementation details must not leak into the Spec). |

---

## Step 5 — Auto-correct structural failures (S-checks only)

Run this step **only when one or more S1–S7 checks failed**. Cross-reference
failures (X1–X4) require human knowledge and are never auto-corrected.

### 5a — Announce

Print to the conversation before touching any file:

```
spec-reviewer: auto-correcting Spec.md — fixing N structural issue(s) found…
```

### 5b — Apply fixes (one Edit per failing S-check)

For each failing S-check, apply the fix described below using the Edit tool,
then immediately print a one-line confirmation of exactly what was changed.

| Check | Fix to apply |
|-------|-------------|
| S1 | Prepend `# Spec: {FeatureName}\n\n` to the file if the heading is missing. |
| S2 | Append an `## Overview` section with the placeholder body below. |
| S3 | Append a `## Domain Concepts` section with the placeholder body below. |
| S4 | Append a `## Business Rules` section with the placeholder table below. |
| S5 | Append an `## API Contract` section with the placeholder block below. |
| S6 | Append a `## Test Scenarios` section with the placeholder table below. |
| S7 | Append an `## Async Processing Flow` section with the placeholder body below. |

Print a correction log line for every fix applied, e.g.:

```
  ✎ [S2] Added ## Overview section — fill in the feature description.
  ✎ [S4] Added ## Business Rules table — replace placeholder rows with real rules.
```

**Placeholder content to insert per section:**

**S2 — Overview**
```markdown
## Overview

<!-- TODO: describe what this feature does and why it exists (2-4 sentences). -->
```

**S3 — Domain Concepts**
```markdown
## Domain Concepts

| Concept | Definition |
|---------|------------|
| <!-- TODO: concept name --> | <!-- TODO: definition --> |
```

**S4 — Business Rules**
```markdown
## Business Rules

| ID   | Rule | Notes |
|------|------|-------|
| BR-1 | <!-- TODO: describe rule --> | |
```

**S5 — API Contract**
```markdown
## API Contract

### `POST /api/v1/{resource}`

**Request**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| <!-- TODO --> | | | |

**Response — 200 OK**

| Field | Type | Description |
|-------|------|-------------|
| <!-- TODO --> | | |
```

**S6 — Test Scenarios**
```markdown
## Test Scenarios

| # | Scenario | Given | When | Then | Rules |
|---|----------|-------|------|------|-------|
| 1 | <!-- TODO: scenario name --> | | | | BR-1 |
```

**S7 — Async Processing Flow**
```markdown
## Async Processing Flow

<!-- TODO: describe the async steps, background jobs, or polling behaviour. -->
```

---

## Step 6 — Report to conversation

Print the following structure exactly:

```
spec-reviewer: {FeatureName}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Sections
  ✓ Overview
  ✓ Domain Concepts  (N entities)
  ✓ Business Rules   (N rules, BR-1–BR-N)
  ✗ API Contract     — <specific gap description>
  ✓ Async Flow       (or "— skipped: no async keywords")
  ✓ Test Scenarios   (N scenarios)

Cross-references
  ✓ All N BR rules have at least one matching test scenario
  ✗ <specific cross-reference failure with BR-N or scenario # cited>

Overall: READY
```

or:

```
Overall: INCOMPLETE

Fix list (priority order):
1. [S4] Add a ## Business Rules table with numbered BR-N rows.
2. [X1] BR-3 has no matching test scenario — add a scenario that covers <BR-3 subject>.
3. [X3] Test Scenario #8 expects HTTP 422 but ## API Contract has no 422 response section.
```

**Rules:**
- Show `✓` for every passing check, `✗` for every failing check.
- `READY` = S1–S6 all pass (S7 if triggered) AND X1–X4 all pass.
- `INCOMPLETE` = one or more checks fail.
- Fix list items cite the Check ID (`[S4]`, `[X1]`, etc.) so the user knows exactly which rule failed.
- Never list passing checks in the fix list.
- Auto-corrected S-checks count as ✓ in the report — no re-read needed; treat them as resolved.
- If Step 5 ran and all S-check failures were corrected but X-check failures remain, add this
  note below the fix list:
  > "Structural sections were auto-corrected. The remaining issues require your input — placeholders
  > mark where content is needed."
- End every output (READY or INCOMPLETE) with the same closing line:

```
Run /spec-reviewer again after fixes, then /sdd-spec when READY.
```

---

## Canonical reference

`specs/JL.Commerce.Tecnology.Service/Features/CreateOrderDomainBusiness/Spec.md`
is the gold-standard example of a complete Spec.md. Read it only when you need
to calibrate a judgment call during cross-reference analysis — not on every run.
