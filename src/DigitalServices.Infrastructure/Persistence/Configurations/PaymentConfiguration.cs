using DigitalServices.Domain.Common;
using DigitalServices.Domain.Entities;
using DigitalServices.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalServices.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", table =>
            table.HasCheckConstraint("CK_Payments_Amount_Positive", "`Amount` > 0"));

        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).HasColumnType("char(36)").IsRequired();
        builder.Property(payment => payment.OrderId).HasColumnType("char(36)").IsRequired();
        builder.Property(payment => payment.Gateway)
            .HasMaxLength(DomainFieldLengths.PaymentGateway)
            .IsRequired();
        builder.Property(payment => payment.GatewayPaymentId)
            .HasMaxLength(DomainFieldLengths.GatewayIdentifier);
        builder.Property(payment => payment.PreferenceId)
            .HasMaxLength(DomainFieldLengths.GatewayIdentifier);
        builder.Property(payment => payment.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(payment => payment.Currency)
            .HasColumnType("char(3)")
            .HasMaxLength(DomainFieldLengths.Currency)
            .IsRequired();
        builder.Property(payment => payment.Status)
            .HasMaxLength(DomainFieldLengths.PaymentStatus)
            .IsRequired();
        builder.Property(payment => payment.StatusDetail)
            .HasMaxLength(DomainFieldLengths.PaymentStatusDetail);
        builder.Property(payment => payment.NotificationId)
            .HasMaxLength(DomainFieldLengths.GatewayIdentifier);
        builder.Property(payment => payment.RawPayload).HasColumnType("longtext");
        builder.Property(payment => payment.CreatedAt)
            .HasConversion(UtcDateTimeOffsetConverters.Required)
            .HasColumnType("datetime(6)")
            .IsRequired();
        builder.Property(payment => payment.UpdatedAt)
            .HasConversion(UtcDateTimeOffsetConverters.Required)
            .HasColumnType("datetime(6)")
            .IsRequired();
        builder.Property(payment => payment.ProcessedAt)
            .HasConversion(UtcDateTimeOffsetConverters.Optional)
            .HasColumnType("datetime(6)");

        builder.HasIndex(payment => payment.OrderId).HasDatabaseName("IX_Payments_OrderId");
        builder.HasIndex(payment => payment.PreferenceId).HasDatabaseName("IX_Payments_PreferenceId");
        builder.HasIndex(payment => new { payment.Gateway, payment.GatewayPaymentId })
            .IsUnique()
            .HasDatabaseName("UX_Payments_Gateway_GatewayPaymentId");
        builder.HasIndex(payment => new { payment.Gateway, payment.NotificationId })
            .IsUnique()
            .HasDatabaseName("UX_Payments_Gateway_NotificationId");
    }
}
