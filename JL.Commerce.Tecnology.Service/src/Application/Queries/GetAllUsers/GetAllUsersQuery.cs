using JL.Commerce.Tecnology.Service.Application.DTOs;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Queries.GetAllUsers;

public sealed record GetAllUsersQuery : IRequest<IReadOnlyList<UserDto>>;
