using DigitalServices.Domain.Common;
using DigitalServices.Domain.Entities;
using DigitalServices.Domain.Enums;
using DigitalServices.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalServices.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
            table.HasCheckConstraint("CK_Orders_TotalAmount_Positive", "`TotalAmount` > 0"));

        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).HasColumnType("char(36)").IsRequired();
        builder.Property(order => order.OrderNumber).HasMaxLength(DomainFieldLengths.OrderNumber).IsRequired();
        builder.Property(order => order.ClientId).HasColumnType("char(36)").IsRequired();
        builder.Property(order => order.PackageId).HasColumnType("char(36)").IsRequired();
        builder.Property(order => order.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(order => order.Currency)
            .HasColumnType("char(3)")
            .HasMaxLength(DomainFieldLengths.Currency)
            .IsRequired();
        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(order => order.ExternalReference)
            .HasMaxLength(DomainFieldLengths.ExternalReference)
            .IsRequired();
        builder.Property(order => order.CreatedAt)
            .HasConversion(UtcDateTimeOffsetConverters.Required)
            .HasColumnType("datetime(6)")
            .IsRequired();
        builder.Property(order => order.UpdatedAt)
            .HasConversion(UtcDateTimeOffsetConverters.Required)
            .HasColumnType("datetime(6)")
            .IsRequired();
        builder.Property(order => order.PaidAt)
            .HasConversion(UtcDateTimeOffsetConverters.Optional)
            .HasColumnType("datetime(6)");

        builder.HasIndex(order => order.OrderNumber).IsUnique().HasDatabaseName("UX_Orders_OrderNumber");
        builder.HasIndex(order => order.ExternalReference).IsUnique().HasDatabaseName("UX_Orders_ExternalReference");
        builder.HasIndex(order => order.ClientId).HasDatabaseName("IX_Orders_ClientId");
        builder.HasIndex(order => order.PackageId).HasDatabaseName("IX_Orders_PackageId");
        builder.HasIndex(order => order.Status).HasDatabaseName("IX_Orders_Status");

        builder.HasMany(order => order.Payments)
            .WithOne(payment => payment.Order)
            .HasForeignKey(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(order => order.Payments)
            .HasField("_payments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
