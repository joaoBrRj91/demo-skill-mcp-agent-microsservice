using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Entity;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.CreateEntity;

public sealed class CreateEntityCommandHandler(IEntityRepository repository)
    : IRequestHandler<CreateEntityCommand, Guid>
{
    public async Task<Guid> Handle(CreateEntityCommand cmd, CancellationToken ct)
    {
        var entity = Entity.Create(cmd.Name, cmd.Description);
        await repository.AddAsync(entity, ct);
        return entity.Id.Value;
    }
}
