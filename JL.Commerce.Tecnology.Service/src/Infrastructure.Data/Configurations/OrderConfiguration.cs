using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => new OrderId(value))
            .ValueGeneratedNever();

        builder.Property(o => o.UserId).IsRequired();

        builder.Property(o => o.TransactionId).IsRequired();
        builder.HasIndex(o => o.TransactionId).IsUnique();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(o => o.ErrorMessage);

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt);
        builder.Property(o => o.DeletedAt);

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.OwnsMany(o => o.Items, b =>
        {
            b.ToTable("OrderItems");
            b.WithOwner().HasForeignKey("OrderId");
            b.Property(i => i.CatalogProductId).IsRequired();
            b.Property(i => i.Quantity).IsRequired();
            b.Property(i => i.UnitPrice).IsRequired().HasPrecision(18, 2);
        });

        builder.OwnsOne(o => o.Payment, b =>
        {
            b.Property(p => p.Method).IsRequired().HasConversion<string>();
            b.Property(p => p.CardNumber).HasMaxLength(19);
            b.Property(p => p.HolderName).HasMaxLength(200);
            b.Property(p => p.Expiry).HasMaxLength(5);
            b.Property(p => p.Cvv).HasMaxLength(4);
            b.Property(p => p.PixKey).HasMaxLength(200);
        });

        builder.OwnsOne(o => o.Address, b =>
        {
            b.Property(a => a.Street).IsRequired().HasMaxLength(500);
            b.Property(a => a.City).IsRequired().HasMaxLength(200);
            b.Property(a => a.State).IsRequired().HasMaxLength(100);
            b.Property(a => a.ZipCode).IsRequired().HasMaxLength(20);
            b.Property(a => a.Country).IsRequired().HasMaxLength(100);
        });

        builder.Ignore(o => o.DomainEvents);
    }
}
