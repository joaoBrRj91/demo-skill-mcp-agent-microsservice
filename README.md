# demo-skill-mcp-server-net-core

> **AI-assisted .NET study project** — a production-grade microservice architecture used as a sandbox for learning how to work with Claude Code, custom MCP servers, DDD scaffolding agents, and automated code review.

---

## What this project is

This repo was built as a **study environment** to explore how AI tooling (Claude Code + MCP servers) fits into a real .NET microservice workflow. It has two independent pieces that work together:

| Directory | What it is |
|---|---|
| `JL.Commerce.Tecnology.Service/` | A real microservice — Hexagonal Architecture + DDD + CQRS + PostgreSQL |
| `JL.DddScaffold.Mcp/` | A custom MCP server that scaffolds DDD aggregates following this project's exact conventions |

The service itself is a **catalog product** API. The important part is not what it does, but *how it is built and extended* — using AI to generate code, review it, and keep it consistent with architecture rules.

---

## Tech Stack

| Concern | Library / Version |
|---|---|
| Runtime | .NET 10 |
| Web | ASP.NET Core Minimal API |
| CQRS | MediatR |
| Validation | FluentValidation |
| Object mapping | AutoMapper |
| Persistence | EF Core 10 + PostgreSQL |
| Messaging | MassTransit **8.5.5** (pinned — 9+ requires paid license) |
| API docs | Native `Microsoft.AspNetCore.OpenApi` + ReDoc |
| AI tooling | Claude Code + custom MCP server |

---

## Architecture overview

The service follows **Hexagonal Architecture (Ports & Adapters)** with DDD and CQRS inside five projects:

```
Domain  ←  Application  ←  Infrastructure.*  ←  Presentation
```

```
src/
├── Domain/                  ← zero external NuGet deps, pure business rules
│   ├── Abstractions/        ← AggregateRoot<TId> base class
│   ├── Aggregates/          ← one folder per aggregate (entity + strongly-typed ID)
│   ├── Events/              ← domain events (sealed records)
│   └── Exceptions/          ← domain-specific exceptions
│
├── Application/             ← orchestration only, no business logic
│   ├── Behaviors/           ← MediatR pipeline: LoggingBehavior → ValidationBehavior
│   ├── Commands/            ← CQRS commands, one folder per operation (3 files each)
│   ├── Queries/             ← CQRS queries, one folder per operation (2 files each)
│   ├── DTOs/                ← response shapes
│   ├── Mappings/            ← AutoMapper profiles
│   └── Ports/               ← all interfaces (IXxxRepository, IEventBus)
│
├── Infrastructure.Data/     ← EF Core + PostgreSQL adapters
│   ├── Configurations/      ← IEntityTypeConfiguration per aggregate
│   ├── Context/             ← AppDbContext
│   └── Repositories/        ← port implementations
│
├── Infrastructure.Integration/ ← MassTransit consumers/publishers
│
└── Presentation/            ← Minimal API wiring
    ├── Endpoints/           ← extension methods, one file per aggregate
    └── Program.cs           ← DI composition root
```

### Dependency rule (strictly enforced)

- **Domain** has zero external NuGet packages.
- **Application** only references Domain and port interfaces — never concrete infrastructure.
- **Infrastructure layers** reference Application (to implement ports) but never each other.
- **Presentation** is the only layer that references everything.

---

## API endpoints

```
POST   /api/v1/catalog-products           → CreateCatalogProductCommand
GET    /api/v1/catalog-products           → GetAllCatalogProductsQuery
GET    /api/v1/catalog-products/{id:guid} → GetCatalogProductByIdQuery
PUT    /api/v1/catalog-products/{id:guid} → UpdateCatalogProductCommand
DELETE /api/v1/catalog-products/{id:guid} → DeleteCatalogProductCommand
POST   /api/v1/entities                           → CreateEntityCommand
GET    /api/v1/entities/{id:guid}               → GetEntityByIdQuery
POST   /api/v1/orders                           → CreateOrderCommand (202 Accepted, async)
GET    /api/v1/orders/{transactionId:guid}      → GetOrderStatusQuery
GET    /openapi/v1.json                         → OpenAPI 3.1 schema
GET    /docs                              → ReDoc UI
```

---

## Running the service

```bash
cd JL.Commerce.Tecnology.Service

# restore & build
dotnet restore
dotnet build

# run (requires PostgreSQL)
dotnet run --project "src/Presentation/JL.Commerce.Tecnology.Service.Presentation.csproj"
```

**App configuration** (via `appsettings` or environment variables):

