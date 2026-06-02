using FluentValidation;

namespace JL.Commerce.Tecnology.Service.Application.Commands.DeleteCatalogProduct;

public sealed class DeleteCatalogProductCommandValidator : AbstractValidator<DeleteCatalogProductCommand>
{
    public DeleteCatalogProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");
    }
}
