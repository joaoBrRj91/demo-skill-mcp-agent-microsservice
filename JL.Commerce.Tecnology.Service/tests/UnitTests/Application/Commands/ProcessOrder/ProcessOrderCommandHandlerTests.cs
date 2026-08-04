using JL.Commerce.Tecnology.Service.Application.Commands.ProcessOrder;
using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using JL.Commerce.Tecnology.Service.Domain.Exceptions;
using Moq;

namespace JL.Commerce.Tecnology.Service.UnitTests.Application.Commands.ProcessOrder;

public sealed class ProcessOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();
    private readonly Mock<IPaymentGateway> _paymentGatewayMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogMock = new();
    private readonly ProcessOrderCommandHandler _handler;

    public ProcessOrderCommandHandlerTests()
    {
        _handler = new ProcessOrderCommandHandler(
            _repositoryMock.Object,
            _paymentGatewayMock.Object,
            _auditLogMock.Object);
    }

    private static Order CreateProcessingOrder()
    {
        var items = new List<OrderItem> { new(Guid.NewGuid(), 1, 50m) };
        var payment = new PaymentDetails(PaymentMethod.Pix, null, null, null, null, "pix@test.com");
        var address = new ShippingAddress("Rua A", "Rio", "RJ", "20000-000", "BR");
        return Order.Create(Guid.NewGuid(), Guid.NewGuid(), items, payment, address);
    }

    [Fact]
    public async Task Handle_OrderNotFound_Throws_OrderNotFoundException()
    {
        // Arrange
        var command = new ProcessOrderCommand(Guid.NewGuid());
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<OrderId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<OrderNotFoundException>(act);
    }

    [Fact]
    public async Task Handle_PaymentSuccess_Calls_MarkAsProcessed_And_UpdateAsync()
    {
        // Arrange
        var order = CreateProcessingOrder();
        var command = new ProcessOrderCommand(order.Id.Value);
        _repositoryMock.Setup(r => r.GetByIdAsync(new OrderId(command.OrderId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _paymentGatewayMock.Setup(g => g.ProcessAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(true, null));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(OrderStatus.Processed, order.Status); // [CON-WF-2]
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PaymentFailure_Calls_MarkAsError_And_UpdateAsync()
    {
        // Arrange
        var order = CreateProcessingOrder();
        var command = new ProcessOrderCommand(order.Id.Value);
        var errorMessage = "Payment declined";
        _repositoryMock.Setup(r => r.GetByIdAsync(new OrderId(command.OrderId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _paymentGatewayMock.Setup(g => g.ProcessAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(false, errorMessage));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(OrderStatus.Error, order.Status); // [BR-8, CON-DI-8]
        Assert.Equal(errorMessage, order.ErrorMessage);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GatewayTimeout_Sets_ErrorState()
    {
        // Arrange
        var order = CreateProcessingOrder();
        var command = new ProcessOrderCommand(order.Id.Value);
        _repositoryMock.Setup(r => r.GetByIdAsync(new OrderId(command.OrderId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _paymentGatewayMock.Setup(g => g.ProcessAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(false, "Gateway timeout after 30s"));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(OrderStatus.Error, order.Status); // [CON-DI-8]
    }

    [Fact]
    public async Task Handle_AlreadyTerminalOrder_IsNoOp()
    {
        // Arrange
        var order = CreateProcessingOrder();
        order.MarkAsProcessed();
        var command = new ProcessOrderCommand(order.Id.Value);
        _repositoryMock.Setup(r => r.GetByIdAsync(new OrderId(command.OrderId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _paymentGatewayMock.Verify(
            g => g.ProcessAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()),
            Times.Never); // [CON-WF-6]
        _repositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
