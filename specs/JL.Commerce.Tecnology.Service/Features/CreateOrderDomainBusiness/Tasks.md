# Tasks — CreateOrderDomainBusiness

## Domain
- [x] Create `OrderId` strongly-typed ID — `sealed record(Guid Value)` with `New()` and `ToString()` (`Domain/Aggregates/Order/OrderId.cs`)
- [x] Create `OrderStatus` enumeration — `Processing`, `Processed`, `Error` (`Domain/Aggregates/Order/OrderStatus.cs`)
- [x] Create `PaymentMethod` enumeration — `CreditCard`, `Pix` (`Domain/Aggregates/Order/PaymentMethod.cs`)
- [x] Create `OrderItem` value object **class** — `CatalogProductId (Guid)`, `Quantity (int)`, `UnitPrice (decimal)` (`Domain/Aggregates/Order/OrderItem.cs`)
- [x] Create `PaymentDetails` value object **class** — `Method (PaymentMethod)`, `CardNumber (string?)`, `HolderName (string?)`, `Expiry (string?)`, `Cvv (string?)`, `PixKey (string?)` (`Domain/Aggregates/Order/PaymentDetails.cs`)
- [x] Create `ShippingAddress` value object **class** — `Street`, `City`, `State`, `ZipCode`, `Country` (all string) (`Domain/Aggregates/Order/ShippingAddress.cs`)
- [x] Create `Order` aggregate inheriting `AggregateRoot<OrderId>` (`Domain/Aggregates/Order/Order.cs`)
  - Properties: `UserId (Guid)`, `TransactionId (Guid)`, `Status (OrderStatus)`, `ErrorMessage (string?)`, `Items (IReadOnlyList<OrderItem>)`, `Payment (PaymentDetails)`, `Address (ShippingAddress)`, `CreatedAt (DateTime)`, `UpdatedAt (DateTime?)`, `DeletedAt (DateTime?)` (CON-GOV-2)
  - `private Order() {}` — EF Core parameterless ctor
  - `static Order Create(Guid transactionId, Guid userId, IReadOnlyList<OrderItem> items, PaymentDetails payment, ShippingAddress address)` — sets Status=Processing, raises `OrderCreatedEvent`; throws `OrderItemsEmptyException` if items is empty
  - `MarkAsProcessed()` — sets Status=Processed, sets UpdatedAt=UtcNow (CON-WF-5), raises `OrderProcessedEvent`; throws `InvalidOrderStatusTransitionException` if Status != Processing (CON-WF-3)
  - `MarkAsError(string message)` — sets Status=Error, sets ErrorMessage, sets UpdatedAt=UtcNow (CON-WF-5), raises `OrderErrorEvent`; throws `InvalidOrderStatusTransitionException` if Status != Processing (CON-WF-3)
- [x] Create `OrderAuditLog` entity — `OrderId (Guid)`, `TransactionId (Guid)`, `FromState (string)`, `ToState (string)`, `OccurredAtUtc (DateTime)`, `TriggeredByEvent (string)` (`Domain/Aggregates/Order/OrderAuditLog.cs`) — CON-GOV-1
- [x] Create domain events: `OrderCreatedEvent(OrderId OrderId)`, `OrderProcessedEvent(OrderId OrderId)`, `OrderErrorEvent(OrderId OrderId, string ErrorMessage)` (`Domain/Events/`)
- [x] Create domain exceptions: `OrderNotFoundException`, `OrderItemsEmptyException`, `InvalidOrderStatusTransitionException` (`Domain/Exceptions/`)

