using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DigitalServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomSoftwareService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "Id", "CreatedAt", "Description", "IconUrl", "IsActive", "Slug", "Title", "UpdatedAt" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Soluciones digitales personalizadas para optimizar la operación y escala de tu negocio.", null, true, "software-a-medida", "Software a Medida", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "ServicePackages",
                columns: new[] { "Id", "CreatedAt", "Currency", "DeliveryDays", "Description", "Features", "IsActive", "Name", "Price", "ServiceId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PEN", 35, "Módulo o herramienta interna con alcance y requerimientos definidos.", "[\"Relevamiento de requisitos\",\"Arquitectura escalable\",\"Pruebas y despliegue\"]", true, "Software Estándar", 5500m, new Guid("10000000-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000009"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PEN", 60, "Plataforma integral con múltiples módulos, integraciones y soporte.", "[\"Arquitectura empresarial\",\"Integraciones a medida\",\"Monitoreo y soporte\"]", true, "Software Empresarial", 12000m, new Guid("10000000-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"));
        }
    }
}
