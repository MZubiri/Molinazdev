using DigitalServices.Application.Chatbot;
using Xunit;

namespace DigitalServices.Tests.Chatbot;

public sealed class ChatContactExtractorTests
{
    [Theory]
    [InlineData("934772764", "+51 934 772 764", "https://wa.me/51934772764")]
    [InlineData("934 772 764", "+51 934 772 764", "https://wa.me/51934772764")]
    [InlineData("+51 934 772 764", "+51 934 772 764", "https://wa.me/51934772764")]
    [InlineData("51934772764", "+51 934 772 764", "https://wa.me/51934772764")]
    [InlineData("Hola, mi número es 934772764 para que me llamen", "+51 934 772 764", "https://wa.me/51934772764")]
    public void ExtractContact_WithPeruvianPhone_ExtractsAndFormatsCorrectly(
        string input,
        string expectedFormatted,
        string expectedWaUrl)
    {
        var result = ChatContactExtractor.ExtractContact(input);

        Assert.NotNull(result);
        Assert.Equal(expectedFormatted, result.FormattedContact);
        Assert.Equal(expectedWaUrl, result.WhatsAppUrl);
        Assert.NotNull(result.PhoneNumber);
    }

    [Theory]
    [InlineData("contacto@molinazdev.lat")]
    [InlineData("Mi correo es cliente.ventas@empresa.pe por favor")]
    public void ExtractContact_WithEmail_ExtractsCorrectly(string input)
    {
        var result = ChatContactExtractor.ExtractContact(input);

        Assert.NotNull(result);
        Assert.NotNull(result.Email);
        Assert.Contains("@", result.FormattedContact);
    }

    [Theory]
    [InlineData("Hola, cuánto cuesta una web de 1200 soles?")]
    [InlineData("Puedo agendar una llamada de 15 minutos?")]
    [InlineData("Tienen atención 24/7?")]
    [InlineData("Mi presupuesto es de 799 a 3500")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ExtractContact_WithoutContactDetails_ReturnsNull(string? input)
    {
        var result = ChatContactExtractor.ExtractContact(input);

        Assert.Null(result);
    }
}
