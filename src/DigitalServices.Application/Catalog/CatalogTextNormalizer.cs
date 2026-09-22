using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DigitalServices.Application.Catalog;

internal static class CatalogTextNormalizer
{
    [return: NotNullIfNotNull(nameof(value))]
    public static string? RepairMojibake(string? value)
    {
        if (string.IsNullOrEmpty(value) ||
            (!value.Contains('Ã') && !value.Contains('Â')))
        {
            return value;
        }

        var repaired = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(value));
        return repaired.Contains('\uFFFD') ? value : repaired;
    }
}