| Key | Purpose |
|---|---|
| `ConnectionStrings:Database` | PostgreSQL connection string |
| `Jwt:Authority` | JWT issuer URL |
| `Jwt:Audience` | JWT audience |

**EF Core migrations:**

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Infrastructure.Data \
  --startup-project src/Presentation

dotnet ef database update \
  --project src/Infrastructure.Data \
  --startup-project src/Presentation
```

---

## AI tooling — the interesting part

This repo was set up to study three AI-assisted workflows:

### 1. DDD Scaffold MCP Server (`ddd-scaffold`)

A custom MCP server registered in `.mcp.json` that generates all boilerplate for a new aggregate across all five layers — ~23 files in a single command, following this project's exact conventions.

**Claude Code picks it up automatically** — no manual startup needed.

#### Scaffold a full aggregate

Ask Claude Code (or call the tool directly):

```
Create an Order aggregate with properties: CustomerId (Guid, required), TotalAmount (decimal), Status (string, maxLength: 50)
```

Claude will call `scaffold_aggregate` with the right parameters and generate:
- Aggregate + strongly-typed ID
- Domain event
- Domain exception
- Command (Create, Update, Delete) + handler + validator
- Query (GetById, GetAll) + handler
- DTO + AutoMapper profile
- Repository port + implementation
- EF Core configuration
- Minimal API endpoints

After generation, four manual steps are always required:

```csharp
// 1. AppDbContext.cs — add DbSet
public DbSet<Order> Orders => Set<Order>();

// 2. Program.cs — register repository
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// 3. Program.cs — register endpoints
app.MapOrderEndpoints();

