# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with this repository.

## Repository layout

Two independent projects live at the root:

| Directory | Purpose |
|---|---|
| `JL.Commerce.Tecnology.Service/` | Main microservice (Hexagonal Architecture + DDD + CQRS) |
| `JL.DddScaffold.Mcp/` | Custom MCP server that scaffolds DDD aggregates for the service |

---

## JL.Commerce.Tecnology.Service

### Build & Run

All commands run from `JL.Commerce.Tecnology.Service/`:

```bash
dotnet restore
dotnet build
dotnet run --project "src/Presentation/JL.Commerce.Tecnology.Service.Presentation.csproj"
```

### EF Core Migrations

```bash
dotnet ef migrations add <Name> --project src/Infrastructure.Data --startup-project src/Presentation
dotnet ef database update --project src/Infrastructure.Data --startup-project src/Presentation
```

### Tests

```bash
dotnet test
```

No test implementations yet — `tests/UnitTests/` and `tests/IntegrationTests/` are empty.

---

### Architecture

**Hexagonal Architecture + DDD + CQRS** with five projects:

| Layer | Project Suffix | Role |
|---|---|---|
| Domain | `.Domain` | Aggregates, value objects, domain events, exceptions — zero external deps |
| Application | `.Application` | CQRS handlers, FluentValidation validators, AutoMapper profiles, port interfaces |
| Infrastructure.Data | `.Infrastructure.Data` | EF Core + PostgreSQL, repository implementations |
| Infrastructure.Integration | `.Infrastructure.Integration` | MassTransit consumers/publishers, cache |
| Presentation | `.Presentation` | ASP.NET Core Minimal API endpoints, Program.cs wiring |

**Dependency rule**: Domain ← Application ← Infrastructure.* ← Presentation. Infrastructure layers reference Application (to implement its ports), never each other.

---

### Naming conventions

| Artifact | C# type | Path |
|---|---|---|
| Aggregate | `sealed class : AggregateRoot<TId>` | `Domain/Aggregates/{Name}/{Name}.cs` |
| Strongly-typed ID | `sealed record {Name}Id(Guid Value)` | `Domain/Aggregates/{Name}/{Name}Id.cs` |
| Domain event | `sealed record : IDomainEvent` | `Domain/Events/{Name}CreatedEvent.cs` |
| Domain exception | `sealed class : Exception` | `Domain/Exceptions/{Name}NotFoundException.cs` |
| Command | `sealed record : IRequest<T>` | `Application/Commands/{Op}/{Op}Command.cs` |
| Command handler | `sealed class` | `Application/Commands/{Op}/{Op}CommandHandler.cs` |
| Command validator | `sealed class : AbstractValidator<T>` | `Application/Commands/{Op}/{Op}CommandValidator.cs` |
| Query | `sealed record : IRequest<T>` | `Application/Queries/{Op}/{Op}Query.cs` |
| Query handler | `sealed class` | `Application/Queries/{Op}/{Op}QueryHandler.cs` |
| DTO | `record` | `Application/DTOs/{Name}Dto.cs` |
| AutoMapper profile | `sealed class : Profile` | `Application/Mappings/{Name}MappingProfile.cs` |
| Repository port | `interface` | `Application/Ports/I{Name}Repository.cs` |
| EF configuration | `sealed class : IEntityTypeConfiguration<T>` | `Infrastructure.Data/Configurations/{Name}Configuration.cs` |
| Repository impl | `sealed class` | `Infrastructure.Data/Repositories/{Name}Repository.cs` |
| Endpoints | `static class` with extension method | `Presentation/Endpoints/{Name}Endpoints.cs` |

**Records vs classes:**
- `record` — commands, queries, DTOs, domain events, strongly-typed IDs
- `sealed class` — aggregates, handlers, validators, repositories, EF configs, mapping profiles

---

### AggregateRoot base class

All aggregates inherit `AggregateRoot<TId>` (`Domain/Abstractions/AggregateRoot.cs`):

- `TId Id { get; protected set; }` — the aggregate's identity
- `IReadOnlyCollection<IDomainEvent> DomainEvents` — event queue (not persisted)
- `protected void AddDomainEvent(IDomainEvent)` — call only from within the aggregate
- `void ClearDomainEvents()` — called by `IEventBus` after publishing

`IDomainEvent` (marker interface) is defined in the same file.

EF Core constraint: every aggregate **must** have a `private {Name}() {}` parameterless constructor. EF configuration must call `.Ignore(e => e.DomainEvents)`.

**Strongly-typed ID pattern:**

```csharp
public sealed record CatalogProductId(Guid Value)
{
    public static CatalogProductId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
```

---

### Key conventions

- **Ports** (interfaces) live in `Application/Ports/`. Adapters (implementations) live in the Infrastructure layers.
- **Commands** go in `Application/Commands/<OperationName>/` with three files: command, handler, validator.
- **Queries** go in `Application/Queries/<OperationName>/` with two files: query, handler.
- **Domain events** are collected on aggregates and published via `IEventBus` after persistence; they are not stored in the database.
- MassTransit is pinned to **8.5.5** — do not upgrade to 9+ (requires a paid license).
- **MediatR pipeline**: `LoggingBehavior` runs first, then `ValidationBehavior` (throws `ValidationException` on failure).

---

### API surface

