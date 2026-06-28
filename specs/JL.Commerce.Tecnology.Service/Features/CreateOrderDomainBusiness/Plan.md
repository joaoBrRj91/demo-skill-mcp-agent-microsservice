# Implementation Plan: Create Order Domain Business

## Architecture Overview

Follows the project's Hexagonal Architecture + DDD + CQRS pattern across five layers:
- **Domain**: Order aggregate, value objects, domain events, domain exceptions
- **Application**: CreateOrderCommand, ProcessOrderCommand, GetOrderStatusQuery, ports (IOrderRepository, IPaymentGateway), DTOs, AutoMapper profile
- **Infrastructure.Data**: OrderRepository, OrderConfiguration (EF Core owned types), AppDbContext DbSet
- **Infrastructure.Integration**: MockPaymentGateway, OrderCreatedConsumer (MassTransit)
- **Presentation**: OrderEndpoints, Program.cs wiring

---

## Domain Layer

### Aggregate: Order
`Domain/Aggregates/Order/Order.cs`
- Inherits `AggregateRoot<OrderId>`
- Properties: `UserId (Guid)`, `Status (OrderStatus)`, `ErrorMessage (string?)`, `Items (IReadOnlyList<OrderItem>)`, `Payment (PaymentDetails)`, `Address (ShippingAddress)`, `CreatedAt (DateTime)`, `UpdatedAt (DateTime?)`
- `private Order() {}` — EF Core parameterless constructor
- `static Order Create(Guid userId, IReadOnlyList<OrderItem> items, PaymentDetails payment, ShippingAddress address)` — sets Status=Processing, raises OrderCreatedEvent; throws OrderItemsEmptyException if items is empty
- `MarkAsProcessed()` — Status=Processed, raises OrderProcessedEvent; throws InvalidOrderStatusTransitionException if Status != Processing
- `MarkAsError(string message)` — Status=Error, sets ErrorMessage, raises OrderErrorEvent; throws InvalidOrderStatusTransitionException if Status != Processing

### Strongly-Typed ID
`Domain/Aggregates/Order/OrderId.cs` — sealed record(Guid Value) with static New() and ToString() override

### Value Objects
Defined as classes (not records) because EF Core owns them.

- `Domain/Aggregates/Order/OrderItem.cs` — CatalogProductId (Guid), Quantity (int), UnitPrice (decimal); all properties are init-only — UnitPrice is captured at creation and never recalculated (BR-3)
- `Domain/Aggregates/Order/PaymentDetails.cs` — Method (PaymentMethod), CardNumber (string?), HolderName (string?), Expiry (string?), Cvv (string?), PixKey (string?)
- `Domain/Aggregates/Order/ShippingAddress.cs` — Street, City, State, ZipCode, Country (all string)

### Enumerations
- `Domain/Aggregates/Order/OrderStatus.cs` — `Processing`, `Processed`, `Error`
- `Domain/Aggregates/Order/PaymentMethod.cs` — `CreditCard`, `Pix`

### Domain Events
- `Domain/Events/OrderCreatedEvent.cs` — `sealed record OrderCreatedEvent(OrderId OrderId) : IDomainEvent`
- `Domain/Events/OrderProcessedEvent.cs` — `sealed record OrderProcessedEvent(OrderId OrderId) : IDomainEvent`
- `Domain/Events/OrderErrorEvent.cs` — `sealed record OrderErrorEvent(OrderId OrderId, string ErrorMessage) : IDomainEvent`

### Domain Exceptions
- `Domain/Exceptions/OrderNotFoundException.cs`
- `Domain/Exceptions/OrderItemsEmptyException.cs`
- `Domain/Exceptions/InvalidOrderStatusTransitionException.cs`

---

## Application Layer

### Ports

`Application/Ports/IOrderRepository.cs`
```
AddAsync(Order order, CancellationToken ct)
GetByIdAsync(OrderId id, CancellationToken ct) → Order?
UpdateAsync(Order order, CancellationToken ct)
```

`Application/Ports/IPaymentGateway.cs`
- `Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken ct)`
- Supporting records in the same file:
  - `PaymentRequest(Guid OrderId, decimal TotalAmount, PaymentDetails Payment)`
  - `PaymentResult(bool Success, string? ErrorMessage)`

---

### Command: CreateOrderCommand
`Application/Commands/CreateOrder/`

