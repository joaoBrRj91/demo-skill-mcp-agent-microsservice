using Asp.Versioning;
using JL.Commerce.Tecnology.Service.Application.Commands.CreateEntity;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Application.Queries.GetEntityById;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace JL.Commerce.Tecnology.Service.Presentation.Endpoints;

public static class EntityEndpoints
{
    public static void MapEntityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/entities")
                       .WithTags("Entities");

        group.MapPost("/", async (
            [FromBody] CreateEntityCommand cmd,
            ISender sender,
            CancellationToken ct) =>
        {
            var id = await sender.Send(cmd, ct);
            return Results.Created($"/api/v1/entities/{id}", new { id });
        })
        .WithName("CreateEntity")
        .WithSummary("Creates a new entity")
        .Produces<object>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var dto = await sender.Send(new GetEntityByIdQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("GetEntityById")
        .WithSummary("Gets an entity by its identifier")
        .Produces<EntityDto>()
        .Produces(StatusCodes.Status404NotFound);
    }
}
