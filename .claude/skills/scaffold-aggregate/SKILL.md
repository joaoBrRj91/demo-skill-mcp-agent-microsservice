---
name: scaffold-aggregate
description: >
  Scaffolds a complete DDD aggregate (~23 files across all layers) for
  JL.Commerce.Tecnology.Service using the ddd-scaffold MCP. Use whenever the user
  wants to add a new aggregate — phrases like "add an Order", "create a Customer
  aggregate", "I need a new entity" all map to this skill.
---

# scaffold-aggregate

Scaffolds a full DDD aggregate for **JL.Commerce.Tecnology.Service** using the
`mcp__ddd-scaffold` MCP server. Covers all layers: Domain, Application,
Infrastructure.Data, and Presentation (~23 files).

## Hardcoded project constants (do not ask the user for these)

- `srcPath`: `C:\Users\joaon\Projetos\IA\Study Projects\demo-skill-mcp-server-net-core\JL.Commerce.Tecnology.Service\src`
- `rootNamespace`: `JL.Commerce.Tecnology.Service`

---

## Step 1 — Aggregate name

Use the argument passed to the skill (e.g. `/scaffold-aggregate Order` → `Order`).
Must be PascalCase. If no argument was given, ask:

> "What is the name of the new aggregate? (PascalCase, e.g. `Order`, `Invoice`)"

---

## Step 2 — Properties

Ask the user to define properties. Explain:

> "Define the properties for **{AggregateName}**. Do NOT include `Id`, `CreatedAt`,
> or `UpdatedAt` — those are always auto-generated.
>
> Each property needs `name` and `type`. Supported types:
> `string`, `int`, `decimal`, `bool`, `DateTime`, `Guid`, `long`
>
> Optional per property: `maxLength` (string), `required` (bool, default true),
> `precision`/`scale` (decimal), `isNullable` (bool), `isUnique` (bool)
>
> Example:
> ```json
> [
>   { "name": "CustomerName", "type": "string", "maxLength": 200 },
>   { "name": "TotalAmount",  "type": "decimal", "precision": 18, "scale": 2 },
>   { "name": "IsConfirmed",  "type": "bool" }
> ]
> ```
>
> Paste JSON or describe in plain English — I'll convert."

If the user describes properties in plain English, convert to JSON and confirm before
continuing. At least one property is required.

---

## Step 3 — Preview

Call `mcp__ddd-scaffold__preview_scaffold`:

```
srcPath        = "C:\Users\joaon\Projetos\IA\Study Projects\demo-skill-mcp-server-net-core\JL.Commerce.Tecnology.Service\src"
rootNamespace  = "JL.Commerce.Tecnology.Service"
aggregateName  = "{AggregateName}"
propertiesJson = "{JSON array}"
```

Show the user the list of files to be generated and the key generated types (aggregate
class, DTO, commands, queries). Then ask:

> "Does this look right? **yes** to scaffold, **no** to cancel, or describe changes
> to adjust properties and re-preview."

Loop preview until user approves.

---

## Step 4 — Scaffold

Call `mcp__ddd-scaffold__scaffold_aggregate` with the same parameters. Report how
many files were written. If any files were skipped (already exist), list them and warn.

---

## Step 5 — Post-scaffold checklist

Print this checklist with **{AggregateName}** substituted throughout:

---

**4 manual steps required to wire up {AggregateName}:**

**1. AppDbContext** — `src/Infrastructure.Data/Context/AppDbContext.cs`
```csharp
public DbSet<{AggregateName}> {AggregateName}s => Set<{AggregateName}>();
```
> Note: verify the plural is correct (e.g. `Address` → `Addresses`, not `Addresss`).

**2. DI registration** — `src/Presentation/Program.cs`
```csharp
builder.Services.AddScoped<I{AggregateName}Repository, {AggregateName}Repository>();
```

**3. Endpoint registration** — `src/Presentation/Program.cs`
```csharp
app.Map{AggregateName}Endpoints();
```

**4. EF Core migration** — run from `JL.Commerce.Tecnology.Service/`
```bash
dotnet ef migrations add Add{AggregateName} \
  --project src/Infrastructure.Data \
  --startup-project src/Presentation
```

---

Note: `dotnet build` runs automatically after each generated file (PostToolUse hook).
Build errors during scaffolding are expected until all files are created — the scaffold
writes all layers before the project is complete.
