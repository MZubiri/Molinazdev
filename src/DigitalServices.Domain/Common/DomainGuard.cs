using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace DigitalServices.Domain.Common;

internal static partial class DomainGuard
{
    public static string Required(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"The value cannot exceed {maxLength.ToString(CultureInfo.InvariantCulture)} characters.");
        }

        return normalized;
    }

    public static string? Optional(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Required(value, maxLength, parameterName);
    }

    public static string NormalizeEmail(string? value, string parameterName)
    {
        var email = Required(value, DomainFieldLengths.Email, parameterName).ToLowerInvariant();

        try
        {
            var parsed = new MailAddress(email);
            if (!string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("The email address is not valid.", parameterName, exception);
        }

        return email;
    }

    public static string NormalizeCurrency(string? value, string parameterName)
    {
        var currency = Required(value, DomainFieldLengths.Currency, parameterName).ToUpperInvariant();
        if (!CurrencyCodeRegex().IsMatch(currency))
        {
            throw new ArgumentException("Currency must be a three-letter ISO code.", parameterName);
        }

        return currency;
    }

    public static string NormalizeSlug(string? value, string parameterName)
    {
        var slug = Required(value, DomainFieldLengths.Slug, parameterName).ToLowerInvariant();
        if (!SlugRegex().IsMatch(slug))
        {
            throw new ArgumentException(
                "Slug may contain lowercase letters, numbers, and single hyphens between words.",
                parameterName);
        }

        return slug;
    }

    public static decimal PositiveAmount(decimal value, string parameterName)
    {
        if (value <= 0m)
        {
            throw new ArgumentOutOfRangeException(parameterName, "The amount must be greater than zero.");
        }

        if (decimal.Round(value, 2, MidpointRounding.ToEven) != value)
        {
            throw new ArgumentOutOfRangeException(parameterName, "The amount cannot have more than two decimal places.");
        }

        return value;
    }

    public static Guid RequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("The identifier is required.", parameterName);
        }

        return value;
    }

    public static DateTimeOffset UtcTimestamp(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            value = value.ToUniversalTime();
        }

        if (value == default)
        {
            throw new ArgumentException("A timestamp is required.", parameterName);
        }

        return value;
    }

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CurrencyCodeRegex();

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugRegex();
}
