# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

All commands run from the solution root: `JL.Commerce.Tecnology.Service/`

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

### Tests (no implementations yet)

```bash
dotnet test
```

## Architecture

**Hexagonal Architecture + DDD + CQRS** with five projects:

| Layer | Project Suffix | Role |
|---|---|---|
| Domain | `.Domain` | Aggregates, value objects, domain events, exceptions — zero external deps |
| Application | `.Application` | CQRS handlers, FluentValidation validators, AutoMapper profiles, port interfaces |
| Infrastructure.Data | `.Infrastructure.Data` | EF Core + PostgreSQL, repository implementations |
| Infrastructure.Integration | `.Infrastructure.Integration` | MassTransit consumers/publishers, cache |
| Presentation | `.Presentation` | ASP.NET Core Minimal API endpoints, Program.cs wiring |

**Dependency rule**: Domain ← Application ← Infrastructure.* ← Presentation. Infrastructure layers reference Application (to implement its ports), never each other.

### Key conventions

- **Ports** (interfaces) live in `Application/Ports/`. Adapters (implementations) live in the Infrastructure layers.
- **Commands** go in `Application/Commands/<OperationName>/` with three files: command, handler, validator.
- **Queries** go in `Application/Queries/<OperationName>/` with two files: query, handler.
- **Domain events** are collected on aggregates and published via `IEventBus` after persistence; they are not stored in the database (`EntityConfiguration` explicitly ignores them).
- **Strongly typed IDs** use record-based value objects (e.g., `EntityId`).
- MassTransit is pinned to **8.5.5** — do not upgrade to 9+ (requires a paid license).

### API surface

```
POST   /api/v1/entities           → CreateEntityCommand
GET    /api/v1/entities/{id:guid} → GetEntityByIdQuery
GET    /openapi/v1.json           → OpenAPI 3.1 schema
GET    /docs                      → ReDoc UI
```

Endpoints are registered via extension methods in `Presentation/Endpoints/` and called from `Program.cs`.

### Infrastructure configuration

- **Database**: PostgreSQL, connection string key `ConnectionStrings:Database`
- **Auth**: JWT Bearer, configured via `Jwt:Authority` and `Jwt:Audience` — middleware is registered but endpoints are currently unauthenticated
- **MediatR pipeline**: `LoggingBehavior` runs first, then `ValidationBehavior` (throws `ValidationException` on failure)
