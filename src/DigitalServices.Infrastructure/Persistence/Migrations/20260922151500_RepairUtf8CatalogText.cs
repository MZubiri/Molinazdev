using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalServices.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260922151500_RepairUtf8CatalogText")]
public sealed class RepairUtf8CatalogText : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE `Services`
            SET
                `Title` = CASE
                    WHEN `Title` LIKE '%Ã%' OR `Title` LIKE '%Â%'
                    THEN CONVERT(CAST(CONVERT(`Title` USING latin1) AS BINARY) USING utf8mb4)
                    ELSE `Title`
                END,
                `Description` = CASE
                    WHEN `Description` LIKE '%Ã%' OR `Description` LIKE '%Â%'
                    THEN CONVERT(CAST(CONVERT(`Description` USING latin1) AS BINARY) USING utf8mb4)
                    ELSE `Description`
                END;
            """);

        migrationBuilder.Sql(
            """
            UPDATE `ServicePackages`
            SET
                `Name` = CASE
                    WHEN `Name` LIKE '%Ã%' OR `Name` LIKE '%Â%'
                    THEN CONVERT(CAST(CONVERT(`Name` USING latin1) AS BINARY) USING utf8mb4)
                    ELSE `Name`
                END,
                `Description` = CASE
                    WHEN `Description` LIKE '%Ã%' OR `Description` LIKE '%Â%'
                    THEN CONVERT(CAST(CONVERT(`Description` USING latin1) AS BINARY) USING utf8mb4)
                    ELSE `Description`
                END,
                `Features` = CASE
                    WHEN CAST(`Features` AS CHAR CHARACTER SET utf8mb4) LIKE '%Ã%'
                      OR CAST(`Features` AS CHAR CHARACTER SET utf8mb4) LIKE '%Â%'
                    THEN CONVERT(
                        CAST(
                            CONVERT(
                                CAST(`Features` AS CHAR CHARACTER SET utf8mb4)
                                USING latin1
                            ) AS BINARY
                        ) USING utf8mb4
                    )
                    ELSE `Features`
                END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This migration repairs corrupted text and is intentionally irreversible.
    }
}