// 4. Terminal — create migration
dotnet ef migrations add AddOrder --project src/Infrastructure.Data --startup-project src/Presentation
```

#### Add a single command to an existing aggregate

```
Add an Approve command to Order that takes no parameters and returns void
```

Claude calls `scaffold_command` → generates `OrderApproveCommand.cs`, `OrderApproveCommandHandler.cs`, `OrderApproveCommandValidator.cs`.

#### Add a query

```
Add a GetByCustomerId query to Order that returns a list
```

Claude calls `scaffold_query` → generates `GetOrderByCustomerIdQuery.cs` + handler.

#### Preview before writing

```
Preview scaffolding an Invoice aggregate with: Number (string), Amount (decimal)
```

Claude calls `preview_scaffold` — shows all file contents **without writing anything**. Useful for reviewing the generated code before committing.

#### List existing aggregates

```
List all aggregates in the project
```

Claude calls `list_aggregates` → shows each aggregate, whether it has a typed ID, and how many commands and queries it has.

---

### 2. DDD Reviewer Agent

A Claude Code subagent defined in `.claude/agents/ddd-reviewer.md`. It reviews code against all architecture rules and uses the **context7 MCP** to fetch current .NET library docs before suggesting fixes.

**Rules enforced per layer:**

| Layer | Key rules |
|---|---|
| Domain | No NuGet deps, private setters only, factory method `Create()`, domain events via `AddDomainEvent()` |
| Application | Handlers inject only port interfaces, call `Create()` not `new`, no business logic in handlers |
| Infrastructure.Data | `IEntityTypeConfiguration` per aggregate, `DomainEvents` ignored, strongly-typed ID conversions |
| Infrastructure.Integration | MassTransit 8.5.5 only, consumers contain no business logic |
| Presentation | Endpoints dispatch via MediatR only, never call repos directly |

**To invoke it**, tell Claude Code:

```
Review the Order aggregate I just created for DDD compliance
```

Claude launches the `ddd-reviewer` subagent, which reads the canonical reference files, applies the rules, and reports `PASS` or `VIOLATION` with fix suggestions grounded in current API docs.

---

### 3. Security Reviewer Agent

A Claude Code subagent defined in `.claude/agents/security-reviewer.md`. It scans code for security issues using **Semgrep** (with a static-analysis fallback) and uses the **context7 MCP** to ground fix suggestions in current .NET library docs.

**Checks performed:**

| Category | What is checked |
|---|---|
| Credentials | Hardcoded secrets, JWT placeholders |
| Authorization | Unauthenticated mutation endpoints |
| Middleware | Missing security headers, exception handling |
| Injection | Raw SQL, EF Core interpolation |
| Data exposure | Serialized internals, PII leakage |

**To invoke it**, tell Claude Code:

```
Review the Order aggregate for security issues
```

---

### 4. Validate-Guardrails Orchestrator

Defined in `.claude/agents/validate-guardrails-implementation.md`. This orchestrator runs **ddd-reviewer and security-reviewer in parallel**, cross-references their findings, and writes a consolidated report to `.claude/agents/reports/`.

**Overall assessment values:** `PASS` / `NEEDS_ATTENTION` / `FAIL` / `SCAN_ERROR`

**Invocation modes:**

- **Manual** — tell Claude Code: `"review with guardrails"` or `"run guardrails review"`

The orchestrator classifies changed files by layer (Domain / Application / Infrastructure.Data / Infrastructure.Integration / Presentation), checks agent memory files to skip already-reviewed code, and determines the recommended fix order for any findings.

---

### 5. Spec-Driven Development (SDD) + TDD Workflow

Features are fully specified before code is written, then implemented through a TDD cycle. The workflow runs in three sequential steps.

**Document sequence:**

| File | Who writes it | What goes in it |
|---|---|---|
| `Spec.md` | You (manually) | Business rules only — no file paths, no class names |
| `Plan.md` | `sdd-spec` skill | All five layers: file paths, class names, method signatures, code snippets |
| `Constitution.md` | `sdd-spec` skill | Immutable constraints using RFC 2119 language (MUST / MUST NOT / SHALL), each with a `CON-*` ID |
| `Tests.md` | `sdd-spec` skill | TDD checkbox list with test class files and `[Fact]` method names; tracks SETUP / RED / GREEN / VERIFY stage state |
| `Tasks.md` | `sdd-spec` skill | Checkbox list — one task per file, each referencing the `CON-*` rules it satisfies |

**`specs/` directory layout:**

```
specs/{ServiceName}/Features/{FeatureName}/
├── Spec.md
├── Plan.md
├── Constitution.md
├── Tests.md
└── Tasks.md
```

**Three-step workflow:**

**Step 1 — Write the spec** (manually or interactively):

```
Start the SDD workflow for CreateOrderDomainBusiness
```

The `sdd-spec-init-orchestrator` agent guides you through 5 phases (COLLECTING → DRAFT_GENERATED → AWAITING_CONFIRMATION → COMPLETION). It accepts structured Q&A or detects natural-language input (>100 words) and skips the questionnaire. State persists across sessions so an interrupted flow can be resumed.

**Step 2 — Generate implementation documents:**

```
Run the sdd-spec skill for CreateOrderDomainBusiness
```

`sdd-spec` generates Plan → Constitution → Tests → Tasks in order; each document uses all prior ones as context. The `sdd-spec-create` agent wraps this with context7 enrichment, fetching current MediatR / FluentValidation / AutoMapper / MassTransit API signatures and embedding them in `Tasks.md`. Skips any file that already exists and is non-empty.

**Step 3 — Implement with TDD:**

```
/sdd-next-task CreateOrderDomainBusiness
```

Runs the 4-stage TDD cycle automatically:

| Stage | Name | What happens |
|---|---|---|
| 1 | SETUP | Creates `UnitTests.csproj` and `IntegrationTests.csproj`, adds both to the solution |
| 2 | RED | Writes all test `.cs` files; no build (types don't exist yet) |
| 3 | GREEN | Writes production files one task at a time; per-task build check (2 fix rounds max) |
| 4 | VERIFY | Runs `dotnet test`; reports pass/fail per test |

Progress is checkpointed via checkbox state in `Tests.md` and `Tasks.md` — safe to stop and resume across sessions. If production code already exists when RED completes, the skill skips GREEN and jumps directly to a build + VERIFY check (CATCH-UP mode).

**Working example:** `specs/JL.Commerce.Tecnology.Service/Features/CreateOrderDomainBusiness/`

---

## Key patterns and conventions

### Aggregate pattern

```csharp
// Domain/Aggregates/Order/Order.cs
public sealed class Order : AggregateRoot<OrderId>
{
    private Order() { }   // required by EF Core

    public string Status { get; private set; } = default!;

    public static Order Create(string status)
    {
        var order = new Order
        {
            Id = OrderId.New(),
            Status = status
        };
        order.AddDomainEvent(new OrderCreatedEvent(order.Id));
        return order;
    }
}

// Domain/Aggregates/Order/OrderId.cs
public sealed record OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
```

### Command + handler + validator pattern

```csharp
// sealed record for the command
public sealed record CreateOrderCommand(string Status) : IRequest<Guid>;

// handler — only calls factory + repository
public sealed class CreateOrderCommandHandler(IOrderRepository repository)
    : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = Order.Create(cmd.Status);
        await repository.AddAsync(order, ct);
        return order.Id.Value;
    }
}

