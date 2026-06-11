using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Infrastructure.DynamoDb;
using Microsoft.Extensions.Configuration;
using Moq;

namespace CaseFraudSys.Api.Tests.Infrastructure;

[Trait("Category", "Unit")]
public class DynamoDbPixIdempotencyRepositoryTests
{
    private readonly Mock<IAmazonDynamoDB> _client = new();
    private readonly DynamoDbPixIdempotencyRepository _repository;

    public DynamoDbPixIdempotencyRepositoryTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DynamoDb:TableName"] = "AccountLimits" })
            .Build();

        _repository = new DynamoDbPixIdempotencyRepository(_client.Object, configuration);
    }

    [Fact]
    public async Task GetCompletedAsync_WhenMissing_ReturnsNull()
    {
        _client
            .Setup(c => c.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse { IsItemSet = false });

        var result = await _repository.GetCompletedAsync("tx-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCompletedAsync_WhenProcessing_ReturnsNull()
    {
        _client
            .Setup(c => c.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse
            {
                IsItemSet = true,
                Item = PixIdempotencyMapper.ToProcessingItem("tx-1", "0001", "12345", 100)
            });

        var result = await _repository.GetCompletedAsync("tx-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPayloadAsync_WhenExists_ReturnsPayload()
    {
        _client
            .Setup(c => c.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse
            {
                IsItemSet = true,
                Item = PixIdempotencyMapper.ToProcessingItem("tx-2", "0001", "12345", 250)
            });

        var payload = await _repository.GetPayloadAsync("tx-2");

        Assert.NotNull(payload);
        Assert.Equal(250, payload!.Amount);
    }

    [Fact]
    public async Task TryAcquireAsync_WhenPutSucceeds_ReturnsTrue()
    {
        _client
            .Setup(c => c.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutItemResponse());

        var acquired = await _repository.TryAcquireAsync("tx-3", "0001", "12345", 100);

        Assert.True(acquired);
    }

    [Fact]
    public async Task TryAcquireAsync_WhenDuplicate_ReturnsFalse()
    {
        _client
            .Setup(c => c.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConditionalCheckFailedException("duplicate"));

        var acquired = await _repository.TryAcquireAsync("tx-4", "0001", "12345", 100);

        Assert.False(acquired);
    }

    [Fact]
    public async Task CompleteAsync_UpdatesItem()
    {
        _client
            .Setup(c => c.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateItemResponse());

        await _repository.CompleteAsync("tx-5", new PixTransactionResponse
        {
            TransactionId = "tx-5",
            Approved = true,
            RemainingLimit = 900,
            CurrentLimit = 900
        });

        _client.Verify(c => c.UpdateItemAsync(
            It.Is<UpdateItemRequest>(r => r.TableName == "AccountLimits"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
