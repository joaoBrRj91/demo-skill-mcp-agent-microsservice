using JL.Commerce.Tecnology.Service.Domain.Abstractions;

namespace JL.Commerce.Tecnology.Service.Application.Ports;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : IDomainEvent;
}
