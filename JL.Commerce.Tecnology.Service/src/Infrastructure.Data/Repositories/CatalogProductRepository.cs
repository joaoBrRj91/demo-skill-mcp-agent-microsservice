using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.CatalogProduct;
using JL.Commerce.Tecnology.Service.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Data.Repositories;

public sealed class CatalogProductRepository(AppDbContext dbContext) : ICatalogProductRepository
{
    public async Task AddAsync(CatalogProduct product, CancellationToken ct = default)
    {
        await dbContext.CatalogProducts.AddAsync(product, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<CatalogProduct?> GetByIdAsync(CatalogProductId id, CancellationToken ct = default) =>
        await dbContext.CatalogProducts.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<CatalogProduct>> GetAllAsync(CancellationToken ct = default) =>
        await dbContext.CatalogProducts.ToListAsync(ct);

    public async Task UpdateAsync(CatalogProduct product, CancellationToken ct = default)
    {
        dbContext.CatalogProducts.Update(product);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(CatalogProductId id, CancellationToken ct = default)
    {
        var product = await GetByIdAsync(id, ct);
        if (product is not null)
        {
            dbContext.CatalogProducts.Remove(product);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
