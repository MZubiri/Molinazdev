using System.ComponentModel.DataAnnotations;

namespace DigitalServices.Application.Common;

internal static class RequestValidator
{
    public static void Validate(object request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                results,
                validateAllProperties: true))
        {
            return;
        }

        var errors = results
            .SelectMany(static result =>
            {
                IEnumerable<string> members = result.MemberNames.Any()
                    ? result.MemberNames
                    : new[] { string.Empty };

                return members.Select(member => new
                {
                    Member = member,
                    Message = result.ErrorMessage ?? "The value is invalid."
                });
            })
            .GroupBy(static item => item.Member, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(item => item.Message).Distinct().ToArray(),
                StringComparer.Ordinal);

        throw new ApplicationValidationException(errors);
    }
}
