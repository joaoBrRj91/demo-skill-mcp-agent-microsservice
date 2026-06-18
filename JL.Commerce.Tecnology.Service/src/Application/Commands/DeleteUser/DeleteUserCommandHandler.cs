using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.User;
using JL.Commerce.Tecnology.Service.Domain.Exceptions;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(IUserRepository repository)
    : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand cmd, CancellationToken ct)
    {
        var id = new UserId(cmd.Id);
        _ = await repository.GetByIdAsync(id, ct)
            ?? throw new UserNotFoundException(cmd.Id);

        await repository.DeleteAsync(id, ct);
    }
}
