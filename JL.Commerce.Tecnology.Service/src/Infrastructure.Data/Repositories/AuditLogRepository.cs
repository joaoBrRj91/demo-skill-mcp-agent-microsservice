using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using JL.Commerce.Tecnology.Service.Infrastructure.Data.Context;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Data.Repositories;

public sealed class AuditLogRepository(AppDbContext dbContext) : IAuditLogRepository
{
    public async Task AppendAsync(OrderAuditLog entry, CancellationToken ct = default)
    {
        await dbContext.OrderAuditLogs.AddAsync(entry, ct);
        await dbContext.SaveChangesAsync(ct);
    }
}