**Command** — `sealed record CreateOrderCommand(...) : IRequest<Guid>`
- Properties: `UserId (Guid)`, `Items (IReadOnlyList<OrderItemInput>)`, `Payment (PaymentDetailsInput)`, `Address (ShippingAddressInput)`
- Supporting input records (defined in the same file):
  - `OrderItemInput(Guid CatalogProductId, int Quantity, decimal UnitPrice)`
  - `PaymentDetailsInput(PaymentMethod Method, string? CardNumber, string? HolderName, string? Expiry, string? Cvv, string? PixKey)`
  - `ShippingAddressInput(string Street, string City, string State, string ZipCode, string Country)`

**Handler** — `sealed class CreateOrderCommandHandler`
1. Map `OrderItemInput` list → `OrderItem` list
2. Map `PaymentDetailsInput` → `PaymentDetails`
3. Map `ShippingAddressInput` → `ShippingAddress`
4. `Order.Create(userId, items, payment, address)`
5. `await repository.AddAsync(order, ct)`
6. `await publishEndpoint.Publish(new OrderCreatedEvent(order.Id), ct)`
7. Return `order.Id.Value`

**Validator** — `sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>`
- `UserId`: NotEmpty
- `Items`: NotEmpty; each element: CatalogProductId NotEmpty, Quantity ≥ 1, UnitPrice > 0
- `Payment.Method`: Must be a valid `PaymentMethod` enum value
- When `Method = CreditCard`: CardNumber NotEmpty (13–19 chars), HolderName NotEmpty, Expiry matches `^(0[1-9]|1[0-2])\/\d{2}$`, Cvv matches `^\d{3,4}$`
- When `Method = Pix`: PixKey NotEmpty
- Address: Street, City, State, ZipCode, Country all NotEmpty

---

### Command: ProcessOrderCommand (internal — not exposed via HTTP)
`Application/Commands/ProcessOrder/`

**Command** — `sealed record ProcessOrderCommand(Guid OrderId) : IRequest`

**Handler** — `sealed class ProcessOrderCommandHandler`
1. `var order = await repository.GetByIdAsync(new OrderId(cmd.OrderId), ct)` — throw `OrderNotFoundException` if null
2. Compute `totalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity)`
3. Build `PaymentRequest(order.Id.Value, totalAmount, order.Payment)`
4. `var result = await paymentGateway.ProcessAsync(request, ct)`
5. If `result.Success`: `order.MarkAsProcessed()`
6. If `!result.Success`: `order.MarkAsError(result.ErrorMessage!)`
7. `await repository.UpdateAsync(order, ct)`

No validator needed (internal command; data integrity guaranteed by consumer).

---

### Query: GetOrderStatusQuery
`Application/Queries/GetOrderStatus/`

**Query** — `sealed record GetOrderStatusQuery(Guid TransactionId) : IRequest<OrderPollingDto?>`

**Handler** — `sealed class GetOrderStatusQueryHandler`
1. `var order = await repository.GetByIdAsync(new OrderId(query.TransactionId), ct)` — return null if not found
2. If `order.Status == OrderStatus.Processing`: map to thin DTO (TransactionId + Status only, Order = null)
3. Otherwise: map to full DTO including OrderDto

---

### DTOs
`Application/DTOs/`

- `OrderPollingDto` — `TransactionId (Guid)`, `Status (string)`, `ErrorMessage (string?)`, `Order (OrderDto?)`
- `OrderDto` — `Id (Guid)`, `UserId (Guid)`, `Items (IReadOnlyList<OrderItemDto>)`, `PaymentMethod (string)`, `Address (ShippingAddressDto)`, `CreatedAt (DateTime)`, `UpdatedAt (DateTime?)`
- `OrderItemDto` — `CatalogProductId (Guid)`, `Quantity (int)`, `UnitPrice (decimal)`
- `ShippingAddressDto` — `Street`, `City`, `State`, `ZipCode`, `Country` (all string)

### AutoMapper Profile
`Application/Mappings/OrderMappingProfile.cs`
- `Order → OrderPollingDto`: `Status = src.Status.ToString()`, `Order = null when Processing`
- `Order → OrderDto`: `PaymentMethod = src.Payment.Method.ToString()`, nested maps for Items and Address
- `OrderItem → OrderItemDto`
- `ShippingAddress → ShippingAddressDto`

---

## Infrastructure.Data

### EF Core Configuration
`Infrastructure.Data/Configurations/OrderConfiguration.cs` — `sealed class : IEntityTypeConfiguration<Order>`

Key requirements:
- `entity.Ignore(e => e.DomainEvents)`
- `entity.Property(e => e.Status).HasConversion<string>()`
- `entity.OwnsMany(e => e.Items, b => { b.ToTable("OrderItems"); ... })`
- `entity.OwnsOne(e => e.Payment, b => { b.Property(p => p.Method).HasConversion<string>(); ... })`
- `entity.OwnsOne(e => e.Address, ...)`

