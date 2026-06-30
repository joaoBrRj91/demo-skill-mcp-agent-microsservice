namespace JL.Commerce.Tecnology.Service.Application.DTOs;

public record OrderPollingDto(
    Guid TransactionId,
    string Status,
    string? ErrorMessage,
    OrderDto? Order);

public record OrderDto(
    Guid Id,
    Guid UserId,
    IReadOnlyList<OrderItemDto> Items,
    string PaymentMethod,
    ShippingAddressDto Address,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record OrderItemDto(
    Guid CatalogProductId,
    int Quantity,
    decimal UnitPrice);

public record ShippingAddressDto(
    string City,
    string State,
    string ZipCode,
    string Country);
