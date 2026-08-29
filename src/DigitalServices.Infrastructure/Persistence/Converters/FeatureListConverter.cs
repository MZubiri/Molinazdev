using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DigitalServices.Infrastructure.Persistence.Converters;

internal static class FeatureListConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static readonly ValueConverter<IReadOnlyList<string>, string> Converter = new(
        value => Serialize(value),
        value => Deserialize(value));

    public static readonly ValueComparer<IReadOnlyList<string>> Comparer = new(
        (left, right) => ReferenceEquals(left, right) ||
                         (left != null && right != null && left.SequenceEqual(right)),
        value => value.Aggregate(0, static (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        value => value.ToArray());

    private static string Serialize(IReadOnlyList<string> features)
    {
        return JsonSerializer.Serialize(features, JsonOptions);
    }

    private static IReadOnlyList<string> Deserialize(string json)
    {
        return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? Array.Empty<string>();
    }
}
