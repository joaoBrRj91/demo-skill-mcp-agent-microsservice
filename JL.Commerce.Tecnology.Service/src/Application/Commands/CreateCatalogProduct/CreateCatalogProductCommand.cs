using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.CreateCatalogProduct;

public sealed record CreateCatalogProductCommand(
    string  Name,
    string  Description,
    string  Sku,
    decimal Price,
    int     StockQuantity,
    string  Category) : IRequest<Guid>;
