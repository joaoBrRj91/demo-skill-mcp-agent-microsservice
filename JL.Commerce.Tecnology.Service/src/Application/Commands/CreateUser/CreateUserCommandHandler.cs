using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.User;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.CreateUser;

public sealed class CreateUserCommandHandler(IUserRepository repository)
    : IRequestHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
       return Guid.NewGuid();
    }
}
