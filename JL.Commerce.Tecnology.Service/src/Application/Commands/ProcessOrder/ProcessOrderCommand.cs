using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Commands.ProcessOrder;

public sealed record ProcessOrderCommand(Guid OrderId) : IRequest;
