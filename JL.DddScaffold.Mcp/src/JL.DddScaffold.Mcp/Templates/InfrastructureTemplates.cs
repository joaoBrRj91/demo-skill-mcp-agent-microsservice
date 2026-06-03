using JL.DddScaffold.Mcp.Models;
using JL.DddScaffold.Mcp.Tools;

namespace JL.DddScaffold.Mcp.Templates;

internal static class InfrastructureTemplates
{
    /// <summary>Infrastructure.Data/Configurations/{Name}Configuration.cs</summary>
    internal static (string, string) EfConfiguration(string name, string rootNs, List<PropertyDefinition> props)
    {
        var ns = $"{rootNs}.Infrastructure.Data.Configurations";
        var aggregateNs = $"{rootNs}.Domain.Aggregates.{name}";
        var plural = ScaffoldHelpers.Pluralize(name);

        var propConfigs = new List<string>();
        foreach (var p in props)
        {
            var lines = new List<string> { $"        builder.Property(p => p.{p.Name})" };

            if (p.Required && !p.IsNullable)
                lines.Add("            .IsRequired()");

            if (p.Type == "string" && p.MaxLength.HasValue)
                lines.Add($"            .HasMaxLength({p.MaxLength})");

            if (p.Type == "decimal" && p.Precision.HasValue)
                lines.Add($"            .HasPrecision({p.Precision}, {p.Scale})");

            propConfigs.Add(string.Join("\n", lines) + ";");

            if (p.IsUnique)
                propConfigs.Add($"        builder.HasIndex(p => p.{p.Name})\n            .IsUnique();");
        }

        // Always add UpdatedAt bare property
        propConfigs.Add("        builder.Property(p => p.UpdatedAt);");

        var propConfigBlock = string.Join("\n\n", propConfigs);

        var content = $$"""
using {{aggregateNs}};
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace {{ns}};

public sealed class {{name}}Configuration : IEntityTypeConfiguration<{{name}}>
{
    public void Configure(EntityTypeBuilder<{{name}}> builder)
    {
        builder.ToTable("{{plural}}");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new {{name}}Id(value))
            .ValueGeneratedNever();

{{propConfigBlock}}

        builder.Ignore(p => p.DomainEvents);
    }
}
""";
        return ($"Infrastructure.Data/Configurations/{name}Configuration.cs", content);
    }

    /// <summary>Infrastructure.Data/Repositories/{Name}Repository.cs</summary>
    internal static (string, string) Repository(string name, string rootNs)
    {
        var ns = $"{rootNs}.Infrastructure.Data.Repositories";
        var portsNs = $"{rootNs}.Application.Ports";
        var aggregateNs = $"{rootNs}.Domain.Aggregates.{name}";
        var contextNs = $"{rootNs}.Infrastructure.Data.Context";
        var plural = ScaffoldHelpers.Pluralize(name);

        var content = $$"""
using {{portsNs}};
using {{aggregateNs}};
using {{contextNs}};
using Microsoft.EntityFrameworkCore;

namespace {{ns}};

public sealed class {{name}}Repository(AppDbContext dbContext) : I{{name}}Repository
{
    public async Task AddAsync({{name}} entity, CancellationToken ct = default)
    {
        await dbContext.{{plural}}.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<{{name}}?> GetByIdAsync({{name}}Id id, CancellationToken ct = default) =>
        await dbContext.{{plural}}.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<{{name}}>> GetAllAsync(CancellationToken ct = default) =>
        await dbContext.{{plural}}.ToListAsync(ct);

    public async Task UpdateAsync({{name}} entity, CancellationToken ct = default)
    {
        dbContext.{{plural}}.Update(entity);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync({{name}}Id id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity is not null)
        {
            dbContext.{{plural}}.Remove(entity);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
""";
        return ($"Infrastructure.Data/Repositories/{name}Repository.cs", content);
    }
}
