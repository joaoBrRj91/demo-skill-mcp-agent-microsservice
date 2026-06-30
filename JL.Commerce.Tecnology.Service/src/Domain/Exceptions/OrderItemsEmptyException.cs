namespace JL.Commerce.Tecnology.Service.Domain.Exceptions;

public sealed class OrderItemsEmptyException()
    : Exception("An order must contain at least one item.");
