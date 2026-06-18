namespace JL.Commerce.Tecnology.Service.Application.DTOs;

public sealed record UserDto(
    Guid      Id,
    string    Name,
    string    Email,
    bool      IsActive,
    DateTime  CreatedAt,
    DateTime? UpdatedAt);
