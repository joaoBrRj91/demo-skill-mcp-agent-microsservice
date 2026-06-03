using System.ComponentModel;
using JL.DddScaffold.Mcp.Models;
using JL.DddScaffold.Mcp.Templates;
using ModelContextProtocol.Server;

namespace JL.DddScaffold.Mcp.Tools;

[McpServerToolType]
public static class ScaffoldAggregateTool
{
    [McpServerTool]
    [Description(
        "Generates all ~23 files for a complete DDD aggregate following " +
        "Hexagonal Architecture + DDD + CQRS conventions (Domain, Application, " +
        "Infrastructure.Data, Presentation layers). " +
        "Writes: aggregate class, strongly-typed ID, created event, not-found exception, " +
        "Create/Update/Delete commands with handlers and validators, " +
        "GetAll and GetById queries with handlers, DTO, AutoMapper profile, " +
        "repository port interface, EF Core configuration, repository implementation, " +
        "and Minimal API endpoints. Skips files that already exist.")]
    public static string ScaffoldAggregate(
        [Description("Absolute path to the solution's src/ directory, e.g. C:/Projects/MyService/src")]
        string srcPath,
        [Description("Root namespace of the target solution, e.g. JL.Commerce.Tecnology.Service")]
        string rootNamespace,
        [Description("PascalCase name of the aggregate, e.g. Order or CatalogProduct")]
        string aggregateName,
        [Description(
            "JSON array of property definitions. Supported fields per object: " +
            "name (string, required), type (string|int|decimal|bool|DateTime|Guid|long, default=string), " +
            "maxLength (int?), required (bool, default=true), isNullable (bool, default=false), " +
            "isUnique (bool, default=false), precision (int?), scale (int, default=2). " +
            "Do NOT include Id, CreatedAt, or UpdatedAt — those are always auto-generated. " +
            "Example: [{\"name\":\"Name\",\"type\":\"string\",\"maxLength\":200},{\"name\":\"Price\",\"type\":\"decimal\",\"precision\":18,\"scale\":2}]")]
        string propertiesJson)
    {
        var props = ScaffoldHelpers.ParseProperties(propertiesJson);
        var files = CollectAllFiles(srcPath, rootNamespace, aggregateName, props);

        var written = new List<string>();
        var skipped = new List<string>();
        var errors  = new List<string>();

        foreach (var (absPath, fileContent) in files)
        {
            try
            {
                if (File.Exists(absPath))
                {
                    skipped.Add(absPath);
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(absPath)!);
                File.WriteAllText(absPath, fileContent, System.Text.Encoding.UTF8);
                written.Add(absPath);
            }
            catch (Exception ex)
            {
                errors.Add($"{absPath}: {ex.Message}");
            }
        }

        return BuildSummary(aggregateName, written, skipped, errors);
    }

    internal static List<(string absPath, string content)> CollectAllFiles(
        string srcPath, string rootNs, string name, List<PropertyDefinition> props)
    {
        var results = new List<(string, string)>();

        void Add((string rel, string content) t) =>
            results.Add((
                Path.Combine(srcPath, t.rel.Replace('/', Path.DirectorySeparatorChar)),
                t.content));

        // Domain
        Add(DomainTemplates.AggregateClass(name, rootNs, props));
        Add(DomainTemplates.AggregateId(name, rootNs));
        Add(DomainTemplates.CreatedEvent(name, rootNs));
        Add(DomainTemplates.NotFoundException(name, rootNs));

        // Application — Commands
        Add(ApplicationTemplates.CreateCommand(name, rootNs, props));
        Add(ApplicationTemplates.CreateCommandHandler(name, rootNs, props));
        Add(ApplicationTemplates.CreateCommandValidator(name, rootNs, props));
        Add(ApplicationTemplates.UpdateCommand(name, rootNs, props));
        Add(ApplicationTemplates.UpdateCommandHandler(name, rootNs, props));
        Add(ApplicationTemplates.UpdateCommandValidator(name, rootNs, props));
        Add(ApplicationTemplates.DeleteCommand(name, rootNs));
        Add(ApplicationTemplates.DeleteCommandHandler(name, rootNs));
        Add(ApplicationTemplates.DeleteCommandValidator(name, rootNs));

        // Application — Queries
        Add(ApplicationTemplates.GetAllQuery(name, rootNs));
        Add(ApplicationTemplates.GetAllQueryHandler(name, rootNs));
        Add(ApplicationTemplates.GetByIdQuery(name, rootNs));
        Add(ApplicationTemplates.GetByIdQueryHandler(name, rootNs));

        // Application — DTO / Mapping / Port
        Add(ApplicationTemplates.Dto(name, rootNs, props));
        Add(ApplicationTemplates.MappingProfile(name, rootNs));
        Add(ApplicationTemplates.RepositoryPort(name, rootNs));

        // Infrastructure.Data
        Add(InfrastructureTemplates.EfConfiguration(name, rootNs, props));
        Add(InfrastructureTemplates.Repository(name, rootNs));

        // Presentation
        Add(PresentationTemplates.Endpoints(name, rootNs, props));

        return results;
    }

    private static string BuildSummary(
        string name, List<string> written, List<string> skipped, List<string> errors)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"scaffold_aggregate completed for '{name}'.");
        sb.AppendLine($"Written ({written.Count} files):");
        foreach (var f in written) sb.AppendLine($"  + {f}");

        if (skipped.Count > 0)
        {
            sb.AppendLine($"\nSkipped — already exist ({skipped.Count} files):");
            foreach (var f in skipped) sb.AppendLine($"  ~ {f}");
        }

        if (errors.Count > 0)
        {
            sb.AppendLine($"\nErrors ({errors.Count}):");
            foreach (var e in errors) sb.AppendLine($"  ! {e}");
        }

        sb.AppendLine($"""

NEXT STEPS (manual edits required):
  1. Add DbSet<{name}> {ScaffoldHelpers.Pluralize(name)} to AppDbContext
  2. Register DI in Program.cs:
       builder.Services.AddScoped<I{name}Repository, {name}Repository>();
  3. Register endpoints in Program.cs:
       app.Map{name}Endpoints();
  4. Create EF migration:
       dotnet ef migrations add Add{name} --project src/Infrastructure.Data --startup-project src/Presentation
""");
        return sb.ToString();
    }
}
