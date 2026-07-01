using JL.Commerce.Tecnology.Service.Application.Commands.ProcessOrder;
using JL.Commerce.Tecnology.Service.Domain.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Integration.Messaging.Consumers;

public sealed class OrderCreatedConsumer(ISender sender, ILogger<OrderCreatedConsumer> logger)
    : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        logger.LogInformation(
            "Order created event received — OrderId: {OrderId}",
            context.Message.OrderId);

        await sender.Send(new ProcessOrderCommand(context.Message.OrderId.Value), context.CancellationToken);
    }
}
