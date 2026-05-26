using JL.Commerce.Tecnology.Service.Domain.Aggregates.CatalogProduct;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Data.Configurations;

public sealed class CatalogProductConfiguration : IEntityTypeConfiguration<CatalogProduct>
{
    public void Configure(EntityTypeBuilder<CatalogProduct> builder)
    {
        builder.ToTable("CatalogProducts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new CatalogProductId(value))
            .ValueGeneratedNever();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.Sku)
            .IsUnique();

        builder.Property(p => p.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.StockQuantity)
            .IsRequired();

        builder.Property(p => p.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.IsActive)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);

        builder.Ignore(p => p.DomainEvents);
    }
}
