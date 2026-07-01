using JL.Commerce.Tecnology.Service.Application.Ports;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Integration.PaymentGateway;

public sealed class MockPaymentGateway : IPaymentGateway
{
    public Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken ct = default) =>
        Task.FromResult(new PaymentResult(Success: true, ErrorMessage: null));
}
