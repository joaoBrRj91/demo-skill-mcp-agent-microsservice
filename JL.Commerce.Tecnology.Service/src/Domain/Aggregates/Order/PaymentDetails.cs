namespace JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

public class PaymentDetails
{
    public PaymentMethod Method { get; init; }
    public string? CardNumber { get; init; }
    public string? HolderName { get; init; }
    public string? Expiry { get; init; }
    public string? Cvv { get; init; }
    public string? PixKey { get; init; }

    public PaymentDetails(PaymentMethod method, string? cardNumber, string? holderName, string? expiry, string? cvv, string? pixKey)
    {
        Method = method;
        CardNumber = cardNumber;
        HolderName = holderName;
        Expiry = expiry;
        Cvv = cvv;
        PixKey = pixKey;
    }

    private PaymentDetails() { }
}
