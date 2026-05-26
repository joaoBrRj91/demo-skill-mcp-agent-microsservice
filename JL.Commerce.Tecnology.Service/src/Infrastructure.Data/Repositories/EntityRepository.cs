using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Entity;
using JL.Commerce.Tecnology.Service.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Data.Repositories;

public sealed class EntityRepository(AppDbContext dbContext) : IEntityRepository
{
    public async Task AddAsync(Entity entity, CancellationToken ct = default)
    {
        await dbContext.Entities.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<Entity?> GetByIdAsync(EntityId id, CancellationToken ct = default) =>
        await dbContext.Entities.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Entity>> GetAllAsync(CancellationToken ct = default) =>
        await dbContext.Entities.ToListAsync(ct);

    public async Task UpdateAsync(Entity entity, CancellationToken ct = default)
    {
        dbContext.Entities.Update(entity);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(EntityId id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity is not null)
        {
            dbContext.Entities.Remove(entity);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