## Application
- [x] Define `IOrderRepository` port — `AddAsync(Order, ct)`, `GetByIdAsync(OrderId, ct) → Order?`, `GetByTransactionIdAsync(Guid, ct) → Order?`, `UpdateAsync(Order, ct)` (`Application/Ports/IOrderRepository.cs`)
- [x] Define `IAuditLogRepository` port — `AppendAsync(OrderAuditLog, ct)` (append-only; no update or delete) (`Application/Ports/IAuditLogRepository.cs`) — CON-GOV-1, CON-GOV-6
- [x] Define `IPaymentGateway` port with `PaymentRequest(Guid OrderId, decimal TotalAmount, PaymentDetails Payment)` and `PaymentResult(bool Success, string? ErrorMessage)` (`Application/Ports/IPaymentGateway.cs`)
- [x] Create `CreateOrderCommand` + handler + validator (`Application/Commands/CreateOrder/`)
  - Command includes client-supplied `TransactionId (Guid)` as idempotency key (CON-IC-1)
  - Handler: if order with same TransactionId already exists, return its Id without re-creating (CON-IC-2)
  - Handler creates order, calls `repository.AddAsync`, publishes `OrderCreatedEvent` via `IPublishEndpoint`
  - Validator: all string fields — strip HTML tags, null bytes (`\0`), and control characters before passing to aggregate (CON-SEC-1); `TransactionId` NotEmpty; `UserId` NotEmpty; `Items` NotEmpty; each item: `CatalogProductId` NotEmpty, `Quantity ≥ 1`, `UnitPrice > 0`; `Payment.Method` is valid `PaymentMethod` enum; when CreditCard — `CardNumber` NotEmpty 13–19 chars, `HolderName` NotEmpty, `Expiry` matches `^(0[1-9]|1[0-2])\/\d{2}$`, `Cvv` matches `^\d{3,4}$`; when Pix — `PixKey` NotEmpty; all address fields NotEmpty
- [x] Create `ProcessOrderCommand` + handler (no validator — internal command) (`Application/Commands/ProcessOrder/`)
  - Handler: load order; if already `Processed` or `Error`, return as no-op (CON-WF-6)
  - Handler: compute `totalAmount`, call `IPaymentGateway.ProcessAsync`, call `MarkAsProcessed()` or `MarkAsError()`, then `UpdateAsync`
  - Handler: write `OrderAuditLog` entry via `IAuditLogRepository` on every state transition (CON-GOV-1)
- [x] Create `GetOrderStatusQuery(Guid TransactionId)` + handler returning `OrderPollingDto?` (`Application/Queries/GetOrderStatus/`)
  - Handler uses `GetByTransactionIdAsync`; returns null (→ 404) if not found
  - If Processing: map thin DTO (TransactionId + Status only, Order = null)
  - Otherwise: map full DTO including `OrderDto`
- [x] Create DTOs — no card numbers, CVV, or raw PII exposed (CON-SEC-2) (`Application/DTOs/`)
  - `OrderPollingDto` — `TransactionId (Guid)`, `Status (string)`, `ErrorMessage (string?)`, `Order (OrderDto?)`
  - `OrderDto` — `Id (Guid)`, `UserId (Guid)`, `Items (IReadOnlyList<OrderItemDto>)`, `PaymentMethod (string)`, `Address (ShippingAddressDto)`, `CreatedAt (DateTime)`, `UpdatedAt (DateTime?)`
  - `OrderItemDto` — `CatalogProductId (Guid)`, `Quantity (int)`, `UnitPrice (decimal)`
  - `ShippingAddressDto` — `City`, `State`, `ZipCode`, `Country` (Street omitted per CON-SEC-7 masking rules)
- [x] Create `OrderMappingProfile` — apply per-field masking rules (CON-SEC-7): omit Street from address, omit card data, map Status to string (`Application/Mappings/OrderMappingProfile.cs`)

## Infrastructure.Data
- [x] Create `OrderConfiguration` EF config (`Infrastructure.Data/Configurations/OrderConfiguration.cs`)
  - `entity.Ignore(e => e.DomainEvents)`
  - `entity.Property(e => e.Status).HasConversion<string>()`
  - `entity.OwnsMany(e => e.Items, b => { b.ToTable("OrderItems"); ... })`
  - `entity.OwnsOne(e => e.Payment, b => { b.Property(p => p.Method).HasConversion<string>(); ... })`
  - `entity.OwnsOne(e => e.Address, ...)`
  - Unique index on `TransactionId` column (CON-IC-3)
  - `entity.Property<byte[]>("RowVersion").IsRowVersion()` — optimistic concurrency guard (CON-IC-3)
  - `DeletedAt` nullable column (CON-GOV-2 soft-delete)
