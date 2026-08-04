using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Abstractions;
using MassTransit;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Integration.Messaging.Publishers;

public sealed class MassTransitEventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : IDomainEvent
        => publishEndpoint.Publish(domainEvent, ct);
}
