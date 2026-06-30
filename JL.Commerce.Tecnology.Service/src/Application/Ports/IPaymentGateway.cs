using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

namespace JL.Commerce.Tecnology.Service.Application.Ports;

public interface IPaymentGateway
{
    Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken ct = default);
}

public sealed record PaymentRequest(Guid OrderId, decimal TotalAmount, PaymentDetails Payment);

public sealed record PaymentResult(bool Success, string? ErrorMessage);
