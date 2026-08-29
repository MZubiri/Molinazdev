using System.ComponentModel.DataAnnotations;
using DigitalServices.Domain.Common;

namespace DigitalServices.Web.ViewModels;

public sealed class CheckoutViewModel
{
    [Required(ErrorMessage = "Selecciona un paquete válido.")]
    public Guid PackageId { get; set; }

    [Required(ErrorMessage = "Ingresa tu nombre completo.")]
    [StringLength(DomainFieldLengths.FullName, ErrorMessage = "El nombre es demasiado largo.")]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [StringLength(DomainFieldLengths.Email, ErrorMessage = "El correo electrónico es demasiado largo.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Ingresa un número telefónico válido.")]
    [StringLength(DomainFieldLengths.PhoneNumber, ErrorMessage = "El número telefónico es demasiado largo.")]
    [Display(Name = "Teléfono")]
    public string? PhoneNumber { get; set; }

    [StringLength(DomainFieldLengths.CompanyName, ErrorMessage = "El nombre de la empresa es demasiado largo.")]
    [Display(Name = "Empresa")]
    public string? CompanyName { get; set; }
}
