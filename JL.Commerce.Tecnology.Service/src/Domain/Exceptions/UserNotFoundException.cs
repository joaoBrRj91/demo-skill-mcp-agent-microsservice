namespace JL.Commerce.Tecnology.Service.Domain.Exceptions;

public sealed class UserNotFoundException(Guid id)
    : Exception($"User with id '{id}' was not found.");
