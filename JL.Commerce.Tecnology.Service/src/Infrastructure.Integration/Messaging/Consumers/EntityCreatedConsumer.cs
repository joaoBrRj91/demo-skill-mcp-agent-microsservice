using JL.Commerce.Tecnology.Service.Domain.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Integration.Messaging.Consumers;

public sealed class EntityCreatedConsumer(ILogger<EntityCreatedConsumer> logger)
    : IConsumer<EntityCreatedEvent>
{
    public Task Consume(ConsumeContext<EntityCreatedEvent> context)
    {
        logger.LogInformation(
            "Entity created event received — EntityId: {EntityId}",
            context.Message.EntityId);

        return Task.CompletedTask;
    }
}
