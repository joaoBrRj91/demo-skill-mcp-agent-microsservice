namespace JL.Commerce.Tecnology.Service.Domain.Exceptions;

public sealed class OrderNotFoundException(Guid id)
    : Exception($"Order with id '{id}' was not found.");
