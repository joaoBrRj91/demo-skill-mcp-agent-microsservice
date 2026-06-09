using JL.Commerce.Tecnology.Service.Domain.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Integration.Messaging.Consumers;

public sealed class CatalogProductCreatedConsumer(ILogger<CatalogProductCreatedConsumer> logger)
    : IConsumer<CatalogProductCreatedEvent>
{
    public Task Consume(ConsumeContext<CatalogProductCreatedEvent> context)
    {
        logger.LogInformation(
            "CatalogProduct created event received — ProductId: {ProductId}",
            context.Message.ProductId);

        return Task.CompletedTask;
    }
}
