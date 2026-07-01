using JL.Commerce.Tecnology.Service.Domain.Aggregates.Order;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JL.Commerce.Tecnology.Service.Infrastructure.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<OrderAuditLog>
{
    public void Configure(EntityTypeBuilder<OrderAuditLog> builder)
    {
        builder.ToTable("OrderAuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.OrderId).IsRequired();
        builder.Property(a => a.TransactionId).IsRequired();
        builder.Property(a => a.FromState).IsRequired().HasMaxLength(50);
        builder.Property(a => a.ToState).IsRequired().HasMaxLength(50);
        builder.Property(a => a.OccurredAtUtc).IsRequired();
        builder.Property(a => a.TriggeredByEvent).IsRequired().HasMaxLength(200);
    }
}
