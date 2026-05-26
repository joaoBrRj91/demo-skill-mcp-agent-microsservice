namespace JL.Commerce.Tecnology.Service.Domain.Exceptions;

public sealed class EntityNotFoundException(Guid id)
    : Exception($"Entity with id '{id}' was not found.");
