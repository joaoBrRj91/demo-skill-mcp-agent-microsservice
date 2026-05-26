using JL.Commerce.Tecnology.Service.Application.DTOs;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Queries.GetEntityById;

public sealed record GetEntityByIdQuery(Guid Id) : IRequest<EntityDto?>;
