using JL.Commerce.Tecnology.Service.Domain.Abstractions;
using JL.Commerce.Tecnology.Service.Domain.Events;
using JL.Commerce.Tecnology.Service.Domain.Exceptions;

namespace JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

public sealed class Order : AggregateRoot<OrderId>
{
    public Guid UserId { get; private set; }
    public Guid TransactionId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public PaymentDetails Payment { get; private set; } = default!;
    public ShippingAddress Address { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Order() { }

    public static Order Create(
        Guid transactionId,
        Guid userId,
        IReadOnlyList<OrderItem> items,
        PaymentDetails payment,
        ShippingAddress address)
    {
        if (items == null || items.Count == 0)
            throw new OrderItemsEmptyException();

        var order = new Order
        {
            Id = OrderId.New(),
            TransactionId = transactionId,
            UserId = userId,
            Status = OrderStatus.Processing,
            Payment = payment,
            Address = address,
            CreatedAt = DateTime.UtcNow
        };

        order._items.AddRange(items);
        order.AddDomainEvent(new OrderCreatedEvent(order.Id));
        return order;
    }

    public void MarkAsProcessed()
    {
        if (Status != OrderStatus.Processing)
            throw new InvalidOrderStatusTransitionException(Status, OrderStatus.Processed);

        Status = OrderStatus.Processed;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderProcessedEvent(Id));
    }

    public void MarkAsError(string message)
    {
        if (Status != OrderStatus.Processing)
            throw new InvalidOrderStatusTransitionException(Status, OrderStatus.Error);

        Status = OrderStatus.Error;
        ErrorMessage = message;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderErrorEvent(Id, message));
    }
}
