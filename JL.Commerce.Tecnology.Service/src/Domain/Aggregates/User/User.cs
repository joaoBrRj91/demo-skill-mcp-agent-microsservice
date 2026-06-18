using JL.Commerce.Tecnology.Service.Domain.Abstractions;
using JL.Commerce.Tecnology.Service.Domain.Events;

namespace JL.Commerce.Tecnology.Service.Domain.Aggregates.User;

public sealed class User : AggregateRoot<UserId>
{
    public string    Name      { get; private set; } = default!;
    public string    Email     { get; private set; } = default!;
    public bool      IsActive  { get; private set; }
    public DateTime  CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private User() { }  // EF Core constructor

    public static User Create(string name, string email)
    {
        var user = new User
        {
            Id        = UserId.New(),
            Name      = name,
            Email     = email,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        };
        user.AddDomainEvent(new UserCreatedEvent(user.Id));
        return user;
    }

    public void Update(string name, string email, bool isActive)
    {
        Name      = name;
        Email     = email;
        IsActive  = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
