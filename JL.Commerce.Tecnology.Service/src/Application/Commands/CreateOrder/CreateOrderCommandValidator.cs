using FluentValidation;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;

namespace JL.Commerce.Tecnology.Service.Application.Commands.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.CatalogProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThanOrEqualTo(1);
            item.RuleFor(i => i.UnitPrice).GreaterThan(0);
        });

        RuleFor(x => x.Payment.Method)
            .IsInEnum().WithMessage("Payment method must be CreditCard or Pix.");

        When(x => x.Payment.Method == PaymentMethod.CreditCard, () =>
        {
            RuleFor(x => x.Payment.CardNumber)
                .NotEmpty()
                .Length(13, 19).WithMessage("CardNumber must be 13–19 characters.");
            RuleFor(x => x.Payment.HolderName).NotEmpty();
            RuleFor(x => x.Payment.Expiry)
                .NotEmpty()
                .Matches(@"^(0[1-9]|1[0-2])\/\d{2}$").WithMessage("Expiry must match MM/YY format.");
            RuleFor(x => x.Payment.Cvv)
                .NotEmpty()
                .Matches(@"^\d{3,4}$").WithMessage("CVV must be 3 or 4 digits.");
        });

        When(x => x.Payment.Method == PaymentMethod.Pix, () =>
        {
            RuleFor(x => x.Payment.PixKey).NotEmpty();
        });

        RuleFor(x => x.Address.Street).NotEmpty().Must(NoNullBytesOrControlChars);
        RuleFor(x => x.Address.City).NotEmpty().Must(NoNullBytesOrControlChars);
        RuleFor(x => x.Address.State).NotEmpty().Must(NoNullBytesOrControlChars);
        RuleFor(x => x.Address.ZipCode).NotEmpty().Must(NoNullBytesOrControlChars);
        RuleFor(x => x.Address.Country).NotEmpty().Must(NoNullBytesOrControlChars);
    }

    private static bool NoNullBytesOrControlChars(string? value) =>
        value is null || (!value.Contains('\0') && value.All(c => !char.IsControl(c)));
}
