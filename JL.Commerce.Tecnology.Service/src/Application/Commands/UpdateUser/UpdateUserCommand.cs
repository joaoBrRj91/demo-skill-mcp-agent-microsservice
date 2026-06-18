using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    Guid   Id,
    string Name,
    string Email,
    bool   IsActive) : IRequest;
