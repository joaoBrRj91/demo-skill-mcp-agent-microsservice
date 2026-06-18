using JL.Commerce.Tecnology.Service.Domain.Abstractions;
using JL.Commerce.Tecnology.Service.Domain.Events;

namespace JL.Commerce.Tecnology.Service.Domain.Aggregates.Entity;

public sealed class Entity : AggregateRoot<EntityId>
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Entity() { }  // EF Core constructor

    public static Entity Create(string name, string description)
    {
        var entity = new Entity
        {
            Id          = EntityId.New(),
            Name        = name,
            Description = description,
            CreatedAt   = DateTime.UtcNow
        };
        entity.AddDomainEvent(new EntityCreatedEvent(entity.Id));
        return entity;
    }

    public void Update(string name, string description)
    {
        Name        = name;
        Description = description;
        UpdatedAt   = DateTime.UtcNow;
    }
}
