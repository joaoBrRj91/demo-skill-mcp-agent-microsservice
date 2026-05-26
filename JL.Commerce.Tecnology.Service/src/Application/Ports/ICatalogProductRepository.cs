using JL.Commerce.Tecnology.Service.Domain.Aggregates.CatalogProduct;

namespace JL.Commerce.Tecnology.Service.Application.Ports;

public interface ICatalogProductRepository
{
    Task AddAsync(CatalogProduct product, CancellationToken ct = default);
    Task<CatalogProduct?> GetByIdAsync(CatalogProductId id, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> GetAllAsync(CancellationToken ct = default);
    Task UpdateAsync(CatalogProduct product, CancellationToken ct = default);
    Task DeleteAsync(CatalogProductId id, CancellationToken ct = default);
}
