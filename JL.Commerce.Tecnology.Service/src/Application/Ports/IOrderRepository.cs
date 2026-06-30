using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

namespace JL.Commerce.Tecnology.Service.Application.Ports;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default);
    Task<Order?> GetByTransactionIdAsync(Guid transactionId, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
}
