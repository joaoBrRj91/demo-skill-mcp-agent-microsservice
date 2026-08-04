using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using JL.Commerce.Tecnology.Service.Domain.Events;
using JL.Commerce.Tecnology.Service.Domain.Exceptions;

namespace JL.Commerce.Tecnology.Service.UnitTests.Domain.Aggregates;

public sealed class OrderTests
{
    private static IReadOnlyList<OrderItem> ValidItems() =>
    [
        new OrderItem(Guid.NewGuid(), 2, 49.99m)
    ];

    private static PaymentDetails PixPayment() =>
        new(PaymentMethod.Pix, null, null, null, null, "pix@example.com");

    private static ShippingAddress ValidAddress() =>
        new("Rua A", "Rio de Janeiro", "RJ", "20000-000", "BR");

    private static Order CreateValidOrder() =>
        Order.Create(Guid.NewGuid(), Guid.NewGuid(), ValidItems(), PixPayment(), ValidAddress());

    [Fact]
    public void Create_WithValidParameters_SetsStatus_Processing()
    {
        // Arrange
        // (none)

        // Act
        var order = CreateValidOrder();

        // Assert
        Assert.Equal(OrderStatus.Processing, order.Status); // [CON-WF-1]
    }

    [Fact]
    public void Create_WithEmptyItems_Throws_OrderItemsEmptyException()
    {
        // Arrange
        var emptyItems = new List<OrderItem>();

        // Act
        var act = () => Order.Create(Guid.NewGuid(), Guid.NewGuid(), emptyItems, PixPayment(), ValidAddress());

        // Assert
        Assert.Throws<OrderItemsEmptyException>(act); // [BR-1, CON-DI-1]
    }

    [Fact]
    public void Create_Raises_OrderCreatedEvent()
    {
        // Arrange
        // (none)

        // Act
        var order = CreateValidOrder();

        // Assert
        var events = order.DomainEvents;
        Assert.Single(events);
        Assert.IsType<OrderCreatedEvent>(events.Single()); // [CON-WF-4]
    }

    [Fact]
    public void Create_CapturesUnitPrice_AtCreationTime()
    {
        // Arrange
        var expectedPrice = 123.45m;
        var items = new List<OrderItem> { new OrderItem(Guid.NewGuid(), 1, expectedPrice) };

        // Act
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), items, PixPayment(), ValidAddress());

        // Assert
        Assert.Equal(expectedPrice, order.Items[0].UnitPrice); // [BR-3, CON-DI-3]
    }

    [Fact]
    public void MarkAsProcessed_WhenProcessing_TransitionsTo_Processed()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        order.MarkAsProcessed();

        // Assert
        Assert.Equal(OrderStatus.Processed, order.Status); // [CON-WF-2]
    }

    [Fact]
    public void MarkAsProcessed_WhenProcessing_Raises_OrderProcessedEvent()
    {
        // Arrange
        var order = CreateValidOrder();
        order.ClearDomainEvents();

        // Act
        order.MarkAsProcessed();

        // Assert
        Assert.Contains(order.DomainEvents, e => e is OrderProcessedEvent); // [CON-WF-4]
    }

    [Fact]
    public void MarkAsProcessed_WhenAlreadyProcessed_Throws_InvalidOrderStatusTransitionException()
    {
        // Arrange
        var order = CreateValidOrder();
        order.MarkAsProcessed();

        // Act
        var act = () => order.MarkAsProcessed();

        // Assert
        Assert.Throws<InvalidOrderStatusTransitionException>(act); // [BR-7, CON-WF-3]
    }

    [Fact]
    public void MarkAsProcessed_WhenError_Throws_InvalidOrderStatusTransitionException()
    {
        // Arrange
        var order = CreateValidOrder();
        order.MarkAsError("gateway timeout");

        // Act
        var act = () => order.MarkAsProcessed();

        // Assert
        Assert.Throws<InvalidOrderStatusTransitionException>(act); // [BR-7, CON-WF-3]
    }

    [Fact]
    public void MarkAsError_WhenProcessing_SetsStatus_Error_WithMessage()
    {
        // Arrange
        var order = CreateValidOrder();
        var errorMessage = "Payment declined";

        // Act
        order.MarkAsError(errorMessage);

        // Assert
        Assert.Equal(OrderStatus.Error, order.Status); // [BR-8, CON-DI-8]
        Assert.Equal(errorMessage, order.ErrorMessage);
    }

    [Fact]
    public void MarkAsError_WhenProcessing_Raises_OrderErrorEvent()
    {
        // Arrange
        var order = CreateValidOrder();
        order.ClearDomainEvents();
        var errorMessage = "Gateway timeout";

        // Act
        order.MarkAsError(errorMessage);

        // Assert
        var errorEvent = order.DomainEvents.OfType<OrderErrorEvent>().Single();
        Assert.Equal(errorMessage, errorEvent.ErrorMessage); // [CON-WF-4]
    }

    [Fact]
    public void MarkAsError_WhenAlreadyProcessed_Throws_InvalidOrderStatusTransitionException()
    {
        // Arrange
        var order = CreateValidOrder();
        order.MarkAsProcessed();

        // Act
        var act = () => order.MarkAsError("some error");

        // Assert
        Assert.Throws<InvalidOrderStatusTransitionException>(act); // [BR-7, CON-WF-3]
    }

    [Fact]
    public void MarkAsError_WhenAlreadyError_Throws_InvalidOrderStatusTransitionException()
    {
        // Arrange
        var order = CreateValidOrder();
        order.MarkAsError("first error");

        // Act
        var act = () => order.MarkAsError("second error");

        // Assert
        Assert.Throws<InvalidOrderStatusTransitionException>(act); // [BR-7, CON-WF-3, CON-DI-7]
    }
}
