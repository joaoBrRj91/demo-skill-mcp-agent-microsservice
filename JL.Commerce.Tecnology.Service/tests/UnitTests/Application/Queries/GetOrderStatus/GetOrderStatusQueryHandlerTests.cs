using AutoMapper;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Application.Mappings;
using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Application.Queries.GetOrderStatus;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace JL.Commerce.Tecnology.Service.UnitTests.Application.Queries.GetOrderStatus;

public sealed class GetOrderStatusQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();
    private readonly IMapper _mapper;
    private readonly GetOrderStatusQueryHandler _handler;

    public GetOrderStatusQueryHandlerTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(OrderMappingProfile).Assembly));
        _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
        _handler = new GetOrderStatusQueryHandler(_repositoryMock.Object, _mapper);
    }

    private static Order CreateProcessingOrder()
    {
        var items = new List<OrderItem> { new(Guid.NewGuid(), 1, 50m) };
        var payment = new PaymentDetails(PaymentMethod.Pix, null, null, null, null, "pix@test.com");
        var address = new ShippingAddress("Rua A", "Rio", "RJ", "20000-000", "BR");
        return Order.Create(Guid.NewGuid(), Guid.NewGuid(), items, payment, address);
    }

    [Fact]
    public async Task Handle_OrderNotFound_Returns_Null()
    {
        // Arrange
        var query = new GetOrderStatusQuery(Guid.NewGuid());
        _repositoryMock.Setup(r => r.GetByTransactionIdAsync(query.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result); // [CON-API-3]
    }

    [Fact]
    public async Task Handle_ProcessingOrder_Returns_ThinDto_OrderFieldIsNull()
    {
        // Arrange
        var order = CreateProcessingOrder();
        var query = new GetOrderStatusQuery(order.TransactionId);
        _repositoryMock.Setup(r => r.GetByTransactionIdAsync(query.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Processing", result.Status);
        Assert.Null(result.Order); // [Spec scenario 4]
    }

    [Fact]
    public async Task Handle_ProcessedOrder_Returns_FullDto_WithOrderDetails()
    {
        // Arrange
        var order = CreateProcessingOrder();
        order.MarkAsProcessed();
        var query = new GetOrderStatusQuery(order.TransactionId);
        _repositoryMock.Setup(r => r.GetByTransactionIdAsync(query.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Processed", result.Status);
        Assert.NotNull(result.Order); // [Spec scenario 5]
        Assert.NotEmpty(result.Order.Items);
    }

    [Fact]
    public async Task Handle_ErrorOrder_Returns_FullDto_WithErrorMessage()
    {
        // Arrange
        var order = CreateProcessingOrder();
        var errorMessage = "Payment failed";
        order.MarkAsError(errorMessage);
        var query = new GetOrderStatusQuery(order.TransactionId);
        _repositoryMock.Setup(r => r.GetByTransactionIdAsync(query.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Error", result.Status);
        Assert.Equal(errorMessage, result.ErrorMessage); // [Spec scenario 6]
    }
}
