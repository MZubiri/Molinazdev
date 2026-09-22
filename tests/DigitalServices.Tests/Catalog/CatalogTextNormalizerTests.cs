using DigitalServices.Application.Catalog;

namespace DigitalServices.Tests.Catalog;

public sealed class CatalogTextNormalizerTests
{
    [Theory]
    [InlineData("DiseÃ±o responsive para mÃ³viles", "Diseño responsive para móviles")]
    [InlineData("AutomatizaciÃ³n BÃ¡sica", "Automatización Básica")]
    [InlineData("Soporte tÃ©cnico hasta 60 dÃ­as", "Soporte técnico hasta 60 días")]
    public void RepairMojibake_RepairsDoubleEncodedUtf8(string corrupted, string expected)
    {
        Assert.Equal(expected, CatalogTextNormalizer.RepairMojibake(corrupted));
    }

    [Theory]
    [InlineData("Diseño responsive para móviles")]
    [InlineData("Métricas esenciales")]
    [InlineData(null)]
    public void RepairMojibake_PreservesCorrectText(string? value)
    {
        Assert.Equal(value, CatalogTextNormalizer.RepairMojibake(value));
    }
}
