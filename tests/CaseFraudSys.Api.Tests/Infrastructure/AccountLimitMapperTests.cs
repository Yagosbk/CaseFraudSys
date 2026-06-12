using CaseFraudSys.Api.Domain.Entities;
using CaseFraudSys.Api.Infrastructure.DynamoDb;

namespace CaseFraudSys.Api.Tests.Infrastructure;

[Trait("Category", "Unit")]
public class AccountLimitMapperTests
{
    [Fact]
    public void BuildPk_FormatsAgencyAndAccount()
    {
        var pk = AccountLimitMapper.BuildPk("0001", "12345");
        Assert.Equal("AGENCY#0001#ACCOUNT#12345", pk);
    }

    [Fact]
    public void ToItem_AndFromItem_RoundTripPreservesData()
    {
        var entity = AccountLimit.Restore("12345678901", "0001", "12345", 5000.50m);

        var item = AccountLimitMapper.ToItem(entity);
        var restored = AccountLimitMapper.FromItem(item);

        Assert.Equal(entity.Document, restored.Document);
        Assert.Equal(entity.Agency, restored.Agency);
        Assert.Equal(entity.Account, restored.Account);
        Assert.Equal(entity.PixLimit, restored.PixLimit);
    }
}
