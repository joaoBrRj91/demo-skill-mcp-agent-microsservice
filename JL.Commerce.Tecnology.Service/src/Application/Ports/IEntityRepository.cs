using JL.Commerce.Tecnology.Service.Domain.Aggregates.Entity;

namespace JL.Commerce.Tecnology.Service.Application.Ports;

public interface IEntityRepository
{
    Task AddAsync(Entity entity, CancellationToken ct = default);
    Task<Entity?> GetByIdAsync(EntityId id, CancellationToken ct = default);
    Task<IReadOnlyList<Entity>> GetAllAsync(CancellationToken ct = default);
    Task UpdateAsync(Entity entity, CancellationToken ct = default);
    Task DeleteAsync(EntityId id, CancellationToken ct = default);
}
