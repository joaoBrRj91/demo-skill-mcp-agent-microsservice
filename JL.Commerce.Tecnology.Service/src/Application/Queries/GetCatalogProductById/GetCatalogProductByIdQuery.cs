using JL.Commerce.Tecnology.Service.Application.DTOs;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Queries.GetCatalogProductById;

public sealed record GetCatalogProductByIdQuery(Guid Id) : IRequest<CatalogProductDto?>;
