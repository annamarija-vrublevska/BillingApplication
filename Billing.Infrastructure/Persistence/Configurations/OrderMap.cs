using Billing.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Infrastructure.Persistence.Configurations;

public sealed class OrderMap : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.OrderNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(order => order.OrderNumber)
            .IsUnique();

        builder.Property(order => order.UserId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(order => order.Amount)
            .HasPrecision(18, 2);

        builder.Property(order => order.Description)
            .HasMaxLength(500);

        builder.Property(order => order.PaymentGateway)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(order => order.ConfirmationNumber)
            .HasMaxLength(64);

        builder.Property(order => order.CreatedAt)
            .IsRequired();

        builder.Property(order => order.ProcessedAt);

        builder.Property(order => order.FailureReason)
            .HasMaxLength(500);

        builder.HasIndex(order => order.Status);
        builder.HasIndex(order => order.CreatedAt);
    }
}
