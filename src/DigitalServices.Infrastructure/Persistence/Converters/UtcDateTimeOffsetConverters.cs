using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DigitalServices.Infrastructure.Persistence.Converters;

internal static class UtcDateTimeOffsetConverters
{
    public static readonly ValueConverter<DateTimeOffset, DateTime> Required = new(
        value => value.UtcDateTime,
        value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

    public static readonly ValueConverter<DateTimeOffset?, DateTime?> Optional = new(
        value => value.HasValue ? value.Value.UtcDateTime : null,
        value => value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : null);
}
