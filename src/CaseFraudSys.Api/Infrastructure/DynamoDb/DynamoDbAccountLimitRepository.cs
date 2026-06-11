using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CaseFraudSys.Api.Domain.Entities;
using CaseFraudSys.Api.Domain.Repositories;

namespace CaseFraudSys.Api.Infrastructure.DynamoDb;

public class DynamoDbAccountLimitRepository : IAccountLimitRepository
{
    private readonly IAmazonDynamoDB _client;
    private readonly string _tableName;

    public DynamoDbAccountLimitRepository(IAmazonDynamoDB client, IConfiguration configuration)
    {
        _client = client;
        _tableName = configuration["DynamoDb:TableName"]!;
    }

    public async Task CreateAsync(AccountLimit accountLimit, CancellationToken cancellationToken = default)
    {
        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = AccountLimitMapper.ToItem(accountLimit),
            ConditionExpression = "attribute_not_exists(#pk)",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#pk"] = AccountLimitMapper.PkAttribute
            }
        };

        await _client.PutItemAsync(request, cancellationToken);
    }

    public async Task<AccountLimit?> GetByAccountAsync(string agency, string account, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [AccountLimitMapper.PkAttribute] = new(AccountLimitMapper.BuildPk(agency, account))
            }
        }, cancellationToken);

        if (!response.IsItemSet)
            return null;

        return AccountLimitMapper.FromItem(response.Item);
    }

    public async Task UpdateLimitAsync(string agency, string account, decimal pixLimit, CancellationToken cancellationToken = default)
    {
        var request = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [AccountLimitMapper.PkAttribute] = new(AccountLimitMapper.BuildPk(agency, account))
            },
            UpdateExpression = "SET #pixLimit = :pixLimit",
            ConditionExpression = "attribute_exists(#pk)",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#pk"] = AccountLimitMapper.PkAttribute,
                ["#pixLimit"] = AccountLimitMapper.PixLimitAttribute
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pixLimit"] = new AttributeValue
                {
                    N = pixLimit.ToString(System.Globalization.CultureInfo.InvariantCulture)
                }
            }
        };

        await _client.UpdateItemAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(string agency, string account, CancellationToken cancellationToken = default)
    {
        var request = new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [AccountLimitMapper.PkAttribute] = new(AccountLimitMapper.BuildPk(agency, account))
            },
            ConditionExpression = "attribute_exists(#pk)",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#pk"] = AccountLimitMapper.PkAttribute
            }
        };

        await _client.DeleteItemAsync(request, cancellationToken);
    }

    public async Task<DebitResult> TryDebitPixLimitAsync(string agency, string account, decimal amount, CancellationToken cancellationToken = default)
    {
        var amountStr = amount.ToString(System.Globalization.CultureInfo.InvariantCulture);

        try
        {
            var response = await _client.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    [AccountLimitMapper.PkAttribute] = new(AccountLimitMapper.BuildPk(agency, account))
                },
                UpdateExpression = "SET #pixLimit = #pixLimit - :amount",
                ConditionExpression = "attribute_exists(#pk) AND #pixLimit >= :amount",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#pk"] = AccountLimitMapper.PkAttribute,
                    ["#pixLimit"] = AccountLimitMapper.PixLimitAttribute
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":amount"] = new AttributeValue { N = amountStr }
                },
                ReturnValues = ReturnValue.ALL_NEW
            }, cancellationToken);

            var updated = AccountLimitMapper.FromItem(response.Attributes);
            return new DebitResult
            {
                Approved = true,
                RemainingLimit = updated.PixLimit
            };
        }
        catch (ConditionalCheckFailedException)
        {
            var current = await GetByAccountAsync(agency, account, cancellationToken);
            return new DebitResult
            {
                Approved = false,
                RemainingLimit = current?.PixLimit ?? 0
            };
        }
    }
}
