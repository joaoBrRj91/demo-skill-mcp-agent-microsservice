namespace JL.Commerce.Tecnology.Service.Domain.Exceptions;

public sealed class CatalogProductNotFoundException(Guid id)
    : Exception($"CatalogProduct with id '{id}' was not found.");
