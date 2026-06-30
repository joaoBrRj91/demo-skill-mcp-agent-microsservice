using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using JL.Commerce.Tecnology.Service.Domain.Events;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler(
    IOrderRepository repository,
    IEventBus eventBus)
    : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var existing = await repository.GetByTransactionIdAsync(cmd.TransactionId, ct);
        if (existing is not null)
            return existing.Id.Value;

        var items = cmd.Items
            .Select(i => new OrderItem(i.CatalogProductId, i.Quantity, i.UnitPrice))
            .ToList();

        var payment = new PaymentDetails(
            cmd.Payment.Method,
            Sanitize(cmd.Payment.CardNumber),
            Sanitize(cmd.Payment.HolderName),
            Sanitize(cmd.Payment.Expiry),
            Sanitize(cmd.Payment.Cvv),
            Sanitize(cmd.Payment.PixKey));

        var address = new ShippingAddress(
            Sanitize(cmd.Address.Street)!,
            Sanitize(cmd.Address.City)!,
            Sanitize(cmd.Address.State)!,
            Sanitize(cmd.Address.ZipCode)!,
            Sanitize(cmd.Address.Country)!);

        var order = Order.Create(cmd.TransactionId, cmd.UserId, items, payment, address);

        await repository.AddAsync(order, ct);
        await eventBus.PublishAsync(new OrderCreatedEvent(order.Id), ct);

        return order.Id.Value;
    }

    private static string? Sanitize(string? value)
    {
        if (value is null) return null;
        value = value.Replace("\0", string.Empty);
        value = System.Text.RegularExpressions.Regex.Replace(value, "<[^>]*>", string.Empty);
        value = new string(value.Where(c => !char.IsControl(c)).ToArray());
        return value;
    }
}
