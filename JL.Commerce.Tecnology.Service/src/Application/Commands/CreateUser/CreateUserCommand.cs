using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.CreateUser;

public sealed record CreateUserCommand(
    string Name,
    string Email) : IRequest<Guid>;
