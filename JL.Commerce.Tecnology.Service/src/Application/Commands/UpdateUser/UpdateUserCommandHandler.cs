using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.User;
using JL.Commerce.Tecnology.Service.Domain.Exceptions;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(IUserRepository repository)
    : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(new UserId(cmd.Id), ct)
            ?? throw new UserNotFoundException(cmd.Id);

        user.Update(cmd.Name, cmd.Email, cmd.IsActive);

        await repository.UpdateAsync(user, ct);
    }
}
