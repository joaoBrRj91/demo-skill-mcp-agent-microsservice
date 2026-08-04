using AutoMapper;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Application.Mappings;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using Microsoft.Extensions.DependencyInjection;

namespace JL.Commerce.Tecnology.Service.UnitTests.Application.Mappings;

public sealed class OrderMappingProfileTests
{
    private readonly IMapper _mapper;

    public OrderMappingProfileTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(OrderMappingProfile).Assembly));
        _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public void AutoMapper_Configuration_Is_Valid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(OrderMappingProfile).Assembly));
        var provider = services.BuildServiceProvider();

        // Act / Assert — throws if invalid
        provider.GetRequiredService<IMapper>();
    }

    [Fact]
    public void Map_ProcessingOrder_To_PollingDto_Order_IsNull()
    {
        // Arrange
        var items = new List<OrderItem> { new(Guid.NewGuid(), 1, 50m) };
        var payment = new PaymentDetails(PaymentMethod.Pix, null, null, null, null, "pix@test.com");
        var address = new ShippingAddress("Rua A", "Rio", "RJ", "20000-000", "BR");
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), items, payment, address);

        // Act
        var dto = _mapper.Map<OrderPollingDto>(order);

        // Assert
        Assert.Equal("Processing", dto.Status);
        Assert.Null(dto.Order);
    }

    [Fact]
    public void Map_ProcessedOrder_To_PollingDto_Order_IsPopulated()
    {
        // Arrange
        var items = new List<OrderItem> { new(Guid.NewGuid(), 2, 100m) };
        var payment = new PaymentDetails(PaymentMethod.CreditCard, "4111111111111111", "Jane", "12/26", "123", null);
        var address = new ShippingAddress("Rua B", "SP", "SP", "01001-000", "BR");
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), items, payment, address);
        order.MarkAsProcessed();

        // Act
        var dto = _mapper.Map<OrderPollingDto>(order);

        // Assert
        Assert.Equal("Processed", dto.Status);
        Assert.NotNull(dto.Order);
    }

    [Fact]
    public void Map_OrderItem_To_OrderItemDto_UnitPrice_IsPreserved()
    {
        // Arrange
        var expectedPrice = 123.45m;
        var items = new List<OrderItem> { new(Guid.NewGuid(), 3, expectedPrice) };
        var payment = new PaymentDetails(PaymentMethod.Pix, null, null, null, null, "pix@test.com");
        var address = new ShippingAddress("Rua C", "BH", "MG", "30000-000", "BR");
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), items, payment, address);
        order.MarkAsProcessed();

        // Act
        var dto = _mapper.Map<OrderPollingDto>(order);

        // Assert
        Assert.Equal(expectedPrice, dto.Order!.Items[0].UnitPrice); // [BR-3, CON-DI-3]
    }

    [Fact]
    public void Map_Order_PaymentMethod_IsString()
    {
        // Arrange
        var items = new List<OrderItem> { new(Guid.NewGuid(), 1, 10m) };
        var payment = new PaymentDetails(PaymentMethod.CreditCard, "4111111111111111", "Alice", "01/27", "321", null);
        var address = new ShippingAddress("Rua D", "Curitiba", "PR", "80000-000", "BR");
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), items, payment, address);
        order.MarkAsProcessed();

        // Act
        var dto = _mapper.Map<OrderPollingDto>(order);

        // Assert
        Assert.Equal("CreditCard", dto.Order!.PaymentMethod);
    }
}