```
POST   /api/v1/catalog-products           → CreateCatalogProductCommand
GET    /api/v1/catalog-products           → GetAllCatalogProductsQuery
GET    /api/v1/catalog-products/{id:guid} → GetCatalogProductByIdQuery
PUT    /api/v1/catalog-products/{id:guid} → UpdateCatalogProductCommand
DELETE /api/v1/catalog-products/{id:guid} → DeleteCatalogProductCommand
POST   /api/v1/entities                   → CreateEntityCommand
GET    /api/v1/entities/{id:guid}         → GetEntityByIdQuery
GET    /openapi/v1.json                   → OpenAPI 3.1 schema
GET    /docs                              → ReDoc UI
```

Endpoints are registered via extension methods in `Presentation/Endpoints/` and called from `Program.cs`.

---

### Infrastructure configuration

- **Database**: PostgreSQL, connection string key `ConnectionStrings:Database`
- **Auth**: JWT Bearer, configured via `Jwt:Authority` and `Jwt:Audience` — middleware is registered but endpoints are currently unauthenticated

---

### Hooks (automated behaviors)

Two hooks are configured in `.claude/settings.local.json` and run automatically:

| Hook | Trigger | Behavior |
|---|---|---|
| PreToolUse | Edit/Write targeting the app settings config file | Blocks the edit (exit 2). Edit that file manually if intentional. |
| PostToolUse | Edit/Write to any `.cs` file | Runs `dotnet build --no-restore` in the service root and shows the last 15 lines. No need to run build manually after editing C# files. |

---

## JL.DddScaffold.Mcp — DDD Scaffold MCP Server

A custom MCP server registered as `ddd-scaffold` in `.mcp.json`. It generates files following this project's exact DDD + Hexagonal conventions. **Started automatically by Claude Code** — no manual startup needed.

### Build

```bash
dotnet restore JL.DddScaffold.Mcp/src/JL.DddScaffold.Mcp/JL.DddScaffold.Mcp.csproj
dotnet build JL.DddScaffold.Mcp/src/JL.DddScaffold.Mcp/JL.DddScaffold.Mcp.csproj
```

### Available tools

#### `scaffold_aggregate`
Generates all ~23 files for a complete aggregate across all layers. Skips files that already exist.

| Parameter | Type | Description |
|---|---|---|
| `srcPath` | string | Absolute path to the service's `src/` directory |
| `rootNamespace` | string | Root namespace, e.g. `JL.Commerce.Tecnology.Service` |
| `aggregateName` | string | PascalCase aggregate name, e.g. `Order` |
| `propertiesJson` | string | JSON array of property definitions (see schema below) |

**Post-generation manual steps (always required):**
1. Add `DbSet<{Name}> {PluralName}` to `AppDbContext`
2. Register DI in `Program.cs`: `builder.Services.AddScoped<I{Name}Repository, {Name}Repository>();`
3. Register endpoints in `Program.cs`: `app.Map{Name}Endpoints();`
4. Create EF migration: `dotnet ef migrations add Add{Name} --project src/Infrastructure.Data --startup-project src/Presentation`

#### `scaffold_command`
Adds a single custom command (command + handler + validator) to an existing aggregate.

| Parameter | Type | Description |
|---|---|---|
| `srcPath` | string | Absolute path to `src/` |
| `rootNamespace` | string | Root namespace |
| `aggregateName` | string | Existing aggregate name |
| `operationName` | string | Operation name without aggregate prefix, e.g. `Approve` → `OrderApproveCommand` |
| `returnsGuid` | bool | `true` = `IRequest<Guid>`, `false` = `IRequest` (void) |
| `propertiesJson` | string | JSON array of command parameters; include `{"name":"Id","type":"Guid"}` first for mutations on existing entities |

#### `scaffold_query`
Adds a single custom query (query + handler) to an existing aggregate.

| Parameter | Type | Description |
|---|---|---|
| `srcPath` | string | Absolute path to `src/` |
| `rootNamespace` | string | Root namespace |
| `aggregateName` | string | Existing aggregate name |
| `operationName` | string | Query operation name |
| `returnsSingleItem` | bool | `true` = returns single DTO, `false` = returns list |
| `parametersJson` | string | JSON array of query parameters |

#### `preview_scaffold`
Dry-run of `scaffold_aggregate` — shows all file contents without writing anything. Use to review generated code before committing to file creation. Same parameters as `scaffold_aggregate`.

#### `list_aggregates`
Lists all aggregates in `Domain/Aggregates/` with summary info (has ID?, command count, query count).

| Parameter | Type | Description |
|---|---|---|
| `srcPath` | string | Absolute path to `src/` |

### propertiesJson schema

```json
[
  {"name": "Name", "type": "string", "maxLength": 200, "required": true},
  {"name": "Price", "type": "decimal", "precision": 18, "scale": 2},
  {"name": "IsActive", "type": "bool"}
]
```

Supported types: `string`, `int`, `decimal`, `bool`, `DateTime`, `Guid`, `long`.

Do **NOT** include `Id`, `CreatedAt`, or `UpdatedAt` — those are always auto-generated.

---

## ddd-reviewer agent

Reviews new or modified code for strict DDD compliance against all project conventions.

**Location:** `.claude/agents/ddd-reviewer.md`

**Invocation:** Claude Code subagent — use the Agent tool with `subagent_type: ddd-reviewer`.

The agent applies rules across all layers (Domain, Application, Infrastructure.Data, Infrastructure.Integration, Presentation) and calls context7 MCP to retrieve up-to-date .NET library docs before writing fix suggestions.
