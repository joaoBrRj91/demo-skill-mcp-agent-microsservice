using AutoMapper;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.User;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(IUserRepository repository, IMapper mapper)
    : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    public async Task<UserDto?> Handle(GetUserByIdQuery query, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(new UserId(query.Id), ct);
        return user is null ? null : mapper.Map<UserDto>(user);
    }
}
