using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

namespace JL.Commerce.Tecnology.Service.Domain.Exceptions;

public sealed class InvalidOrderStatusTransitionException(OrderStatus from, OrderStatus to)
    : Exception($"Cannot transition order from '{from}' to '{to}'.");
