using JL.Commerce.Tecnology.Service.Domain.Abstractions;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.User;

namespace JL.Commerce.Tecnology.Service.Domain.Events;

public sealed record UserCreatedEvent(UserId UserId) : IDomainEvent;
