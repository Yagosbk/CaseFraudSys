using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CaseFraudSys.Api.Domain.Entities;
using CaseFraudSys.Api.Infrastructure.DynamoDb;
using Microsoft.Extensions.Configuration;
using Moq;

namespace CaseFraudSys.Api.Tests.Infrastructure;

[Trait("Category", "Unit")]
public class DynamoDbAccountLimitRepositoryTests
{
    private readonly Mock<IAmazonDynamoDB> _client = new();
    private readonly DynamoDbAccountLimitRepository _repository;

    public DynamoDbAccountLimitRepositoryTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DynamoDb:TableName"] = "AccountLimits" })
            .Build();

        _repository = new DynamoDbAccountLimitRepository(_client.Object, configuration);
    }

    [Fact]
    public async Task CreateAsync_CallsPutItem()
    {
        _client
            .Setup(c => c.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutItemResponse());

        await _repository.CreateAsync(new AccountLimit
        {
            Document = "12345678901",
            Agency = "0001",
            Account = "12345",
            PixLimit = 1000
        });

        _client.Verify(c => c.PutItemAsync(
            It.Is<PutItemRequest>(r => r.TableName == "AccountLimits"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByAccountAsync_WhenItemMissing_ReturnsNull()
    {
        _client
            .Setup(c => c.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse { IsItemSet = false });

        var result = await _repository.GetByAccountAsync("0001", "12345");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByAccountAsync_WhenItemExists_ReturnsAccountLimit()
    {
        _client
            .Setup(c => c.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse
            {
                IsItemSet = true,
                Item = AccountLimitMapper.ToItem(new AccountLimit
                {
                    Document = "12345678901",
                    Agency = "0001",
                    Account = "12345",
                    PixLimit = 1500
                })
            });

        var result = await _repository.GetByAccountAsync("0001", "12345");

        Assert.NotNull(result);
        Assert.Equal(1500, result!.PixLimit);
    }

    [Fact]
    public async Task TryDebitPixLimitAsync_WhenApproved_ReturnsRemainingLimit()
    {
        _client
            .Setup(c => c.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateItemResponse
            {
                Attributes = AccountLimitMapper.ToItem(new AccountLimit
                {
                    Document = "12345678901",
                    Agency = "0001",
                    Account = "12345",
                    PixLimit = 700
                })
            });

        var result = await _repository.TryDebitPixLimitAsync("0001", "12345", 300);

        Assert.True(result.Approved);
        Assert.Equal(700, result.RemainingLimit);
    }

    [Fact]
    public async Task TryDebitPixLimitAsync_WhenConditionalCheckFails_ReturnsDenied()
    {
        _client
            .Setup(c => c.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConditionalCheckFailedException("insufficient"));

        _client
            .Setup(c => c.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse
            {
                IsItemSet = true,
                Item = AccountLimitMapper.ToItem(new AccountLimit
                {
                    Document = "12345678901",
                    Agency = "0001",
                    Account = "12345",
                    PixLimit = 500
                })
            });

        var result = await _repository.TryDebitPixLimitAsync("0001", "12345", 800);

        Assert.False(result.Approved);
        Assert.Equal(500, result.RemainingLimit);
    }
}