### Repository
`Infrastructure.Data/Repositories/OrderRepository.cs` — `sealed class OrderRepository : IOrderRepository`
- Owned types load automatically with the aggregate root in EF Core (no explicit Include needed)
- Standard Add / Update / SaveChangesAsync pattern matching existing repositories

### AppDbContext
Add `DbSet<Order> Orders { get; set; }` to `AppDbContext.cs`.

### EF Migration
After all wiring is complete:
```
dotnet ef migrations add AddOrder --project src/Infrastructure.Data --startup-project src/Presentation
dotnet ef database update --project src/Infrastructure.Data --startup-project src/Presentation
```

---

## Infrastructure.Integration

### Mock Payment Gateway
`Infrastructure.Integration/PaymentGateway/MockPaymentGateway.cs` — `sealed class MockPaymentGateway : IPaymentGateway`
- Default: returns `PaymentResult(Success: true, ErrorMessage: null)`
- No external dependency; safe to replace with a real implementation later without touching other layers

### MassTransit Consumer
`Infrastructure.Integration/Messaging/Consumers/OrderCreatedConsumer.cs` — `sealed class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>`
- Inject `ISender` and `ILogger<OrderCreatedConsumer>`
- On Consume: log OrderId, send `ProcessOrderCommand(context.Message.OrderId.Value)` via MediatR sender

---

## Presentation Layer

### Endpoints
`Presentation/Endpoints/OrderEndpoints.cs` — static class with `MapOrderEndpoints(this IEndpointRouteBuilder app)` extension method

```
POST /api/v1/orders                      → CreateOrderCommand → TypedResults.Accepted(..., { transactionId, status })
GET  /api/v1/orders/{transactionId:guid} → GetOrderStatusQuery → TypedResults.Ok(dto) | TypedResults.NotFound()
```

POST returns HTTP 202 Accepted (not 201 Created) — the resource is not yet fully formed at response time.

### Program.cs Additions
1. `builder.Services.AddScoped<IOrderRepository, OrderRepository>()`
2. `builder.Services.AddScoped<IPaymentGateway, MockPaymentGateway>()`
3. In MassTransit configuration: `AddConsumer<OrderCreatedConsumer>()`
4. `app.MapOrderEndpoints()`

---

## File Checklist

### Domain (13 files)
- `Domain/Aggregates/Order/OrderId.cs`
- `Domain/Aggregates/Order/Order.cs`
- `Domain/Aggregates/Order/OrderItem.cs`
- `Domain/Aggregates/Order/PaymentDetails.cs`
- `Domain/Aggregates/Order/ShippingAddress.cs`
- `Domain/Aggregates/Order/OrderStatus.cs`
- `Domain/Aggregates/Order/PaymentMethod.cs`
- `Domain/Events/OrderCreatedEvent.cs`
- `Domain/Events/OrderProcessedEvent.cs`
- `Domain/Events/OrderErrorEvent.cs`
- `Domain/Exceptions/OrderNotFoundException.cs`
- `Domain/Exceptions/OrderItemsEmptyException.cs`
- `Domain/Exceptions/InvalidOrderStatusTransitionException.cs`

### Application (12 files)
- `Application/Ports/IOrderRepository.cs`
- `Application/Ports/IPaymentGateway.cs`
- `Application/Commands/CreateOrder/CreateOrderCommand.cs`
- `Application/Commands/CreateOrder/CreateOrderCommandHandler.cs`
- `Application/Commands/CreateOrder/CreateOrderCommandValidator.cs`
- `Application/Commands/ProcessOrder/ProcessOrderCommand.cs`
- `Application/Commands/ProcessOrder/ProcessOrderCommandHandler.cs`
- `Application/Queries/GetOrderStatus/GetOrderStatusQuery.cs`
- `Application/Queries/GetOrderStatus/GetOrderStatusQueryHandler.cs`
- `Application/DTOs/OrderPollingDto.cs` (+ OrderDto, OrderItemDto, ShippingAddressDto)
- `Application/Mappings/OrderMappingProfile.cs`

### Infrastructure.Data (2 files + 1 edit)
- `Infrastructure.Data/Configurations/OrderConfiguration.cs`
- `Infrastructure.Data/Repositories/OrderRepository.cs`
- `Infrastructure.Data/AppDbContext.cs` — add DbSet<Order>

### Infrastructure.Integration (2 files)
- `Infrastructure.Integration/PaymentGateway/MockPaymentGateway.cs`
- `Infrastructure.Integration/Messaging/Consumers/OrderCreatedConsumer.cs`

### Presentation (1 file + 1 edit)
- `Presentation/Endpoints/OrderEndpoints.cs`
- `Presentation/Program.cs` — 4 additions
