using DigitalServices.Domain.Common;
using DigitalServices.Domain.Entities;
using DigitalServices.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalServices.Infrastructure.Persistence.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");
        builder.HasKey(client => client.Id);
        builder.Property(client => client.Id).HasColumnType("char(36)").IsRequired();
        builder.Property(client => client.FullName).HasMaxLength(DomainFieldLengths.FullName).IsRequired();
        builder.Property(client => client.Email).HasMaxLength(DomainFieldLengths.Email).IsRequired();
        builder.Property(client => client.PhoneNumber).HasMaxLength(DomainFieldLengths.PhoneNumber);
        builder.Property(client => client.CompanyName).HasMaxLength(DomainFieldLengths.CompanyName);
        builder.Property(client => client.CreatedAt)
            .HasConversion(UtcDateTimeOffsetConverters.Required)
            .HasColumnType("datetime(6)")
            .IsRequired();
        builder.Property(client => client.UpdatedAt)
            .HasConversion(UtcDateTimeOffsetConverters.Required)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasIndex(client => client.Email).IsUnique().HasDatabaseName("UX_Clients_Email");

        builder.HasMany(client => client.Orders)
            .WithOne(order => order.Client)
            .HasForeignKey(order => order.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(client => client.Orders)
            .HasField("_orders")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
