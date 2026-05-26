using AutoMapper;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.CatalogProduct;

namespace JL.Commerce.Tecnology.Service.Application.Mappings;

public sealed class CatalogProductMappingProfile : Profile
{
    public CatalogProductMappingProfile()
    {
        CreateMap<CatalogProduct, CatalogProductDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.Value));
    }
}
