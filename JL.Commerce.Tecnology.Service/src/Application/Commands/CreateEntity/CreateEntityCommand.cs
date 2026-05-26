using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.CreateEntity;

public sealed record CreateEntityCommand(string Name, string Description) : IRequest<Guid>;
