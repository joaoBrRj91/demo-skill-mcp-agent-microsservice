using JL.DddScaffold.Mcp.Models;
using JL.DddScaffold.Mcp.Tools;

namespace JL.DddScaffold.Mcp.Templates;

internal static class DomainTemplates
{
    /// <summary>Domain/Aggregates/{Name}/{Name}.cs</summary>
    internal static (string relPath, string content) AggregateClass(
        string name, string rootNs, List<PropertyDefinition> props)
    {
        var ns = $"{rootNs}.Domain.Aggregates.{name}";
        var eventsNs = $"{rootNs}.Domain.Events";

        var propDecls = string.Join("\n", props.Select(p =>
            $"    public {ScaffoldHelpers.TypeDecl(p)} {p.Name} {{ get; private set; }}{ScaffoldHelpers.DefaultInit(p)}"));
        if (!string.IsNullOrEmpty(propDecls)) propDecls += "\n";

        var createProps = props.Where(p => p.Name != "IsActive").ToList();
        var createParams = string.Join(
            ",\n        ",
            createProps.Select(p => $"{ScaffoldHelpers.TypeDecl(p)} {ScaffoldHelpers.ToCamelCase(p.Name)}"));
        var createAssignments = string.Join(
            "\n            ",
            createProps.Select(p => $"{p.Name} = {ScaffoldHelpers.ToCamelCase(p.Name)},"));

        var hasIsActive = props.Any(p => p.Name == "IsActive");
        var isActiveLine = hasIsActive ? "\n            IsActive  = true," : "";

        var updateParams = string.Join(
            ",\n        ",
            props.Select(p => $"{ScaffoldHelpers.TypeDecl(p)} {ScaffoldHelpers.ToCamelCase(p.Name)}"));
        var updateAssignments = string.Join(
            "\n        ",
            props.Select(p => $"{p.Name} = {ScaffoldHelpers.ToCamelCase(p.Name)};"));

        var hasUpdatedAt = props.Any(p => p.Name == "UpdatedAt");
        var updatedAtLine = hasUpdatedAt ? "\n        UpdatedAt = DateTime.UtcNow;" : "";

        var content = $$"""
using {{eventsNs}};
using {{rootNs}}.Domain.Abstractions;

namespace {{ns}};

public sealed class {{name}} : AggregateRoot<{{name}}Id>
{
{{propDecls}}    public DateTime  CreatedAt  { get; private set; }
    public DateTime? UpdatedAt  { get; private set; }

    private {{name}}() { }  // EF Core constructor

    public static {{name}} Create(
        {{createParams}})
    {
        var entity = new {{name}}
        {
            Id            = {{name}}Id.New(),
            {{createAssignments}}{{isActiveLine}}
            CreatedAt     = DateTime.UtcNow
        };
        entity.AddDomainEvent(new {{name}}CreatedEvent(entity.Id));
        return entity;
    }

    public void Update(
        {{updateParams}})
    {
        {{updateAssignments}}{{updatedAtLine}}
    }
}
""";

        return ($"Domain/Aggregates/{name}/{name}.cs", content);
    }

    /// <summary>Domain/Aggregates/{Name}/{Name}Id.cs</summary>
    internal static (string relPath, string content) AggregateId(string name, string rootNs)
    {
        var ns = $"{rootNs}.Domain.Aggregates.{name}";
        var content = $$"""
namespace {{ns}};

public sealed record {{name}}Id(Guid Value)
{
    public static {{name}}Id New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
""";
        return ($"Domain/Aggregates/{name}/{name}Id.cs", content);
    }

    /// <summary>Domain/Events/{Name}CreatedEvent.cs</summary>
    internal static (string relPath, string content) CreatedEvent(string name, string rootNs)
    {
        var ns = $"{rootNs}.Domain.Events";
        var aggregateNs = $"{rootNs}.Domain.Aggregates.{name}";
        var content = $$"""
using {{aggregateNs}};
using {{rootNs}}.Domain.Abstractions;

namespace {{ns}};

public sealed record {{name}}CreatedEvent({{name}}Id EntityId) : IDomainEvent;
""";
        return ($"Domain/Events/{name}CreatedEvent.cs", content);
    }

    /// <summary>Domain/Exceptions/{Name}NotFoundException.cs</summary>
    internal static (string relPath, string content) NotFoundException(string name, string rootNs)
    {
        var ns = $"{rootNs}.Domain.Exceptions";
        var content = $$"""
namespace {{ns}};

public sealed class {{name}}NotFoundException(Guid id)
    : Exception($"{{name}} with id '{id}' was not found.");
""";
        return ($"Domain/Exceptions/{name}NotFoundException.cs", content);
    }
}
