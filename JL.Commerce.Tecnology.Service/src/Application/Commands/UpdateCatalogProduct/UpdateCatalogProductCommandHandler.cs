using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.CatalogProduct;
using JL.Commerce.Tecnology.Service.Domain.Exceptions;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.UpdateCatalogProduct;

public sealed class UpdateCatalogProductCommandHandler(ICatalogProductRepository repository)
    : IRequestHandler<UpdateCatalogProductCommand>
{
    public async Task Handle(UpdateCatalogProductCommand cmd, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(new CatalogProductId(cmd.Id), ct)
            ?? throw new CatalogProductNotFoundException(cmd.Id);

        product.Update(cmd.Name, cmd.Description, cmd.Sku, cmd.Price, cmd.StockQuantity, cmd.Category, cmd.IsActive);

        await repository.UpdateAsync(product, ct);
    }
}
