using JL.DddScaffold.Mcp.Models;
using JL.DddScaffold.Mcp.Tools;

namespace JL.DddScaffold.Mcp.Templates;

internal static class ApplicationTemplates
{
    // ──────────────────────────────────────────────
    //  CREATE COMMAND
    // ──────────────────────────────────────────────

    internal static (string, string) CreateCommand(string name, string rootNs, List<PropertyDefinition> props)
    {
        var ns = $"{rootNs}.Application.Commands.Create{name}";
        var createProps = props.Where(p => p.Name != "IsActive").ToList();
        var recordParams = string.Join(",\n    ", createProps.Select(p =>
            $"{ScaffoldHelpers.TypeDecl(p)} {p.Name}"));

        var content = $$"""
using MediatR;

namespace {{ns}};

public sealed record Create{{name}}Command(
    {{recordParams}}) : IRequest<Guid>;
""";
        return ($"Application/Commands/Create{name}/Create{name}Command.cs", content);
    }

    internal static (string, string) CreateCommandHandler(string name, string rootNs, List<PropertyDefinition> props)
    {
        var ns = $"{rootNs}.Application.Commands.Create{name}";
        var portsNs = $"{rootNs}.Application.Ports";
        var aggregateNs = $"{rootNs}.Domain.Aggregates.{name}";
        var createProps = props.Where(p => p.Name != "IsActive").ToList();
        var createArgs = string.Join(",\n            ", createProps.Select(p =>
            $"cmd.{p.Name}"));

        var content = $$"""
using {{portsNs}};
using {{aggregateNs}};
using MediatR;

namespace {{ns}};

public sealed class Create{{name}}CommandHandler(I{{name}}Repository repository)
    : IRequestHandler<Create{{name}}Command, Guid>
{
    public async Task<Guid> Handle(Create{{name}}Command cmd, CancellationToken ct)
    {
        var entity = {{name}}.Create(
            {{createArgs}});

        await repository.AddAsync(entity, ct);
        return entity.Id.Value;
    }
}
""";
        return ($"Application/Commands/Create{name}/Create{name}CommandHandler.cs", content);
    }

    internal static (string, string) CreateCommandValidator(string name, string rootNs, List<PropertyDefinition> props)
    {
        var ns = $"{rootNs}.Application.Commands.Create{name}";
        var createProps = props.Where(p => p.Name != "IsActive").ToList();
        var rules = BuildValidationRules(createProps, includeId: false);

        var content = $$"""
using FluentValidation;

namespace {{ns}};

public sealed class Create{{name}}CommandValidator : AbstractValidator<Create{{name}}Command>
{
    public Create{{name}}CommandValidator()
    {
{{rules}}
    }
}
""";
        return ($"Application/Commands/Create{name}/Create{name}CommandValidator.cs", content);
    }

    // ──────────────────────────────────────────────
    //  UPDATE COMMAND
    // ──────────────────────────────────────────────

    internal static (string, string) UpdateCommand(string name, string rootNs, List<PropertyDefinition> props)
    {
        var ns = $"{rootNs}.Application.Commands.Update{name}";
        var recordParams = "Guid    Id,\n    " + string.Join(",\n    ", props.Select(p =>
            $"{ScaffoldHelpers.TypeDecl(p)} {p.Name}"));

        var content = $$"""
using MediatR;

namespace {{ns}};

public sealed record Update{{name}}Command(
    {{recordParams}}) : IRequest;
""";
        return ($"Application/Commands/Update{name}/Update{name}Command.cs", content);
    }

    internal static (string, string) UpdateCommandHandler(string name, string rootNs, List<PropertyDefinition> props)
    {
        var ns = $"{rootNs}.Application.Commands.Update{name}";
        var portsNs = $"{rootNs}.Application.Ports";
        var aggregateNs = $"{rootNs}.Domain.Aggregates.{name}";
        var exceptionsNs = $"{rootNs}.Domain.Exceptions";
        var updateArgs = string.Join(", ", props.Select(p => $"cmd.{p.Name}"));

        var content = $$"""
using {{portsNs}};
using {{aggregateNs}};
using {{exceptionsNs}};
using MediatR;

namespace {{ns}};

public sealed class Update{{name}}CommandHandler(I{{name}}Repository repository)
    : IRequestHandler<Update{{name}}Command>
{
    public async Task Handle(Update{{name}}Command cmd, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(new {{name}}Id(cmd.Id), ct)
            ?? throw new {{name}}NotFoundException(cmd.Id);

        entity.Update({{updateArgs}});

        await repository.UpdateAsync(entity, ct);
    }
}
""";
        return ($"Application/Commands/Update{name}/Update{name}CommandHandler.cs", content);
    }

