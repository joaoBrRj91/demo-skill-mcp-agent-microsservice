using AutoMapper;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.CatalogProduct;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Queries.GetCatalogProductById;

public sealed class GetCatalogProductByIdQueryHandler(ICatalogProductRepository repository, IMapper mapper)
    : IRequestHandler<GetCatalogProductByIdQuery, CatalogProductDto?>
{
    public async Task<CatalogProductDto?> Handle(GetCatalogProductByIdQuery query, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(new CatalogProductId(query.Id), ct);
        return product is null ? null : mapper.Map<CatalogProductDto>(product);
    }
}
