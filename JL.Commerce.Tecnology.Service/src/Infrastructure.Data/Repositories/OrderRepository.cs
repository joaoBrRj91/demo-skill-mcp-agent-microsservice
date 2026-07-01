using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using JL.Commerce.Tecnology.Service.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Data.Repositories;

public sealed class OrderRepository(AppDbContext dbContext) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await dbContext.Orders.AddAsync(order, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default) =>
        await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Order?> GetByTransactionIdAsync(Guid transactionId, CancellationToken ct = default) =>
        await dbContext.Orders.FirstOrDefaultAsync(o => o.TransactionId == transactionId, ct);

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        dbContext.Orders.Update(order);
        await dbContext.SaveChangesAsync(ct);
    }
}