    internal static (string, string) UpdateCommandValidator(string name, string rootNs, List<PropertyDefinition> props)
    {
        var ns = $"{rootNs}.Application.Commands.Update{name}";
        var rules = BuildValidationRules(props, includeId: true);

        var content = $$"""
using FluentValidation;

namespace {{ns}};

public sealed class Update{{name}}CommandValidator : AbstractValidator<Update{{name}}Command>
{
    public Update{{name}}CommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

{{rules}}
    }
}
""";
        return ($"Application/Commands/Update{name}/Update{name}CommandValidator.cs", content);
    }

    // ──────────────────────────────────────────────
    //  DELETE COMMAND
    // ──────────────────────────────────────────────

    internal static (string, string) DeleteCommand(string name, string rootNs)
    {
        var ns = $"{rootNs}.Application.Commands.Delete{name}";
        var content = $$"""
using MediatR;

namespace {{ns}};

public sealed record Delete{{name}}Command(Guid Id) : IRequest;
""";
        return ($"Application/Commands/Delete{name}/Delete{name}Command.cs", content);
    }

    internal static (string, string) DeleteCommandHandler(string name, string rootNs)
    {
        var ns = $"{rootNs}.Application.Commands.Delete{name}";
        var portsNs = $"{rootNs}.Application.Ports";
        var aggregateNs = $"{rootNs}.Domain.Aggregates.{name}";
        var exceptionsNs = $"{rootNs}.Domain.Exceptions";

        var content = $$"""
using {{portsNs}};
using {{aggregateNs}};
using {{exceptionsNs}};
using MediatR;

namespace {{ns}};

public sealed class Delete{{name}}CommandHandler(I{{name}}Repository repository)
    : IRequestHandler<Delete{{name}}Command>
{
    public async Task Handle(Delete{{name}}Command cmd, CancellationToken ct)
    {
        var id = new {{name}}Id(cmd.Id);
        _ = await repository.GetByIdAsync(id, ct)
            ?? throw new {{name}}NotFoundException(cmd.Id);

        await repository.DeleteAsync(id, ct);
    }
}
""";
        return ($"Application/Commands/Delete{name}/Delete{name}CommandHandler.cs", content);
    }

    internal static (string, string) DeleteCommandValidator(string name, string rootNs)
    {
        var ns = $"{rootNs}.Application.Commands.Delete{name}";
        var content = $$"""
using FluentValidation;

namespace {{ns}};

public sealed class Delete{{name}}CommandValidator : AbstractValidator<Delete{{name}}Command>
{
    public Delete{{name}}CommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");
    }
}
""";
        return ($"Application/Commands/Delete{name}/Delete{name}CommandValidator.cs", content);
    }

    // ──────────────────────────────────────────────
    //  QUERIES
    // ──────────────────────────────────────────────

    internal static (string, string) GetAllQuery(string name, string rootNs)
    {
        var plural = ScaffoldHelpers.Pluralize(name);
        var ns = $"{rootNs}.Application.Queries.GetAll{plural}";
        var dtosNs = $"{rootNs}.Application.DTOs";

        var content = $$"""
using {{dtosNs}};
using MediatR;

namespace {{ns}};

public sealed record GetAll{{plural}}Query : IRequest<IReadOnlyList<{{name}}Dto>>;
""";
        return ($"Application/Queries/GetAll{plural}/GetAll{plural}Query.cs", content);
    }

    internal static (string, string) GetAllQueryHandler(string name, string rootNs)
    {
        var plural = ScaffoldHelpers.Pluralize(name);
        var ns = $"{rootNs}.Application.Queries.GetAll{plural}";
        var dtosNs = $"{rootNs}.Application.DTOs";
        var portsNs = $"{rootNs}.Application.Ports";

        var content = $$"""
using AutoMapper;
using {{dtosNs}};
using {{portsNs}};
using MediatR;

namespace {{ns}};

public sealed class GetAll{{plural}}QueryHandler(I{{name}}Repository repository, IMapper mapper)
    : IRequestHandler<GetAll{{plural}}Query, IReadOnlyList<{{name}}Dto>>
{
    public async Task<IReadOnlyList<{{name}}Dto>> Handle(GetAll{{plural}}Query query, CancellationToken ct)
    {
        var entities = await repository.GetAllAsync(ct);
        return mapper.Map<IReadOnlyList<{{name}}Dto>>(entities);
    }
}
""";
        return ($"Application/Queries/GetAll{plural}/GetAll{plural}QueryHandler.cs", content);
    }

    internal static (string, string) GetByIdQuery(string name, string rootNs)
    {
        var ns = $"{rootNs}.Application.Queries.Get{name}ById";
        var dtosNs = $"{rootNs}.Application.DTOs";

        var content = $$"""
using {{dtosNs}};
using MediatR;

namespace {{ns}};

public sealed record Get{{name}}ByIdQuery(Guid Id) : IRequest<{{name}}Dto?>;
""";
        return ($"Application/Queries/Get{name}ById/Get{name}ByIdQuery.cs", content);
    }

