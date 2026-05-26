using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.CatalogProduct;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.CreateCatalogProduct;

public sealed class CreateCatalogProductCommandHandler(ICatalogProductRepository repository)
    : IRequestHandler<CreateCatalogProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateCatalogProductCommand cmd, CancellationToken ct)
    {
        var product = CatalogProduct.Create(
            cmd.Name,
            cmd.Description,
            cmd.Sku,
            cmd.Price,
            cmd.StockQuantity,
            cmd.Category);

        await repository.AddAsync(product, ct);
        return product.Id.Value;
    }
}
