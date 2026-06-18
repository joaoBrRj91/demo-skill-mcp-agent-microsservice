namespace JL.Commerce.Tecnology.Service.Domain.Aggregates.User;

public sealed record UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
