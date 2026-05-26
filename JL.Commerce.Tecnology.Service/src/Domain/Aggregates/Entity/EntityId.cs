namespace JL.Commerce.Tecnology.Service.Domain.Aggregates.Entity;

public sealed record EntityId(Guid Value)
{
    public static EntityId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
