using JL.Commerce.Tecnology.Service.Application.DTOs;
using MediatR;

namespace JL.Commerce.Tecnology.Service.Application.Queries.GetOrderStatus;

public sealed record GetOrderStatusQuery(Guid TransactionId) : IRequest<OrderPollingDto?>;
