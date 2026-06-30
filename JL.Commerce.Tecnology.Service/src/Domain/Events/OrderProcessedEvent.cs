using JL.Commerce.Tecnology.Service.Domain.Abstractions;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

namespace JL.Commerce.Tecnology.Service.Domain.Events;

public sealed record OrderProcessedEvent(OrderId OrderId) : IDomainEvent;
