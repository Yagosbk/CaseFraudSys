using Amazon.DynamoDBv2.Model;
using CaseFraudSys.Api.Infrastructure.DynamoDb;

namespace CaseFraudSys.Api.Tests.Infrastructure;

[Trait("Category", "Unit")]
public class PixIdempotencyMapperTests
{
    [Fact]
    public void BuildPk_FormatsTransactionId()
    {
        Assert.Equal("TX#tx-001", PixIdempotencyMapper.BuildPk("tx-001"));
    }

    [Fact]
    public void ToProcessingItem_ContainsExpectedAttributes()
    {
        var item = PixIdempotencyMapper.ToProcessingItem("tx-001", "0001", "12345", 150m);

        Assert.Equal("TX#tx-001", item[AccountLimitMapper.PkAttribute].S);
        Assert.Equal("0001", item[AccountLimitMapper.AgencyAttribute].S);
        Assert.Equal("12345", item[AccountLimitMapper.AccountAttribute].S);
        Assert.Equal("150", item[PixIdempotencyMapper.AmountAttribute].N);
        Assert.Equal(PixIdempotencyMapper.StatusProcessing, item[PixIdempotencyMapper.StatusAttribute].S);
    }

    [Fact]
    public void ToPayload_ParsesStoredItem()
    {
        var item = PixIdempotencyMapper.ToProcessingItem("tx-002", "0002", "67890", 200m);

        var payload = PixIdempotencyMapper.ToPayload(item);

        Assert.Equal("0002", payload.Agency);
        Assert.Equal("67890", payload.Account);
        Assert.Equal(200m, payload.Amount);
    }

    [Fact]
    public void ToResponse_MapsCompletedTransaction()
    {
        var item = new Dictionary<string, AttributeValue>
        {
            [PixIdempotencyMapper.ApprovedAttribute] = new() { BOOL = true },
            [PixIdempotencyMapper.RemainingLimitAttribute] = new() { N = "700" },
            [PixIdempotencyMapper.CurrentLimitAttribute] = new() { N = "700" }
        };

        var response = PixIdempotencyMapper.ToResponse(item, "tx-done");

        Assert.Equal("tx-done", response.TransactionId);
        Assert.True(response.Approved);
        Assert.Equal(700m, response.RemainingLimit);
        Assert.Equal(700m, response.CurrentLimit);
        Assert.True(response.IsDuplicate);
    }
}
