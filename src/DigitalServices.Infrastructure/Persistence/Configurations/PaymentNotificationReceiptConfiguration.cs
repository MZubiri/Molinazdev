using DigitalServices.Domain.Common;
using DigitalServices.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalServices.Infrastructure.Persistence.Configurations;

internal sealed class PaymentNotificationReceiptConfiguration
    : IEntityTypeConfiguration<PaymentNotificationReceipt>
{
    public void Configure(EntityTypeBuilder<PaymentNotificationReceipt> builder)
    {
        builder.ToTable("PaymentNotificationReceipts");
        builder.HasKey(receipt => receipt.Id);
        builder.Property(receipt => receipt.Id).HasColumnType("char(36)").IsRequired();
        builder.Property(receipt => receipt.Gateway)
            .HasMaxLength(DomainFieldLengths.PaymentGateway)
            .IsRequired();
        builder.Property(receipt => receipt.NotificationId)
            .HasMaxLength(DomainFieldLengths.GatewayIdentifier)
            .IsRequired();
        builder.Property(receipt => receipt.GatewayPaymentId)
            .HasMaxLength(DomainFieldLengths.GatewayIdentifier)
            .IsRequired();
        builder.Property(receipt => receipt.PaymentId).HasColumnType("char(36)").IsRequired();
        builder.Property(receipt => receipt.ReceivedAt)
            .HasConversion(UtcDateTimeOffsetConverters.Required)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasIndex(receipt => new { receipt.Gateway, receipt.NotificationId })
            .IsUnique()
            .HasDatabaseName("UX_PaymentNotificationReceipts_Gateway_NotificationId");
        builder.HasIndex(receipt => receipt.GatewayPaymentId)
            .HasDatabaseName("IX_PaymentNotificationReceipts_GatewayPaymentId");
        builder.HasIndex(receipt => receipt.PaymentId)
            .HasDatabaseName("IX_PaymentNotificationReceipts_PaymentId");

        builder.HasOne(receipt => receipt.Payment)
            .WithMany()
            .HasForeignKey(receipt => receipt.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
