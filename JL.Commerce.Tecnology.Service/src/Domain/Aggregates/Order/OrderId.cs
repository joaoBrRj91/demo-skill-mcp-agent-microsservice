namespace JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

public sealed record OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
