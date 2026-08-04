using JL.Commerce.Tecnology.Service.Application.Commands.CreateOrder;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

namespace JL.Commerce.Tecnology.Service.UnitTests.Application.Commands.CreateOrder;

public sealed class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    private static CreateOrderCommand ValidPixCommand() => new(
        TransactionId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        Items: [new OrderItemInput(Guid.NewGuid(), 1, 99.99m)],
        Payment: new PaymentDetailsInput(PaymentMethod.Pix, null, null, null, null, "pix@test.com"),
        Address: new ShippingAddressInput("Rua A", "Rio", "RJ", "20000-000", "BR"));

    private static CreateOrderCommand ValidCreditCardCommand() => new(
        TransactionId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        Items: [new OrderItemInput(Guid.NewGuid(), 2, 49.99m)],
        Payment: new PaymentDetailsInput(PaymentMethod.CreditCard, "4111111111111111", "John Doe", "12/26", "123", null),
        Address: new ShippingAddressInput("Rua B", "SP", "SP", "01001-000", "BR"));

    [Fact]
    public void Validate_ValidPixPayload_Passes()
    {
        // Arrange
        var command = ValidPixCommand();

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidCreditCardPayload_Passes()
    {
        // Arrange
        var command = ValidCreditCardCommand();

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyUserId_Fails()
    {
        // Arrange
        var command = ValidPixCommand() with { UserId = Guid.Empty };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("UserId"));
    }

    [Fact]
    public void Validate_EmptyItems_Fails()
    {
        // Arrange
        var command = ValidPixCommand() with { Items = [] };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid); // [BR-1, CON-DI-1]
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Items"));
    }

    [Fact]
    public void Validate_ItemQuantityZero_Fails()
    {
        // Arrange
        var command = ValidPixCommand() with
        {
            Items = [new OrderItemInput(Guid.NewGuid(), 0, 10m)]
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid); // [BR-2]
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Quantity"));
    }

    [Fact]
    public void Validate_ItemQuantityNegative_Fails()
    {
        // Arrange
        var command = ValidPixCommand() with
        {
            Items = [new OrderItemInput(Guid.NewGuid(), -1, 10m)]
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid); // [BR-2]
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Quantity"));
    }

    [Fact]
    public void Validate_ItemUnitPriceZero_Fails()
    {
        // Arrange
        var command = ValidPixCommand() with
        {
            Items = [new OrderItemInput(Guid.NewGuid(), 1, 0m)]
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid); // [CON-DI-6]
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("UnitPrice"));
    }

    [Fact]
    public void Validate_CreditCard_MissingCardNumber_Fails()
    {
        // Arrange
        var command = ValidCreditCardCommand() with
        {
            Payment = new PaymentDetailsInput(PaymentMethod.CreditCard, null, "John Doe", "12/26", "123", null)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid); // [BR-5]
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("CardNumber"));
    }

    [Fact]
    public void Validate_CreditCard_MissingHolderName_Fails()
    {
        // Arrange
        var command = ValidCreditCardCommand() with
        {
            Payment = new PaymentDetailsInput(PaymentMethod.CreditCard, "4111111111111111", null, "12/26", "123", null)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid); // [BR-5]
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("HolderName"));
    }

    [Fact]
    public void Validate_CreditCard_InvalidExpiryFormat_Fails()
    {
        // Arrange
        var command = ValidCreditCardCommand() with
        {
            Payment = new PaymentDetailsInput(PaymentMethod.CreditCard, "4111111111111111", "John", "13/99", "123", null)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid); // [BR-5]
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Expiry"));
    }

    [Fact]
    public void Validate_CreditCard_InvalidCvv_Fails()
    {
        // Arrange
        var command = ValidCreditCardCommand() with
        {
            Payment = new PaymentDetailsInput(PaymentMethod.CreditCard, "4111111111111111", "John", "12/26", "12", null)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid); // [BR-5]
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Cvv"));
    }

    [Fact]
    public void Validate_Pix_MissingPixKey_Fails()
    {
        // Arrange
        var command = ValidPixCommand() with
        {
            Payment = new PaymentDetailsInput(PaymentMethod.Pix, null, null, null, null, null)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid); // [BR-6, CON-DI-5]
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("PixKey"));
    }

    [Fact]
    public void Validate_UnsupportedPaymentMethod_Fails()
    {
        // Arrange — cast an out-of-range int to PaymentMethod to simulate unsupported value
        var command = ValidPixCommand() with
        {
            Payment = new PaymentDetailsInput((PaymentMethod)99, null, null, null, null, null)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid); // [BR-4, CON-DI-4]
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Method"));
    }

    [Fact]
    public void Validate_MissingAddressStreet_Fails()
    {
        // Arrange
        var command = ValidPixCommand() with
        {
            Address = new ShippingAddressInput(string.Empty, "Rio", "RJ", "20000-000", "BR")
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Street"));
    }

    [Fact]
    public void Validate_MissingAddressCity_Fails()
    {
        // Arrange
        var command = ValidPixCommand() with
        {
            Address = new ShippingAddressInput("Rua A", string.Empty, "RJ", "20000-000", "BR")
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("City"));
    }

    [Fact]
    public void Validate_MissingAddressZipCode_Fails()
    {
        // Arrange
        var command = ValidPixCommand() with
        {
            Address = new ShippingAddressInput("Rua A", "Rio", "RJ", string.Empty, "BR")
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("ZipCode"));
    }
}
