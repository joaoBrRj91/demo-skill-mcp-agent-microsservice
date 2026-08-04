# Tests — CreateOrderDomainBusiness

> TDD: implement ALL items in this file before opening Tasks.md.
> Stage 1 → Setup | Stage 2 → Write test files (RED) | Stage 3 → Implement Tasks.md (GREEN) | Stage 4 → dotnet test

---

## Stage 1 — Setup

- [x] Create unit test project (`tests/UnitTests/JL.Commerce.Tecnology.Service.UnitTests.csproj`)
  - Target: `net10.0`; packages: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Moq`, `coverlet.collector`, `AutoMapper`
  - Project references: `Domain`, `Application`
- [x] Create integration test project (`tests/IntegrationTests/JL.Commerce.Tecnology.Service.IntegrationTests.csproj`)
  - Target: `net10.0`; same packages + `Microsoft.AspNetCore.Mvc.Testing`
  - Project reference: `Presentation`
- [x] Add both projects to the solution file (`JL.Commerce.Tecnology.Service.sln`)

---

## Stage 2 — Unit Tests: Domain Layer

- [x] [TR-1] `OrderId` value object (`tests/UnitTests/Domain/Aggregates/Order/OrderIdTests.cs`)
  - `New_Returns_Valid_NonEmpty_Guid` — `OrderId.New().Value` is not `Guid.Empty`
  - `Two_New_Calls_Produce_Unique_Ids` — two consecutive calls to `OrderId.New()` produce different `Value`s
  - `ToString_Returns_Guid_String` — `orderId.ToString()` equals `orderId.Value.ToString()`

- [x] [TR-2] `Order` aggregate (`tests/UnitTests/Domain/Aggregates/Order/OrderTests.cs`)
  - `Create_WithValidParameters_SetsStatus_Processing` — `Order.Create(...)` returns an Order with `Status == OrderStatus.Processing` // [CON-WF-1]
  - `Create_WithEmptyItems_Throws_OrderItemsEmptyException` — passing an empty list to `Order.Create` throws `OrderItemsEmptyException` // [BR-1, CON-DI-1]
  - `Create_Raises_OrderCreatedEvent` — `DomainEvents` contains exactly one `OrderCreatedEvent` after `Create` // [CON-WF-4]
  - `Create_CapturesUnitPrice_AtCreationTime` — `OrderItem.UnitPrice` on the persisted aggregate equals the value passed at creation, not a recalculated value // [BR-3, CON-DI-3]
  - `MarkAsProcessed_WhenProcessing_TransitionsTo_Processed` — calling `MarkAsProcessed()` on a `Processing` order sets `Status == Processed` // [CON-WF-2]
  - `MarkAsProcessed_WhenProcessing_Raises_OrderProcessedEvent` — `DomainEvents` contains `OrderProcessedEvent` after transition // [CON-WF-4]
  - `MarkAsProcessed_WhenAlreadyProcessed_Throws_InvalidOrderStatusTransitionException` — calling `MarkAsProcessed()` on a `Processed` order throws `InvalidOrderStatusTransitionException` // [BR-7, CON-WF-3]
  - `MarkAsProcessed_WhenError_Throws_InvalidOrderStatusTransitionException` — calling `MarkAsProcessed()` on an `Error` order throws `InvalidOrderStatusTransitionException` // [BR-7, CON-WF-3]
  - `MarkAsError_WhenProcessing_SetsStatus_Error_WithMessage` — `Status == Error` and `ErrorMessage` equals the argument passed to `MarkAsError` // [BR-8, CON-DI-8]
  - `MarkAsError_WhenProcessing_Raises_OrderErrorEvent` — `DomainEvents` contains `OrderErrorEvent` with matching `ErrorMessage` // [CON-WF-4]
  - `MarkAsError_WhenAlreadyProcessed_Throws_InvalidOrderStatusTransitionException` — calling `MarkAsError` on a `Processed` order throws // [BR-7, CON-WF-3]
  - `MarkAsError_WhenAlreadyError_Throws_InvalidOrderStatusTransitionException` — calling `MarkAsError` on an `Error` order throws // [BR-7, CON-WF-3, CON-DI-7]

---

## Stage 2 — Unit Tests: Application Layer

- [x] [TR-3] `CreateOrderCommandValidator` (`tests/UnitTests/Application/Commands/CreateOrder/CreateOrderCommandValidatorTests.cs`)
  - `Validate_ValidPixPayload_Passes` — a fully-populated Pix command passes validation
  - `Validate_ValidCreditCardPayload_Passes` — a fully-populated CreditCard command passes validation
  - `Validate_EmptyUserId_Fails` — `UserId == Guid.Empty` produces a validation error
  - `Validate_EmptyItems_Fails` — `Items = []` produces a validation error // [BR-1, CON-DI-1]
  - `Validate_ItemQuantityZero_Fails` — an item with `Quantity = 0` produces a validation error // [BR-2]
  - `Validate_ItemQuantityNegative_Fails` — an item with `Quantity = -1` produces a validation error // [BR-2]
  - `Validate_ItemUnitPriceZero_Fails` — an item with `UnitPrice = 0` produces a validation error // [CON-DI-6]
  - `Validate_CreditCard_MissingCardNumber_Fails` — `CardNumber` absent when method is CreditCard produces a validation error // [BR-5]
  - `Validate_CreditCard_MissingHolderName_Fails` — `HolderName` absent produces a validation error // [BR-5]
  - `Validate_CreditCard_InvalidExpiryFormat_Fails` — `Expiry = "13/99"` (invalid month) produces a validation error // [BR-5]
  - `Validate_CreditCard_InvalidCvv_Fails` — `Cvv = "12"` (too short) produces a validation error // [BR-5]
  - `Validate_Pix_MissingPixKey_Fails` — `PixKey` absent when method is Pix produces a validation error // [BR-6, CON-DI-5]
  - `Validate_UnsupportedPaymentMethod_Fails` — an unrecognised method string produces a validation error // [BR-4, CON-DI-4]
  - `Validate_MissingAddressStreet_Fails` — `Address.Street` empty produces a validation error
  - `Validate_MissingAddressCity_Fails` — `Address.City` empty produces a validation error
  - `Validate_MissingAddressZipCode_Fails` — `Address.ZipCode` empty produces a validation error

- [x] [TR-4] `CreateOrderCommandHandler` (`tests/UnitTests/Application/Commands/CreateOrder/CreateOrderCommandHandlerTests.cs`)
  - Constructor builds mocks for `IOrderRepository` and `IPublishEndpoint`
  - `Handle_ValidCommand_Calls_AddAsync_Once` — `repository.AddAsync` is called exactly once with an Order whose `Status == Processing` // [CON-WF-1]
  - `Handle_ValidCommand_Publishes_OrderCreatedEvent` — `publishEndpoint.Publish<OrderCreatedEvent>` is called exactly once with the correct `OrderId`
  - `Handle_ValidCommand_Returns_OrderId_As_Guid` — return value matches the persisted order's `Id.Value`
  - `Handle_ValidCommand_InitialStatus_Is_Processing` — the Order passed to `AddAsync` has `Status == OrderStatus.Processing` // [CON-WF-1]

- [x] [TR-5] `ProcessOrderCommandHandler` (`tests/UnitTests/Application/Commands/ProcessOrder/ProcessOrderCommandHandlerTests.cs`)
  - Constructor builds mocks for `IOrderRepository` and `IPaymentGateway`
  - `Handle_OrderNotFound_Throws_OrderNotFoundException` — when `GetByIdAsync` returns null, handler throws `OrderNotFoundException`
  - `Handle_PaymentSuccess_Calls_MarkAsProcessed_And_UpdateAsync` — when gateway returns `Success=true`, `UpdateAsync` is called with an order in `Processed` state // [CON-WF-2]
  - `Handle_PaymentFailure_Calls_MarkAsError_And_UpdateAsync` — when gateway returns `Success=false`, `UpdateAsync` is called with an order in `Error` state and matching `ErrorMessage` // [BR-8, CON-DI-8]
  - `Handle_GatewayTimeout_Sets_ErrorState` — when gateway returns `Success=false` with a timeout message, order reaches `Error` state // [CON-DI-8]
  - `Handle_AlreadyTerminalOrder_IsNoOp` — when order is already in `Processed` or `Error` state, handler neither calls the gateway nor calls `UpdateAsync` // [CON-WF-6]

- [x] [TR-6] `GetOrderStatusQueryHandler` (`tests/UnitTests/Application/Queries/GetOrderStatus/GetOrderStatusQueryHandlerTests.cs`)
  - Constructor builds mock for `IOrderRepository`
  - `Handle_OrderNotFound_Returns_Null` — when `GetByIdAsync` returns null, handler returns null // [CON-API-3]
  - `Handle_ProcessingOrder_Returns_ThinDto_OrderFieldIsNull` — when order `Status == Processing`, returned `OrderPollingDto.Order` is null // [Spec scenario 4]
  - `Handle_ProcessedOrder_Returns_FullDto_WithOrderDetails` — when `Status == Processed`, `OrderPollingDto.Order` is populated with items and address // [Spec scenario 5]
  - `Handle_ErrorOrder_Returns_FullDto_WithErrorMessage` — when `Status == Error`, `OrderPollingDto.ErrorMessage` matches `order.ErrorMessage` // [Spec scenario 6]

- [x] [TR-7] `OrderMappingProfile` (`tests/UnitTests/Application/Mappings/OrderMappingProfileTests.cs`)
  - Uses `MapperConfiguration.AssertConfigurationIsValid()` to verify all maps are complete
  - `Map_ProcessingOrder_To_PollingDto_Order_IsNull` — a `Processing` order maps to `OrderPollingDto` with `Order == null`
  - `Map_ProcessedOrder_To_PollingDto_Order_IsPopulated` — a `Processed` order maps to a non-null `OrderDto`
  - `Map_OrderItem_To_OrderItemDto_UnitPrice_IsPreserved` — `OrderItemDto.UnitPrice` equals the source `UnitPrice` unchanged // [BR-3, CON-DI-3]
  - `Map_Order_PaymentMethod_IsString` — `OrderDto.PaymentMethod` equals the enum name as a string (e.g., `"CreditCard"`)

---

## Stage 2 — Integration Tests

- [x] [TR-8] `OrderEndpoints` integration tests (`tests/IntegrationTests/Endpoints/OrderEndpointsTests.cs`)
  - Uses `WebApplicationFactory<Program>`; configure in-memory or SQLite store and disable real MassTransit transport
  - `PostOrder_ValidPixPayload_Returns_202_Accepted` — POST with valid Pix payload returns HTTP 202 // [CON-API-1, Spec scenario 1]
  - `PostOrder_ValidCreditCardPayload_Returns_202_Accepted` — POST with valid CreditCard payload returns HTTP 202 // [CON-API-1, Spec scenario 2]
  - `PostOrder_Returns_TransactionId_And_Processing_Status` — 202 body contains `transactionId` (non-empty UUID) and `status == "Processing"` // [CON-API-2]
  - `PostOrder_EmptyItems_Returns_422` — POST with `items=[]` returns HTTP 422 // [CON-DI-1, Spec scenario 8]
  - `PostOrder_MissingCreditCardNumber_Returns_422` — POST with method=CreditCard and no cardNumber returns HTTP 422 // [Spec scenario 9]
  - `PostOrder_MissingPixKey_Returns_422` — POST with method=Pix and no pixKey returns HTTP 422 // [Spec scenario 10]
  - `PostOrder_ItemQuantityBelowMinimum_Returns_422` — POST with `quantity=0` returns HTTP 422 // [Spec scenario 11]
  - `PostOrder_UnsupportedPaymentMethod_Returns_422` — POST with `method="BankTransfer"` returns HTTP 422 // [CON-DI-4, Spec scenario 14]
  - `GetOrderStatus_UnknownTransactionId_Returns_404` — GET with a UUID that has no matching order returns HTTP 404 // [CON-API-3, Spec scenario 7]
  - `GetOrderStatus_InvalidUuid_Returns_400` — GET with a non-UUID path segment returns HTTP 400 // [CON-API-6]
  - `GetOrderStatus_ProcessingOrder_Returns_200_With_Processing_Status` — GET on a `Processing` order returns HTTP 200 with `status == "Processing"` and no order details // [Spec scenario 4]
  - `PostOrder_ErrorResponse_DoesNotExposeStackTrace` — a 422 response body contains no stack trace or internal exception details // [CON-SEC-3]

---

## Stage 4 — Verification

- [x] `dotnet test` — zero failures, all 58 test cases pass
