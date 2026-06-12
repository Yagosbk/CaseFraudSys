using CaseFraudSys.Api.Domain.Exceptions;
using CaseFraudSys.Api.Domain.ValueObjects;

namespace CaseFraudSys.Api.Tests.Domain.ValueObjects;

[Trait("Category", "Unit")]
public class AccountKeyTests
{
    [Fact]
    public void Create_WithValidValues_TrimsWhitespace()
    {
        var key = AccountKey.Create(" 0001 ", " 12345 ");

        Assert.Equal("0001", key.Agency);
        Assert.Equal("12345", key.Account);
    }

    [Theory]
    [InlineData("", "12345")]
    [InlineData("0001", "")]
    [InlineData(null, "12345")]
    public void Create_WithMissingValues_ThrowsApiException(string? agency, string? account)
    {
        Assert.Throws<ApiException>(() => AccountKey.Create(agency, account));
    }
}
