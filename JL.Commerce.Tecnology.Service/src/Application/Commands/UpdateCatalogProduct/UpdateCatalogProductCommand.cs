using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.UpdateCatalogProduct;

public sealed record UpdateCatalogProductCommand(
    Guid    Id,
    string  Name,
    string  Description,
    string  Sku,
    decimal Price,
    int     StockQuantity,
    string  Category,
    bool    IsActive) : IRequest;
