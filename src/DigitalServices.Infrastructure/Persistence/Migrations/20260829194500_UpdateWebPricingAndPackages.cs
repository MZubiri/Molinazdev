using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWebPricingAndPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "Description", "Features", "Price" },
                values: new object[] { "Página de alto impacto para captar clientes o vender un servicio puntual.", "[\"Diseño responsive para móviles\",\"Formulario de contacto y WhatsApp\",\"Dominio y 1er año de hosting incluido\",\"Soporte técnico hasta 60 días\",\"SEO técnico y velocidad\"]", 799m });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                columns: new[] { "Name", "Description", "Features", "Price" },
                values: new object[] { "Web Autoadministrable", "Sitio web completo con panel fácil para editar textos, fotos y secciones.", "[\"Panel autoadministrable intuitivo\",\"Hasta 8 secciones o páginas\",\"Dominio y 1er año de hosting incluido\",\"Soporte técnico hasta 60 días\",\"Formularios y WhatsApp integrados\"]", 1800m });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                columns: new[] { "Description", "Features", "Price" },
                values: new object[] { "Sistema o plataforma digital a medida según los requerimientos de tu negocio.", "[\"Relevamiento técnico y arquitectura\",\"Panel de usuarios y base de datos\",\"Dominio y 1er año de hosting incluido\",\"Soporte técnico hasta 60 días\",\"Integraciones y pasarelas acordadas\"]", 4000m });

            migrationBuilder.InsertData(
                table: "ServicePackages",
                columns: new[] { "Id", "CreatedAt", "Currency", "DeliveryDays", "Description", "Features", "IsActive", "Name", "Price", "ServiceId", "UpdatedAt" },
                values: new object[] { new Guid("20000000-0000-0000-0000-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PEN", 25, "Tienda online completa con pasarela de pagos integrada para vender por internet.", "[\"Catálogo de productos y carrito\",\"Pasarela de pagos integrada (Tarjetas, Yape, Plin)\",\"Dominio y 1er año de hosting incluido\",\"Soporte técnico hasta 60 días\",\"Panel de pedidos y stock\"]", true, "Ecommerce", 2400m, new Guid("10000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000010"));

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "Description", "Features", "Price" },
                values: new object[] { "Página enfocada en conversión para una campaña o servicio.", "[\"Diseño responsive\",\"Formulario de contacto\",\"SEO técnico inicial\"]", 1200m });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                columns: new[] { "Name", "Description", "Features", "Price" },
                values: new object[] { "Sitio Corporativo", "Presencia web completa para presentar empresa, servicios y casos de éxito.", "[\"Hasta 8 secciones\",\"Panel de contenidos\",\"Analítica básica\"]", 2800m });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                columns: new[] { "Description", "Features", "Price" },
                values: new object[] { "Base a medida para procesos digitales con alcance definido en descubrimiento.", "[\"Descubrimiento técnico\",\"Arquitectura escalable\",\"Despliegue inicial\"]", 6500m });
        }
    }
}
