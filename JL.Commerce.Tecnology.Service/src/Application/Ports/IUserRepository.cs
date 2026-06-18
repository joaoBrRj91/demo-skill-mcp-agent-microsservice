using JL.Commerce.Tecnology.Service.Domain.Aggregates.User;

namespace JL.Commerce.Tecnology.Service.Application.Ports;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken ct = default);
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task DeleteAsync(UserId id, CancellationToken ct = default);
}