- [x] Create `AuditLogConfiguration` EF config — maps `OrderAuditLog` to `OrderAuditLogs` table; no soft-delete (CON-GOV-6) (`Infrastructure.Data/Configurations/AuditLogConfiguration.cs`)
- [x] Create `OrderRepository` implementing `IOrderRepository` (`Infrastructure.Data/Repositories/OrderRepository.cs`)
- [x] Create `AuditLogRepository` implementing `IAuditLogRepository` — `AppendAsync` only; never updates or deletes entries (`Infrastructure.Data/Repositories/AuditLogRepository.cs`) — CON-GOV-6
- [x] Add `DbSet<Order> Orders { get; set; }` and `DbSet<OrderAuditLog> OrderAuditLogs { get; set; }` to `AppDbContext`
- [ ] Run EF migration: `dotnet ef migrations add AddOrder --project src/Infrastructure.Data --startup-project src/Presentation`

## Infrastructure.Integration
- [x] Create `MockPaymentGateway` implementing `IPaymentGateway` — returns `PaymentResult(Success: true, ErrorMessage: null)` by default; no external dependency; drop-in replaceable without touching other layers (`Infrastructure.Integration/PaymentGateway/MockPaymentGateway.cs`)
- [x] Create `OrderCreatedConsumer : IConsumer<OrderCreatedEvent>` — inject `ISender` and `ILogger`; on Consume dispatch `ProcessOrderCommand(context.Message.OrderId.Value)` via MediatR; MassTransit's outbox or deduplication ensures CON-IC-4 idempotency (`Infrastructure.Integration/Messaging/Consumers/OrderCreatedConsumer.cs`)

## Presentation
- [x] Create `OrderEndpoints` static class with `MapOrderEndpoints(this IEndpointRouteBuilder app)` extension method (`Presentation/Endpoints/OrderEndpoints.cs`)
  - `POST /api/v1/orders` → HTTP 202 Accepted with `{ transactionId, status }` (CON-API-1, CON-API-2)
  - `GET /api/v1/orders/{transactionId:guid}` → HTTP 200 with `OrderPollingDto` or HTTP 404 (CON-API-3, CON-API-6)
- [x] Add global exception handler middleware in `Program.cs` — never expose stack traces or raw DB errors in responses; return only a correlation ID + generic message (CON-SEC-3)
- [x] Add security headers to Order endpoints: `X-Content-Type-Options: nosniff`, `Cache-Control: no-store, no-cache`, `Strict-Transport-Security: max-age=31536000; includeSubDomains` (CON-SEC-4)
- [x] Register DI in `Program.cs`:
  - `builder.Services.AddScoped<IOrderRepository, OrderRepository>()`
  - `builder.Services.AddScoped<IPaymentGateway, MockPaymentGateway>()`
  - `builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>()`
- [x] Register `OrderCreatedConsumer` in MassTransit configuration in `Program.cs`
- [x] Call `app.MapOrderEndpoints()` in `Program.cs`

## Verification
- [ ] `dotnet build` — zero errors
- [ ] `dotnet test` — no regressions
- [ ] `POST /api/v1/orders` returns HTTP 202 with `transactionId` and `status: "Processing"`
- [ ] `GET /api/v1/orders/{transactionId}` returns 200 while processing, 200 with full `OrderDto` once done, 404 for unknown UUID
- [ ] Repeated `POST` with same `TransactionId` returns existing order status without creating a duplicate (CON-IC-2)
- [ ] Domain exception raised when attempting to re-process a `Processed` or `Error` order — status unchanged (CON-WF-3, CON-WF-6)
- [ ] `POST /api/v1/orders` with `payment.method = "BankTransfer"` (or any unsupported value) returns HTTP 422 (BR-4)
- [ ] After processing, `GET /api/v1/orders/{transactionId}` returns each item's `unitPrice` unchanged from what was submitted at creation — no recalculation (BR-3)
- [ ] GET response contains no `cardNumber`, `cvv`, `holderName`, or `street` fields (CON-SEC-2, CON-SEC-7)
- [ ] Error response body contains only a correlation ID and generic message — no stack trace (CON-SEC-3)
- [ ] `OrderAuditLogs` table receives one append-only entry per state transition (CON-GOV-1, CON-GOV-6)
