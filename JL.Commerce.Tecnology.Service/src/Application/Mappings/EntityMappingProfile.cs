using AutoMapper;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Entity;

namespace JL.Commerce.Tecnology.Service.Application.Mappings;

public sealed class EntityMappingProfile : Profile
{
    public EntityMappingProfile()
    {
        CreateMap<Entity, EntityDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.Value));
    }
}
