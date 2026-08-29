using DigitalServices.Domain.Common;
using DigitalServices.Domain.Entities;
using DigitalServices.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalServices.Infrastructure.Persistence.Configurations;

internal sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");
        builder.HasKey(service => service.Id);

        builder.Property(service => service.Id).HasColumnType("char(36)").IsRequired();
        builder.Property(service => service.Title).HasMaxLength(DomainFieldLengths.ServiceTitle).IsRequired();
        builder.Property(service => service.Slug).HasMaxLength(DomainFieldLengths.Slug).IsRequired();
        builder.Property(service => service.Description).HasMaxLength(DomainFieldLengths.Description);
        builder.Property(service => service.IconUrl).HasMaxLength(DomainFieldLengths.Url);
        builder.Property(service => service.IsActive).IsRequired();
        builder.Property(service => service.CreatedAt)
            .HasConversion(UtcDateTimeOffsetConverters.Required)
            .HasColumnType("datetime(6)")
            .IsRequired();
        builder.Property(service => service.UpdatedAt)
            .HasConversion(UtcDateTimeOffsetConverters.Required)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasIndex(service => service.Slug).IsUnique().HasDatabaseName("UX_Services_Slug");
        builder.HasIndex(service => service.IsActive).HasDatabaseName("IX_Services_IsActive");

        builder.HasMany(service => service.Packages)
            .WithOne(package => package.Service)
            .HasForeignKey(package => package.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(service => service.Packages)
            .HasField("_packages")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        Seed(builder);
    }

    private static void Seed(EntityTypeBuilder<Service> builder)
    {
        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(
            new
            {
                Id = SeedIds.WebDevelopmentService,
                Title = "Desarrollo Web",
                Slug = "desarrollo-web",
                Description = "Sitios web rápidos, accesibles y preparados para crecer con tu negocio.",
                IconUrl = (string?)null,
                IsActive = true,
                CreatedAt = seededAt,
                UpdatedAt = seededAt
            },
            new
            {
                Id = SeedIds.AiChatbotsService,
                Title = "Chatbots con IA",
                Slug = "chatbots-con-ia",
                Description = "Asistentes inteligentes conectados con los procesos y datos de tu empresa.",
                IconUrl = (string?)null,
                IsActive = true,
                CreatedAt = seededAt,
                UpdatedAt = seededAt
            },
            new
            {
                Id = SeedIds.AutomationsService,
                Title = "Automatizaciones",
                Slug = "automatizaciones",
                Description = "Flujos automatizados que reducen tareas manuales y errores operativos.",
                IconUrl = (string?)null,
                IsActive = true,
                CreatedAt = seededAt,
                UpdatedAt = seededAt
            },
            new
            {
                Id = SeedIds.CustomSoftwareService,
                Title = "Software a Medida",
                Slug = "software-a-medida",
                Description = "Soluciones digitales personalizadas para optimizar la operación y escala de tu negocio.",
                IconUrl = (string?)null,
                IsActive = true,
                CreatedAt = seededAt,
                UpdatedAt = seededAt
            });
    }
}
