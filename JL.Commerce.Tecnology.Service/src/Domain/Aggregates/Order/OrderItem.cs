namespace JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

public class OrderItem
{
    public Guid CatalogProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }

    public OrderItem(Guid catalogProductId, int quantity, decimal unitPrice)
    {
        CatalogProductId = catalogProductId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    private OrderItem() { }
}
