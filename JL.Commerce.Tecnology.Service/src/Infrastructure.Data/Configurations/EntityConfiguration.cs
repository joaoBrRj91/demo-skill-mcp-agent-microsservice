using JL.Commerce.Tecnology.Service.Domain.Aggregates.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Data.Configurations;

public sealed class EntityConfiguration : IEntityTypeConfiguration<Entity>
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
        builder.ToTable("Entities");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new EntityId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Ignore(e => e.DomainEvents);
    }
}
