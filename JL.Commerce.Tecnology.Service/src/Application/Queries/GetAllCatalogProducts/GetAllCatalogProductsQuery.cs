using JL.Commerce.Tecnology.Service.Application.DTOs;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Queries.GetAllCatalogProducts;

public sealed record GetAllCatalogProductsQuery : IRequest<IReadOnlyList<CatalogProductDto>>;






