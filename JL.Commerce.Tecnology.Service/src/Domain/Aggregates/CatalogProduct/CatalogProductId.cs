namespace JL.Commerce.Tecnology.Service.Domain.Aggregates.CatalogProduct;

public sealed record CatalogProductId(Guid Value)
{
    public static CatalogProductId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
