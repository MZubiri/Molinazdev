using DigitalServices.Domain.Common;
using DigitalServices.Domain.Entities;
using DigitalServices.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalServices.Infrastructure.Persistence.Configurations;

internal sealed class ServicePackageConfiguration : IEntityTypeConfiguration<ServicePackage>
{
    public void Configure(EntityTypeBuilder<ServicePackage> builder)
    {
        builder.ToTable("ServicePackages", table =>
        {
            table.HasCheckConstraint("CK_ServicePackages_Price_Positive", "`Price` > 0");
            table.HasCheckConstraint("CK_ServicePackages_DeliveryDays_Positive", "`DeliveryDays` > 0");
        });

        builder.HasKey(package => package.Id);
        builder.Property(package => package.Id).HasColumnType("char(36)").IsRequired();
        builder.Property(package => package.ServiceId).HasColumnType("char(36)").IsRequired();
        builder.Property(package => package.Name).HasMaxLength(DomainFieldLengths.PackageName).IsRequired();
        builder.Property(package => package.Description).HasMaxLength(DomainFieldLengths.Description);
        builder.Property(package => package.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(package => package.Currency)
            .HasColumnType("char(3)")
            .HasMaxLength(DomainFieldLengths.Currency)
            .IsRequired();
        builder.Property(package => package.DeliveryDays).IsRequired();

        var featuresProperty = builder.Property(package => package.Features)
            .HasConversion(FeatureListConverter.Converter)
            .HasColumnType("json")
            .IsRequired();
        featuresProperty.Metadata.SetValueComparer(FeatureListConverter.Comparer);

        builder.Property(package => package.IsActive).IsRequired();
        builder.Property(package => package.CreatedAt)
            .HasConversion(UtcDateTimeOffsetConverters.Required)
            .HasColumnType("datetime(6)")
            .IsRequired();
        builder.Property(package => package.UpdatedAt)
            .HasConversion(UtcDateTimeOffsetConverters.Required)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasIndex(package => package.ServiceId).HasDatabaseName("IX_ServicePackages_ServiceId");
        builder.HasIndex(package => package.IsActive).HasDatabaseName("IX_ServicePackages_IsActive");
        builder.HasIndex(package => new { package.ServiceId, package.IsActive })
            .HasDatabaseName("IX_ServicePackages_ServiceId_IsActive");

        builder.HasMany<Order>()
            .WithOne(order => order.Package)
            .HasForeignKey(order => order.PackageId)
            .OnDelete(DeleteBehavior.Restrict);

        Seed(builder);
    }

    private static void Seed(EntityTypeBuilder<ServicePackage> builder)
    {
        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(
            Package(SeedIds.LandingPagePackage, SeedIds.WebDevelopmentService, "Landing Page",
                "Página enfocada en conversión para una campaña o servicio.", 1200m, 10,
                ["Diseño responsive", "Formulario de contacto", "SEO técnico inicial"], seededAt),
            Package(SeedIds.CorporateSitePackage, SeedIds.WebDevelopmentService, "Sitio Corporativo",
                "Presencia web completa para presentar empresa, servicios y casos de éxito.", 2800m, 20,
                ["Hasta 8 secciones", "Panel de contenidos", "Analítica básica"], seededAt),
            Package(SeedIds.CustomPlatformPackage, SeedIds.WebDevelopmentService, "Plataforma Personalizada",
                "Base a medida para procesos digitales con alcance definido en descubrimiento.", 6500m, 45,
                ["Descubrimiento técnico", "Arquitectura escalable", "Despliegue inicial"], seededAt),
            Package(SeedIds.BasicChatbotPackage, SeedIds.AiChatbotsService, "Chatbot Básico",
                "Asistente para preguntas frecuentes entrenado con contenido del negocio.", 1800m, 14,
                ["Base de conocimiento", "Widget web", "Métricas esenciales"], seededAt),
            Package(SeedIds.EnterpriseChatbotPackage, SeedIds.AiChatbotsService, "Chatbot Empresarial",
                "Asistente conectado con fuentes y flujos internos seleccionados.", 4800m, 30,
                ["Integraciones acordadas", "Escalamiento a humano", "Monitoreo"], seededAt),
            Package(SeedIds.BasicAutomationPackage, SeedIds.AutomationsService, "Automatización Básica",
                "Automatización de un flujo repetitivo claramente delimitado.", 1400m, 12,
                ["Un flujo", "Registro de ejecución", "Documentación operativa"], seededAt),
            Package(SeedIds.CustomAutomationPackage, SeedIds.AutomationsService, "Automatización Personalizada",
                "Diseño e implementación de varios pasos e integraciones acordadas.", 3900m, 25,
                ["Análisis del proceso", "Integraciones personalizadas", "Alertas de fallos"], seededAt),
            Package(SeedIds.StandardSoftwarePackage, SeedIds.CustomSoftwareService, "Software Estándar",
                "Módulo o herramienta interna con alcance y requerimientos definidos.", 5500m, 35,
                ["Relevamiento de requisitos", "Arquitectura escalable", "Pruebas y despliegue"], seededAt),
            Package(SeedIds.EnterpriseSoftwarePackage, SeedIds.CustomSoftwareService, "Software Empresarial",
                "Plataforma integral con múltiples módulos, integraciones y soporte.", 12000m, 60,
                ["Arquitectura empresarial", "Integraciones a medida", "Monitoreo y soporte"], seededAt));
    }

    private static object Package(
        Guid id,
        Guid serviceId,
        string name,
        string description,
        decimal price,
        int deliveryDays,
        IReadOnlyList<string> features,
        DateTimeOffset seededAt)
    {
        return new
        {
            Id = id,
            ServiceId = serviceId,
            Name = name,
            Description = (string?)description,
            Price = price,
            Currency = "PEN",
            DeliveryDays = deliveryDays,
            Features = features,
            IsActive = true,
            CreatedAt = seededAt,
            UpdatedAt = seededAt
        };
    }
}
