using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using JL.Commerce.Tecnology.Service.Domain.Events;
using JL.Commerce.Tecnology.Service.Domain.Exceptions;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.ProcessOrder;

public sealed class ProcessOrderCommandHandler(
    IOrderRepository repository,
    IPaymentGateway paymentGateway,
    IAuditLogRepository auditLogRepository)
    : IRequestHandler<ProcessOrderCommand>
{
    public async Task Handle(ProcessOrderCommand cmd, CancellationToken ct)
    {
        var order = await repository.GetByIdAsync(new OrderId(cmd.OrderId), ct)
            ?? throw new OrderNotFoundException(cmd.OrderId);

        if (order.Status != OrderStatus.Processing)
            return;

        var totalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        var request = new PaymentRequest(order.Id.Value, totalAmount, order.Payment);
        var result = await paymentGateway.ProcessAsync(request, ct);

        var fromState = order.Status.ToString();

        if (result.Success)
            order.MarkAsProcessed();
        else
            order.MarkAsError(result.ErrorMessage!);

        await repository.UpdateAsync(order, ct);

        var triggeredBy = result.Success
            ? nameof(OrderProcessedEvent)
            : nameof(OrderErrorEvent);

        var auditEntry = new OrderAuditLog(
            order.Id.Value,
            order.TransactionId,
            fromState,
            order.Status.ToString(),
            DateTime.UtcNow,
            triggeredBy);

        await auditLogRepository.AppendAsync(auditEntry, ct);
    }
}
