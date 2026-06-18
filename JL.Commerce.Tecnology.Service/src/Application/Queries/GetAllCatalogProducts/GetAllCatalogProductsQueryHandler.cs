using AutoMapper;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Application.Ports;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Queries.GetAllCatalogProducts;

public sealed class GetAllCatalogProductsQueryHandler(ICatalogProductRepository repository, IMapper mapper)
    : IRequestHandler<GetAllCatalogProductsQuery, IReadOnlyList<CatalogProductDto>>
{
    public async Task<IReadOnlyList<CatalogProductDto>> Handle(GetAllCatalogProductsQuery query, CancellationToken ct)
    {
        var products = await repository.GetAllAsync(ct);
        return mapper.Map<IReadOnlyList<CatalogProductDto>>(products);





        
    }
}
