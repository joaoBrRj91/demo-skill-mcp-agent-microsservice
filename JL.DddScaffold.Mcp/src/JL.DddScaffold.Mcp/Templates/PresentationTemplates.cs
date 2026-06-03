using JL.DddScaffold.Mcp.Models;
using JL.DddScaffold.Mcp.Tools;

namespace JL.DddScaffold.Mcp.Templates;

internal static class PresentationTemplates
{
    /// <summary>Presentation/Endpoints/{Name}Endpoints.cs</summary>
    internal static (string, string) Endpoints(string name, string rootNs, List<PropertyDefinition> props)
    {
        var ns = $"{rootNs}.Presentation.Endpoints";
        var plural = ScaffoldHelpers.Pluralize(name);
        var kebab = ScaffoldHelpers.ToKebabCase(plural);
        var appNs = $"{rootNs}.Application";
        var exceptionsNs = $"{rootNs}.Domain.Exceptions";

        // Build request record fields (same as UpdateCommand minus Guid Id)
        var requestFields = string.Join(",\n    ", props.Select(p =>
            $"{ScaffoldHelpers.TypeDecl(p),9} {p.Name}"));

        // Build create command constructor args
        var createArgs = string.Join(",\n            ", props
            .Where(p => p.Name != "IsActive")
            .Select(p => $"body.{p.Name}"));

        // Build update command constructor args
        var updateArgs = "id,\n            " + string.Join(",\n            ", props.Select(p => $"body.{p.Name}"));

        var content = $$"""
using {{appNs}}.Commands.Create{{name}};
using {{appNs}}.Commands.Delete{{name}};
using {{appNs}}.Commands.Update{{name}};
using {{appNs}}.DTOs;
using {{appNs}}.Queries.GetAll{{plural}};
using {{appNs}}.Queries.Get{{name}}ById;
using {{exceptionsNs}};
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace {{ns}};

public static class {{name}}Endpoints
{
    public static void Map{{name}}Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/{{kebab}}")
                       .WithTags("{{plural}}");

        group.MapPost("/", CreateAsync)
            .WithName("Create{{name}}")
            .WithSummary("Creates a new {{name}}")
            .Produces<object>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/", GetAllAsync)
            .WithName("GetAll{{plural}}")
            .WithSummary("Returns all {{plural}}")
            .Produces<IReadOnlyList<{{name}}Dto>>();

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("Get{{name}}ById")
            .WithSummary("Gets a {{name}} by its identifier")
            .Produces<{{name}}Dto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("Update{{name}}")
            .WithSummary("Updates an existing {{name}}")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("Delete{{name}}")
            .WithSummary("Deletes a {{name}} by its identifier")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] Create{{name}}Command cmd,
        ISender sender,
        CancellationToken ct)
    {
        var id = await sender.Send(cmd, ct);
        return TypedResults.Created($"/api/v1/{{kebab}}/{id}", new { id });
    }

    private static async Task<IResult> GetAllAsync(
        ISender sender,
        CancellationToken ct)
    {
        var items = await sender.Send(new GetAll{{plural}}Query(), ct);
        return TypedResults.Ok(items);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var dto = await sender.Send(new Get{{name}}ByIdQuery(id), ct);
        return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        [FromBody] Update{{name}}Request body,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            var cmd = new Update{{name}}Command(
                {{updateArgs}});
            await sender.Send(cmd, ct);
            return TypedResults.NoContent();
        }
        catch ({{name}}NotFoundException ex)
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
            await sender.Send(new Delete{{name}}Command(id), ct);
            return TypedResults.NoContent();
        }
        catch ({{name}}NotFoundException ex)
        {
            return TypedResults.NotFound(new { ex.Message });
        }
    }
}

public sealed record Update{{name}}Request(
    {{requestFields}});
""";
        return ($"Presentation/Endpoints/{name}Endpoints.cs", content);
    }
}
