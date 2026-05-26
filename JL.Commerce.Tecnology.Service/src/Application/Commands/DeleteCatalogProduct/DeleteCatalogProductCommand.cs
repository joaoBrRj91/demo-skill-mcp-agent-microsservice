using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.DeleteCatalogProduct;

public sealed record DeleteCatalogProductCommand(Guid Id) : IRequest;
