namespace JL.Commerce.Tecnology.Service.Domain.Abstractions;

public abstract class AggregateRoot<TId>
{
    public TId Id { get; protected set; } = default!;

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public interface IDomainEvent { }
