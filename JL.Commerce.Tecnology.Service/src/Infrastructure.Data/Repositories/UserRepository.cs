using JL.Commerce.Tecnology.Service.Application.Ports;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.User;
using JL.Commerce.Tecnology.Service.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Data.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await dbContext.Users.AddAsync(user, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) =>
        await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) =>
        await dbContext.Users.ToListAsync(ct);

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(UserId id, CancellationToken ct = default)
    {
        var user = await GetByIdAsync(id, ct);
        if (user is not null)
        {
            dbContext.Users.Remove(user);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
