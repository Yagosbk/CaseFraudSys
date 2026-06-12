using CaseFraudSys.Api.Domain.Entities;
using CaseFraudSys.Api.Domain.Exceptions;
using CaseFraudSys.Api.Domain.ValueObjects;

namespace CaseFraudSys.Api.Tests.Domain.Entities;

[Trait("Category", "Unit")]
public class AccountLimitTests
{
    [Fact]
    public void Create_WithValidData_ReturnsEntity()
    {
        var entity = AccountLimit.Create(
            Cpf.Create("12345678901"),
            AccountKey.Create("0001", "12345"),
            5000);

        Assert.Equal("12345678901", entity.Document);
        Assert.Equal("0001", entity.Agency);
        Assert.Equal("12345", entity.Account);
        Assert.Equal(5000, entity.PixLimit);
    }

    [Fact]
    public void Create_WithNonPositiveLimit_ThrowsApiException()
    {
        Assert.Throws<ApiException>(() => AccountLimit.Create(
            Cpf.Create("12345678901"),
            AccountKey.Create("0001", "12345"),
            0));
    }

    [Fact]
    public void UpdatePixLimit_WithPositiveValue_UpdatesLimit()
    {
        var entity = AccountLimit.Restore("12345678901", "0001", "12345", 5000);

        entity.UpdatePixLimit(3000);

        Assert.Equal(3000, entity.PixLimit);
    }

    [Theory]
    [InlineData(100, 5000, true)]
    [InlineData(5000, 5000, true)]
    [InlineData(5001, 5000, false)]
    [InlineData(0, 5000, false)]
    public void CanDebit_EvaluatesBusinessRule(decimal amount, decimal limit, bool expected)
    {
        var entity = AccountLimit.Restore("12345678901", "0001", "12345", limit);

        Assert.Equal(expected, entity.CanDebit(amount));
    }
}
