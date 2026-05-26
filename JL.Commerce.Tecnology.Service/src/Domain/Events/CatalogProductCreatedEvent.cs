using JL.Commerce.Tecnology.Service.Domain.Abstractions;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.CatalogProduct;

namespace JL.Commerce.Tecnology.Service.Domain.Events;

public sealed record CatalogProductCreatedEvent(CatalogProductId ProductId) : IDomainEvent;
