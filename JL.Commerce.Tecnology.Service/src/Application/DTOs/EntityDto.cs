namespace JL.Commerce.Tecnology.Service.Application.DTOs;

public sealed record EntityDto(
    Guid     Id,
    string   Name,
    string   Description,
    DateTime CreatedAt);
