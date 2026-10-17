using System.Text.RegularExpressions;

namespace DigitalServices.Application.Chatbot;

public sealed record ExtractedContact(
    string RawValue,
    string? PhoneNumber,
    string? Email,
    string FormattedContact,
    string? WhatsAppUrl
);

public static partial class ChatContactExtractor
{
    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    // Matches Peruvian mobile phone numbers (9 digits starting with 9, optional +51 / 51 prefix)
    [GeneratedRegex(@"(?:(?:\+?51|\(51\))[\s.-]*)?(9\d{2}[\s.-]?\d{3}[\s.-]?\d{3})\b")]
    private static partial Regex PeruMobileRegex();

    // Matches international numbers with leading +
    [GeneratedRegex(@"\+\d{1,3}[\s.-]?\(?\d{1,4}\)?[\s.-]?\d{2,4}[\s.-]?\d{2,4}\b")]
    private static partial Regex InternationalPhoneRegex();

    public static ExtractedContact? ExtractContact(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // 1. Try finding email
        var emailMatch = EmailRegex().Match(text);
        string? email = emailMatch.Success ? emailMatch.Value : null;

        // 2. Try finding Peruvian mobile phone
        var peruMatch = PeruMobileRegex().Match(text);
        string? phone = null;
        string? rawPhoneMatch = null;

        if (peruMatch.Success)
        {
            rawPhoneMatch = peruMatch.Value;
            var digits = new string(rawPhoneMatch.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("51") && digits.Length == 11)
            {
                digits = digits[2..];
            }

            if (digits.Length == 9 && digits.StartsWith('9'))
            {
                phone = digits;
            }
        }

        // 3. Try finding international phone if Peru mobile was not found
        if (phone is null)
        {
            var intlMatch = InternationalPhoneRegex().Match(text);
            if (intlMatch.Success)
            {
                var digits = new string(intlMatch.Value.Where(c => char.IsDigit(c) || c == '+').ToArray());
                var digitCount = digits.Count(char.IsDigit);
                if (digitCount >= 8 && digitCount <= 15)
                {
                    rawPhoneMatch = intlMatch.Value;
                    phone = digits;
                }
            }
        }

        if (email is null && phone is null)
        {
            return null;
        }

        string formatted;
        string? waUrl = null;

        if (phone is not null)
        {
            if (phone.Length == 9 && phone.StartsWith('9'))
            {
                formatted = $"+51 {phone[..3]} {phone[3..6]} {phone[6..]}";
                waUrl = $"https://wa.me/51{phone}";
            }
            else
            {
                formatted = phone;
                var cleanDigits = new string(phone.Where(char.IsDigit).ToArray());
                waUrl = $"https://wa.me/{cleanDigits}";
            }

            if (email is not null)
            {
                formatted += $" ({email})";
            }
        }
        else
        {
            formatted = email!;
        }

        return new ExtractedContact(
            RawValue: rawPhoneMatch ?? email!,
            PhoneNumber: phone,
            Email: email,
            FormattedContact: formatted,
            WhatsAppUrl: waUrl
        );
    }
}
