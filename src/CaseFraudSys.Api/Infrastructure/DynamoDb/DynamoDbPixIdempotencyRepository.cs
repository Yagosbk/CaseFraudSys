using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Domain.Repositories;

namespace CaseFraudSys.Api.Infrastructure.DynamoDb;

public class DynamoDbPixIdempotencyRepository : IPixIdempotencyRepository
{
    private readonly IAmazonDynamoDB _client;
    private readonly string _tableName;

    public DynamoDbPixIdempotencyRepository(IAmazonDynamoDB client, IConfiguration configuration)
    {
        _client = client;
        _tableName = configuration["DynamoDb:TableName"]!;
    }

    public async Task<PixTransactionResponse?> GetCompletedAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        var item = await GetItemAsync(transactionId, cancellationToken);
        if (item is null)
            return null;

        if (item[PixIdempotencyMapper.StatusAttribute].S != PixIdempotencyMapper.StatusCompleted)
            return null;

        return PixIdempotencyMapper.ToResponse(item, transactionId);
    }

    public async Task<PixIdempotencyPayload?> GetPayloadAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        var item = await GetItemAsync(transactionId, cancellationToken);
        return item is null ? null : PixIdempotencyMapper.ToPayload(item);
    }

    public async Task<bool> TryAcquireAsync(
        string transactionId,
        string agency,
        string account,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = PixIdempotencyMapper.ToProcessingItem(transactionId, agency, account, amount),
                ConditionExpression = "attribute_not_exists(#pk)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#pk"] = AccountLimitMapper.PkAttribute
                }
            }, cancellationToken);

            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public async Task CompleteAsync(
        string transactionId,
        PixTransactionResponse response,
        CancellationToken cancellationToken = default)
    {
        await _client.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [AccountLimitMapper.PkAttribute] = new(PixIdempotencyMapper.BuildPk(transactionId))
            },
            UpdateExpression =
                "SET #status = :status, #approved = :approved, #remaining = :remaining, #current = :current",
            ConditionExpression = "#status = :processing",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#status"] = PixIdempotencyMapper.StatusAttribute,
                ["#approved"] = PixIdempotencyMapper.ApprovedAttribute,
                ["#remaining"] = PixIdempotencyMapper.RemainingLimitAttribute,
                ["#current"] = PixIdempotencyMapper.CurrentLimitAttribute
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":status"] = new(PixIdempotencyMapper.StatusCompleted),
                [":processing"] = new(PixIdempotencyMapper.StatusProcessing),
                [":approved"] = new AttributeValue { BOOL = response.Approved },
                [":remaining"] = new AttributeValue
                {
                    N = response.RemainingLimit.ToString(System.Globalization.CultureInfo.InvariantCulture)
                },
                [":current"] = new AttributeValue
                {
                    N = response.CurrentLimit.ToString(System.Globalization.CultureInfo.InvariantCulture)
                }
            }
        }, cancellationToken);
    }

    private async Task<Dictionary<string, AttributeValue>?> GetItemAsync(
        string transactionId,
        CancellationToken cancellationToken)
    {
        var response = await _client.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [AccountLimitMapper.PkAttribute] = new(PixIdempotencyMapper.BuildPk(transactionId))
            }
        }, cancellationToken);

        return response.IsItemSet ? response.Item : null;
    }
}
