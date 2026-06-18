using JL.Commerce.Tecnology.Service.Application.Commands.CreateUser;
using JL.Commerce.Tecnology.Service.Application.Commands.DeleteUser;
using JL.Commerce.Tecnology.Service.Application.Commands.UpdateUser;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Application.Queries.GetAllUsers;
using JL.Commerce.Tecnology.Service.Application.Queries.GetUserById;
using JL.Commerce.Tecnology.Service.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace JL.Commerce.Tecnology.Service.Presentation.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/users")
                       .WithTags("Users");

        group.MapPost("/", CreateAsync)
            .WithName("CreateUser")
            .WithSummary("Creates a new user")
            .Produces<object>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/", GetAllAsync)
            .WithName("GetAllUsers")
            .WithSummary("Returns all users")
            .Produces<IReadOnlyList<UserDto>>();

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetUserById")
            .WithSummary("Gets a user by its identifier")
            .Produces<UserDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateUser")
            .WithSummary("Updates an existing user")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteUser")
            .WithSummary("Deletes a user by its identifier")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateUserCommand cmd,
        ISender sender,
        CancellationToken ct)
    {
        var id = await sender.Send(cmd, ct);
        return TypedResults.Created($"/api/v1/users/{id}", new { id });
    }

    private static async Task<IResult> GetAllAsync(
        ISender sender,
        CancellationToken ct)
    {
        var users = await sender.Send(new GetAllUsersQuery(), ct);
        return TypedResults.Ok(users);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var dto = await sender.Send(new GetUserByIdQuery(id), ct);
        return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateUserRequest body,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            var cmd = new UpdateUserCommand(id, body.Name, body.Email, body.IsActive);
            await sender.Send(cmd, ct);
            return TypedResults.NoContent();
        }
        catch (UserNotFoundException ex)
        {
            return TypedResults.NotFound(new { ex.Message });
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            await sender.Send(new DeleteUserCommand(id), ct);
            return TypedResults.NoContent();
        }
        catch (UserNotFoundException ex)
        {
            return TypedResults.NotFound(new { ex.Message });
        }
    }
}

public sealed record UpdateUserRequest(string Name, string Email, bool IsActive);
