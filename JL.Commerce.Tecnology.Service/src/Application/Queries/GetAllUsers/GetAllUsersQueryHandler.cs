using AutoMapper;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Application.Ports;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Queries.GetAllUsers;

public sealed class GetAllUsersQueryHandler(IUserRepository repository, IMapper mapper)
    : IRequestHandler<GetAllUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(GetAllUsersQuery query, CancellationToken ct)
    {
        var users = await repository.GetAllAsync(ct);
        return mapper.Map<IReadOnlyList<UserDto>>(users);
    }
}
