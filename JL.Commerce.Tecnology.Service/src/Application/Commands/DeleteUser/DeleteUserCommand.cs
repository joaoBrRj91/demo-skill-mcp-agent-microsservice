using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid Id) : IRequest;
