using JL.Commerce.Tecnology.Service.Application.Commands.CreateOrder;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Application.Queries.GetOrderStatus;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace JL.Commerce.Tecnology.Service.Presentation.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/orders")
                       .WithTags("Orders");

        group.MapPost("/", CreateAsync)
            .WithName("CreateOrder")
            .WithSummary("Submits an order for async processing")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .AddEndpointFilter(SecurityHeadersFilter);

        group.MapGet("/{transactionId:guid}", GetStatusAsync)
            .WithName("GetOrderStatus")
            .WithSummary("Polls the current status of an order by transactionId")
            .Produces<OrderPollingDto>()
            .Produces(StatusCodes.Status404NotFound)
            .AddEndpointFilter(SecurityHeadersFilter);
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateOrderCommand cmd,
        ISender sender,
        CancellationToken ct)
    {
        var orderId = await sender.Send(cmd, ct);
        return TypedResults.Accepted(
            $"/api/v1/orders/{cmd.TransactionId}",
            new { transactionId = cmd.TransactionId, status = "Processing" });
    }

    private static async Task<IResult> GetStatusAsync(
        Guid transactionId,
        ISender sender,
        CancellationToken ct)
    {
        var dto = await sender.Send(new GetOrderStatusQuery(transactionId), ct);
        return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
    }

    private static async ValueTask<object?> SecurityHeadersFilter(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var result = await next(ctx);
        var response = ctx.HttpContext.Response;
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["Cache-Control"] = "no-store, no-cache";
        response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        return result;
    }
}
