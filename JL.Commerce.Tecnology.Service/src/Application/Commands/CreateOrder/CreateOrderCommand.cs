using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.CreateOrder;

public sealed record CreateOrderCommand(
    Guid TransactionId,
    Guid UserId,
    IReadOnlyList<OrderItemInput> Items,
    PaymentDetailsInput Payment,
    ShippingAddressInput Address) : IRequest<Guid>;

public sealed record OrderItemInput(Guid CatalogProductId, int Quantity, decimal UnitPrice);

public sealed record PaymentDetailsInput(
    PaymentMethod Method,
    string? CardNumber,
    string? HolderName,
    string? Expiry,
    string? Cvv,
    string? PixKey);

public sealed record ShippingAddressInput(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country);
