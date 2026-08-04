using JL.Commerce.Tecnology.Service.Application.Commands.CreateOrder;
using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using JL.Commerce.Tecnology.Service.Domain.Events;
using Moq;

namespace JL.Commerce.Tecnology.Service.UnitTests.Application.Commands.CreateOrder;

public sealed class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _handler = new CreateOrderCommandHandler(_repositoryMock.Object, _eventBusMock.Object);
    }

    private static CreateOrderCommand ValidPixCommand(Guid? transactionId = null) => new(
        TransactionId: transactionId ?? Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        Items: [new OrderItemInput(Guid.NewGuid(), 1, 50m)],
        Payment: new PaymentDetailsInput(PaymentMethod.Pix, null, null, null, null, "pix@test.com"),
        Address: new ShippingAddressInput("Rua A", "Rio", "RJ", "20000-000", "BR"));

    [Fact]
    public async Task Handle_ValidCommand_Calls_AddAsync_Once()
    {
        // Arrange
        var command = ValidPixCommand();
        _repositoryMock.Setup(r => r.GetByTransactionIdAsync(command.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            r => r.AddAsync(It.Is<Order>(o => o.Status == OrderStatus.Processing), It.IsAny<CancellationToken>()),
            Times.Once); // [CON-WF-1]
    }

    [Fact]
    public async Task Handle_ValidCommand_Publishes_OrderCreatedEvent()
    {
        // Arrange
        var command = ValidPixCommand();
        _repositoryMock.Setup(r => r.GetByTransactionIdAsync(command.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _eventBusMock.Verify(
            e => e.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_Returns_OrderId_As_Guid()
    {
        // Arrange
        var command = ValidPixCommand();
        Order? capturedOrder = null;
        _repositoryMock.Setup(r => r.GetByTransactionIdAsync(command.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((o, _) => capturedOrder = o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        Assert.Equal(capturedOrder!.Id.Value, result);
    }

    [Fact]
    public async Task Handle_ValidCommand_InitialStatus_Is_Processing()
    {
        // Arrange
        var command = ValidPixCommand();
        Order? capturedOrder = null;
        _repositoryMock.Setup(r => r.GetByTransactionIdAsync(command.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((o, _) => capturedOrder = o);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(OrderStatus.Processing, capturedOrder!.Status); // [CON-WF-1]
    }
}
