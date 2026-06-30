using AutoMapper;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Queries.GetOrderStatus;

public sealed class GetOrderStatusQueryHandler(
    IOrderRepository repository,
    IMapper mapper)
    : IRequestHandler<GetOrderStatusQuery, OrderPollingDto?>
{
    public async Task<OrderPollingDto?> Handle(GetOrderStatusQuery query, CancellationToken ct)
    {
        var order = await repository.GetByTransactionIdAsync(query.TransactionId, ct);
        if (order is null)
            return null;

        if (order.Status == OrderStatus.Processing)
        {
            return new OrderPollingDto(
                order.TransactionId,
                order.Status.ToString(),
                null,
                null);
        }

        return mapper.Map<OrderPollingDto>(order);
    }
}
