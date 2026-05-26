using JL.Commerce.Tecnology.Service.Domain.Abstractions;
using JL.Commerce.Tecnology.Service.Domain.Events;

namespace JL.Commerce.Tecnology.Service.Domain.Aggregates.CatalogProduct;

public sealed class CatalogProduct : AggregateRoot<CatalogProductId>
{
    public string Name          { get; private set; } = default!;
    public string Description   { get; private set; } = default!;
    public string Sku           { get; private set; } = default!;
    public decimal Price        { get; private set; }
    public int StockQuantity    { get; private set; }
    public string Category      { get; private set; } = default!;
    public bool IsActive        { get; private set; }
    public DateTime CreatedAt   { get; private set; }
    public DateTime? UpdatedAt  { get; private set; }

    private CatalogProduct() { }  // EF Core constructor

    public static CatalogProduct Create(
        string name,
        string description,
        string sku,
        decimal price,
        int stockQuantity,
        string category)
    {
        var product = new CatalogProduct
        {
            Id            = CatalogProductId.New(),
            Name          = name,
            Description   = description,
            Sku           = sku,
            Price         = price,
            StockQuantity = stockQuantity,
            Category      = category,
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow
        };
        product.AddDomainEvent(new CatalogProductCreatedEvent(product.Id));
        return product;
    }

    public void Update(
        string name,
        string description,
        string sku,
        decimal price,
        int stockQuantity,
        string category,
        bool isActive)
    {
        Name          = name;
        Description   = description;
        Sku           = sku;
        Price         = price;
        StockQuantity = stockQuantity;
        Category      = category;
        IsActive      = isActive;
        UpdatedAt     = DateTime.UtcNow;
    }
}
