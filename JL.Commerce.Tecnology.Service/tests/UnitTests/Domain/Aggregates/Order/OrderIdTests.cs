using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

namespace JL.Commerce.Tecnology.Service.UnitTests.Domain.Aggregates;

public sealed class OrderIdTests
{
    [Fact]
    public void New_Returns_Valid_NonEmpty_Guid()
    {
        // Arrange
        // (none)

        // Act
        var orderId = OrderId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, orderId.Value);
    }

    [Fact]
    public void Two_New_Calls_Produce_Unique_Ids()
    {
        // Arrange
        // (none)

        // Act
        var id1 = OrderId.New();
        var id2 = OrderId.New();

        // Assert
        Assert.NotEqual(id1.Value, id2.Value);
    }

    [Fact]
    public void ToString_Returns_Guid_String()
    {
        // Arrange
        var orderId = OrderId.New();

        // Act
        var result = orderId.ToString();

        // Assert
        Assert.Equal(orderId.Value.ToString(), result);
    }
}
