using JL.Commerce.Tecnology.Service.Domain.Abstractions;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Entity;

namespace JL.Commerce.Tecnology.Service.Domain.Events;

public sealed record EntityCreatedEvent(EntityId EntityId) : IDomainEvent;
