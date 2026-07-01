using JL.Commerce.Tecnology.Service.Domain.Aggregates.CatalogProduct;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Entity;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Data.Context;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<CatalogProduct> CatalogProducts => Set<CatalogProduct>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderAuditLog> OrderAuditLogs => Set<OrderAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
