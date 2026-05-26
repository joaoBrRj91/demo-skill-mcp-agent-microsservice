using AutoMapper;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Entity;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Queries.GetEntityById;

public sealed class GetEntityByIdQueryHandler(IEntityRepository repository, IMapper mapper)
    : IRequestHandler<GetEntityByIdQuery, EntityDto?>
{
    public async Task<EntityDto?> Handle(GetEntityByIdQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(new EntityId(query.Id), ct);
        return entity is null ? null : mapper.Map<EntityDto>(entity);
    }
}
