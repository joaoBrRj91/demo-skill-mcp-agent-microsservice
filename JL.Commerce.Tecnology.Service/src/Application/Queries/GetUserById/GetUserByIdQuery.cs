using JL.Commerce.Tecnology.Service.Application.DTOs;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserDto?>;
