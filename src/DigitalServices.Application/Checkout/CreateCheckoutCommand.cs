using System.ComponentModel.DataAnnotations;
using DigitalServices.Domain.Common;

namespace DigitalServices.Application.Checkout;

public sealed class CreateCheckoutCommand
{
    [Required]
    public Guid PackageId { get; init; }

    [Required]
    [StringLength(DomainFieldLengths.FullName, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(DomainFieldLengths.Email)]
    public string Email { get; init; } = string.Empty;

    [Phone]
    [StringLength(DomainFieldLengths.PhoneNumber)]
    public string? PhoneNumber { get; init; }

    [StringLength(DomainFieldLengths.CompanyName)]
    public string? CompanyName { get; init; }
}
