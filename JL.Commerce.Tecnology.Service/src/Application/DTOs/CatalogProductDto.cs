namespace JL.Commerce.Tecnology.Service.Application.DTOs;

public sealed record CatalogProductDto(
    Guid      Id,
    string    Name,
    string    Description,
    string    Sku,
    decimal   Price,
    int       StockQuantity,
    string    Category,
    bool      IsActive,
    DateTime  CreatedAt,
    DateTime? UpdatedAt);
