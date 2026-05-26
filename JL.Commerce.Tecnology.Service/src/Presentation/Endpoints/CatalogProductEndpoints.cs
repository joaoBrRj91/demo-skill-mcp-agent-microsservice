using JL.Commerce.Tecnology.Service.Application.Commands.CreateCatalogProduct;
using JL.Commerce.Tecnology.Service.Application.Commands.DeleteCatalogProduct;
using JL.Commerce.Tecnology.Service.Application.Commands.UpdateCatalogProduct;
using JL.Commerce.Tecnology.Service.Application.DTOs;
using JL.Commerce.Tecnology.Service.Application.Queries.GetAllCatalogProducts;
using JL.Commerce.Tecnology.Service.Application.Queries.GetCatalogProductById;
using JL.Commerce.Tecnology.Service.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace JL.Commerce.Tecnology.Service.Presentation.Endpoints;

public static class CatalogProductEndpoints
{
    public static void MapCatalogProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/catalog-products")
                       .WithTags("CatalogProducts");

        group.MapPost("/", CreateAsync)
            .WithName("CreateCatalogProduct")
            .WithSummary("Creates a new catalog product")
            .Produces<object>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/", GetAllAsync)
            .WithName("GetAllCatalogProducts")
            .WithSummary("Returns all catalog products")
            .Produces<IReadOnlyList<CatalogProductDto>>();

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetCatalogProductById")
            .WithSummary("Gets a catalog product by its identifier")
            .Produces<CatalogProductDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateCatalogProduct")
            .WithSummary("Updates an existing catalog product")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteCatalogProduct")
            .WithSummary("Deletes a catalog product by its identifier")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateCatalogProductCommand cmd,
        ISender sender,
        CancellationToken ct)
    {
        var id = await sender.Send(cmd, ct);
        return TypedResults.Created($"/api/v1/catalog-products/{id}", new { id });
    }

    private static async Task<IResult> GetAllAsync(
        ISender sender,
        CancellationToken ct)
    {
        var products = await sender.Send(new GetAllCatalogProductsQuery(), ct);
        return TypedResults.Ok(products);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var dto = await sender.Send(new GetCatalogProductByIdQuery(id), ct);
        return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateCatalogProductRequest body,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            var cmd = new UpdateCatalogProductCommand(
                id,
                body.Name,
                body.Description,
                body.Sku,
                body.Price,
                body.StockQuantity,
                body.Category,
                body.IsActive);

            await sender.Send(cmd, ct);
            return TypedResults.NoContent();
        }
        catch (CatalogProductNotFoundException ex)
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
            await sender.Send(new DeleteCatalogProductCommand(id), ct);
            return TypedResults.NoContent();
        }
        catch (CatalogProductNotFoundException ex)
        {
            return TypedResults.NotFound(new { ex.Message });
        }
    }
}

public sealed record UpdateCatalogProductRequest(
    string  Name,
    string  Description,
    string  Sku,
    decimal Price,
    int     StockQuantity,
    string  Category,
    bool    IsActive);
