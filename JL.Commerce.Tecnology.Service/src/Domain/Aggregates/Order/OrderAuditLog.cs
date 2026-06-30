namespace JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

public class OrderAuditLog
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public Guid TransactionId { get; init; }
    public string FromState { get; init; } = string.Empty;
    public string ToState { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; }
    public string TriggeredByEvent { get; init; } = string.Empty;

    public OrderAuditLog(
        Guid orderId,
        Guid transactionId,
        string fromState,
        string toState,
        DateTime occurredAtUtc,
        string triggeredByEvent)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        TransactionId = transactionId;
        FromState = fromState;
        ToState = toState;
        OccurredAtUtc = occurredAtUtc;
        TriggeredByEvent = triggeredByEvent;
    }

    private OrderAuditLog() { }
}