// validator
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
    }
}
```

### MediatR pipeline execution order

```
Request → LoggingBehavior → ValidationBehavior → Handler → Response
```

`ValidationBehavior` throws `ValidationException` on failure, which the presentation layer maps to HTTP 400.

### EF Core configuration pattern

```csharp
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => new OrderId(value));
        builder.Ignore(o => o.DomainEvents); // always required
    }
}
```

---

## Study tips

### Exploring AI-assisted scaffolding

1. **Start with `preview_scaffold`** before writing any files. Read the generated code and understand the pattern before committing.
2. **Compare the generated files** with the canonical examples in `Domain/Aggregates/CatalogProduct/` and `Application/Commands/CreateCatalogProduct/`.
3. **Intentionally break a rule** (e.g. add a public setter to an aggregate) then run the `ddd-reviewer` agent to see it catch and explain the violation.

### Understanding the architecture

1. Follow the dependency rule by reading `.csproj` files — Domain should have zero `PackageReference` elements.
2. Trace a full request from endpoint to database: `CatalogProductEndpoints.cs` → `CreateCatalogProductCommand` → `CreateCatalogProductCommandHandler` → `ICatalogProductRepository` → `CatalogProductRepository` → `AppDbContext`.
3. Find where domain events are collected (`AddDomainEvent` in the aggregate) and where they are published (look for `IEventBus` usage in handlers).

### Extending the project

1. Add a new aggregate using the MCP scaffold tool, then fill in any domain-specific logic.
2. Add a custom command to an existing aggregate (e.g. `Publish` on `CatalogProduct`).
3. Add an integration consumer that reacts to a domain event.
4. Add JWT authentication to the endpoints (middleware is registered but endpoints are currently open).

### Customizing the MCP server

The scaffold templates live in `JL.DddScaffold.Mcp/`. Modifying the templates lets you explore how MCP tools are built and how they integrate with Claude Code. The server is a plain .NET console app using the MCP SDK.

---

## Project structure (top-level)

```
.
├── JL.Commerce.Tecnology.Service/   ← microservice
│   ├── src/
│   │   ├── Domain/
│   │   ├── Application/
│   │   ├── Infrastructure.Data/
│   │   ├── Infrastructure.Integration/
│   │   └── Presentation/
│   └── tests/
│       ├── UnitTests/               ← xUnit + Moq unit tests
│       └── IntegrationTests/        ← xUnit integration tests
│
├── JL.DddScaffold.Mcp/              ← custom MCP server
│   └── src/
│       └── JL.DddScaffold.Mcp/
│
├── specs/                           ← SDD feature specifications
│   └── JL.Commerce.Tecnology.Service/
│       └── Features/
│           └── CreateOrderDomainBusiness/
│               ├── Spec.md          ← business rules (written manually)
│               ├── Plan.md          ← implementation plan (generated)
│               ├── Constitution.md  ← immutable constraints (generated)
│               ├── Tests.md         ← TDD checkpoint list (generated)
│               └── Tasks.md         ← checkbox task list (generated)
│
├── .claude/
│   ├── agents/
│   │   ├── ddd-reviewer.md                      ← DDD compliance reviewer subagent
│   │   ├── security-reviewer.md                 ← OWASP/Semgrep security reviewer subagent
│   │   ├── validate-guardrails-implementation.md ← orchestrator (runs both in parallel)
│   │   ├── sdd-spec-init-orchestrator.md        ← interactive spec creation orchestrator
│   │   ├── sdd-spec-create.md                   ← context7-enriched document generator
│   │   ├── memories/                            ← per-agent memory files
│   │   │   ├── ddd-reviewer-memory.md
│   │   │   └── security-reviewer-memory.md
│   │   ├── references/
│   │   │   └── project-architecture-reference.md
│   │   └── reports/                             ← guardrails consolidated reports
│   ├── skills/
│   │   ├── scaffold-aggregate/SKILL.md          ← wraps MCP scaffold_aggregate tool
│   │   ├── sdd-spec/SKILL.md                   ← generates Plan/Constitution/Tests/Tasks from Spec
│   │   └── sdd-next-task/SKILL.md              ← TDD 4-stage cycle (SETUP → RED → GREEN → VERIFY)
│   └── settings.local.json                      ← Claude Code settings
│
├── .mcp.json                        ← MCP server registration
└── CLAUDE.md                        ← instructions for Claude Code
```

---

## Building the MCP server

```bash
dotnet restore JL.DddScaffold.Mcp/src/JL.DddScaffold.Mcp/JL.DddScaffold.Mcp.csproj
dotnet build   JL.DddScaffold.Mcp/src/JL.DddScaffold.Mcp/JL.DddScaffold.Mcp.csproj
```

The server is registered in `.mcp.json` and started automatically by Claude Code when you open this workspace — no manual startup needed.
