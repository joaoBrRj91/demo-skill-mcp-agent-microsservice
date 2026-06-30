using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

namespace JL.Commerce.Tecnology.Service.Application.Ports;

public interface IAuditLogRepository
{
    Task AppendAsync(OrderAuditLog entry, CancellationToken ct = default);
}
