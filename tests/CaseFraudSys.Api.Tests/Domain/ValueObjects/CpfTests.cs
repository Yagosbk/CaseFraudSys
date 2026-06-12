using CaseFraudSys.Api.Domain.Exceptions;
using CaseFraudSys.Api.Domain.ValueObjects;

namespace CaseFraudSys.Api.Tests.Domain.ValueObjects;

[Trait("Category", "Unit")]
public class CpfTests
{
    [Theory]
    [InlineData("12345678901")]
    [InlineData("123.456.789-01")]
    public void Create_WithValidDocument_ReturnsNormalizedValue(string document)
    {
        var cpf = Cpf.Create(document);

        Assert.Equal("12345678901", cpf.Value);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_WithInvalidDocument_ThrowsApiException(string? document)
    {
        Assert.Throws<ApiException>(() => Cpf.Create(document));
    }

    [Theory]
    [InlineData("12345678901", true)]
    [InlineData("123.456.789-01", true)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public void TryCreate_ValidatesFormat(string? document, bool expected)
    {
        Assert.Equal(expected, Cpf.TryCreate(document, out _));
    }
}
