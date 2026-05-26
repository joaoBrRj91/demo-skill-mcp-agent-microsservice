using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.CatalogProduct;
using JL.Commerce.Tecnology.Service.Domain.Exceptions;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.DeleteCatalogProduct;

public sealed class DeleteCatalogProductCommandHandler(ICatalogProductRepository repository)
    : IRequestHandler<DeleteCatalogProductCommand>
{
    public async Task Handle(DeleteCatalogProductCommand cmd, CancellationToken ct)
    {
        var id = new CatalogProductId(cmd.Id);
        var exists = await repository.GetByIdAsync(id, ct)
            ?? throw new CatalogProductNotFoundException(cmd.Id);

        await repository.DeleteAsync(id, ct);
    }
}
