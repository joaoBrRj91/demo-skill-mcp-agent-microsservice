using JL.Commerce.Tecnology.Service.Domain.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Integration.Messaging.Consumers;

public sealed class UserCreatedConsumer(ILogger<UserCreatedConsumer> logger)
    : IConsumer<UserCreatedEvent>
{
    public Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        logger.LogInformation(
            "User created event received — UserId: {UserId}",
            context.Message.UserId);

        return Task.CompletedTask;
    }
}