    internal static (string, string) GetByIdQueryHandler(string name, string rootNs)
    {
        var ns = $"{rootNs}.Application.Queries.Get{name}ById";
        var dtosNs = $"{rootNs}.Application.DTOs";
        var portsNs = $"{rootNs}.Application.Ports";
        var aggregateNs = $"{rootNs}.Domain.Aggregates.{name}";

        var content = $$"""
using AutoMapper;
using {{dtosNs}};
using {{portsNs}};
using {{aggregateNs}};
using MediatR;

namespace {{ns}};

public sealed class Get{{name}}ByIdQueryHandler(I{{name}}Repository repository, IMapper mapper)
    : IRequestHandler<Get{{name}}ByIdQuery, {{name}}Dto?>
{
    public async Task<{{name}}Dto?> Handle(Get{{name}}ByIdQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(new {{name}}Id(query.Id), ct);
        return entity is null ? null : mapper.Map<{{name}}Dto>(entity);
    }
}
""";
        return ($"Application/Queries/Get{name}ById/Get{name}ByIdQueryHandler.cs", content);
    }

    // ──────────────────────────────────────────────
    //  DTO, MAPPING, PORT
    // ──────────────────────────────────────────────

    internal static (string, string) Dto(string name, string rootNs, List<PropertyDefinition> props)
    {
        var ns = $"{rootNs}.Application.DTOs";
        var dtoProps = new List<string> { "Guid      Id" };
        dtoProps.AddRange(props.Select(p => $"{ScaffoldHelpers.TypeDecl(p),9} {p.Name}"));
        dtoProps.Add("DateTime  CreatedAt");
        dtoProps.Add("DateTime? UpdatedAt");
        var recordParams = string.Join(",\n    ", dtoProps);

        var content = $$"""
namespace {{ns}};

public sealed record {{name}}Dto(
    {{recordParams}});
""";
        return ($"Application/DTOs/{name}Dto.cs", content);
    }

    internal static (string, string) MappingProfile(string name, string rootNs)
    {
        var ns = $"{rootNs}.Application.Mappings";
        var dtosNs = $"{rootNs}.Application.DTOs";
        var aggregateNs = $"{rootNs}.Domain.Aggregates.{name}";

        var content = $$"""
using AutoMapper;
using {{dtosNs}};
using {{aggregateNs}};

namespace {{ns}};

public sealed class {{name}}MappingProfile : Profile
{
    public {{name}}MappingProfile()
    {
        CreateMap<{{name}}, {{name}}Dto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.Value));
    }
}
""";
        return ($"Application/Mappings/{name}MappingProfile.cs", content);
    }

    internal static (string, string) RepositoryPort(string name, string rootNs)
    {
        var ns = $"{rootNs}.Application.Ports";
        var aggregateNs = $"{rootNs}.Domain.Aggregates.{name}";

        var content = $$"""
using {{aggregateNs}};

namespace {{ns}};

public interface I{{name}}Repository
{
    Task AddAsync({{name}} entity, CancellationToken ct = default);
    Task<{{name}}?> GetByIdAsync({{name}}Id id, CancellationToken ct = default);
    Task<IReadOnlyList<{{name}}>> GetAllAsync(CancellationToken ct = default);
    Task UpdateAsync({{name}} entity, CancellationToken ct = default);
    Task DeleteAsync({{name}}Id id, CancellationToken ct = default);
}
""";
        return ($"Application/Ports/I{name}Repository.cs", content);
    }

    // ──────────────────────────────────────────────
    //  PRIVATE HELPERS
    // ──────────────────────────────────────────────

    private static string BuildValidationRules(List<PropertyDefinition> props, bool includeId)
    {
        var lines = new List<string>();

        foreach (var p in props)
        {
            var rules = new List<string>();

            if (p.Type == "string" && p.Required && !p.IsNullable)
                rules.Add($"            .NotEmpty().WithMessage(\"{p.Name} is required.\")");

            if (p.Type == "string" && p.MaxLength.HasValue)
                rules.Add($"            .MaximumLength({p.MaxLength}).WithMessage(\"{p.Name} must not exceed {p.MaxLength} characters.\")");

            if (p.Type == "decimal" && !p.IsNullable)
                rules.Add($"            .GreaterThan(0).WithMessage(\"{p.Name} must be greater than zero.\")");

            if (p.Type == "int" && !p.IsNullable &&
                (p.Name.Contains("Quantity", StringComparison.OrdinalIgnoreCase)
                 || p.Name.Contains("Count", StringComparison.OrdinalIgnoreCase)
                 || p.Name.Contains("Amount", StringComparison.OrdinalIgnoreCase)))
                rules.Add($"            .GreaterThanOrEqualTo(0).WithMessage(\"{p.Name} must be zero or greater.\")");

            if (rules.Count > 0)
                lines.Add($"        RuleFor(x => x.{p.Name})\n" + string.Join("\n", rules) + ";");
        }

        return string.Join("\n\n", lines);
    }
}
