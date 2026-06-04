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
POST   /api/v1/entities                   → CreateEntityCommand
GET    /api/v1/entities/{id:guid}         → GetEntityByIdQuery
GET    /openapi/v1.json                   → OpenAPI 3.1 schema
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

### 3. Automated hooks

Two hooks run automatically without any manual action:

| Hook | When | What happens |
|---|---|---|
| `PreToolUse` | Any edit to the app settings config file | Blocks the edit (edit it manually if intentional) |
| `PostToolUse` | Any edit to a `.cs` file | Runs `dotnet build --no-restore` and shows the last 15 lines |

This means: **edit a `.cs` file → build runs automatically**. You see compile errors immediately without running build manually.

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
│   └── src/
│       ├── Domain/
│       ├── Application/
│       ├── Infrastructure.Data/
│       ├── Infrastructure.Integration/
│       └── Presentation/
│
├── JL.DddScaffold.Mcp/              ← custom MCP server
│   └── src/
│       └── JL.DddScaffold.Mcp/
│
├── .claude/
│   ├── agents/
│   │   ├── ddd-reviewer.md          ← DDD compliance reviewer subagent
│   │   └── references/
│   │       └── project-architecture-reference.md
│   └── settings.local.json          ← hooks configuration
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
