using JL.Commerce.Tecnology.Service.Domain.Aggregates.CatalogProduct;
using JL.Commerce.Tecnology.Service.Domain.Aggregates.Entity;
using Microsoft.EntityFrameworkCore;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Data.Context;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<CatalogProduct> CatalogProducts => Set<CatalogProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
