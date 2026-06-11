using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace CaseFraudSys.Api.Infrastructure.DynamoDb;

public class DynamoDbTableInitializer
{
    private readonly IAmazonDynamoDB _client;
    private readonly IConfiguration _config;
    private readonly ILogger<DynamoDbTableInitializer> _logger;

    public DynamoDbTableInitializer(
        IAmazonDynamoDB client,
        IConfiguration config,
        ILogger<DynamoDbTableInitializer> logger)
    {
        _client = client;
        _config = config;
        _logger = logger;
    }

    public async Task EnsureTableExistsAsync()
    {
        var tableName = _config["DynamoDb:TableName"]!;

        var tables = await _client.ListTablesAsync();
        if (tables.TableNames.Contains(tableName))
        {
            _logger.LogInformation("DynamoDB table {TableName} already exists", tableName);
            return;
        }

        await _client.CreateTableAsync(new CreateTableRequest
        {
            TableName = tableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            AttributeDefinitions =
            [
                new AttributeDefinition("PK", ScalarAttributeType.S)
            ],
            KeySchema =
            [
                new KeySchemaElement("PK", KeyType.HASH)
            ]
        });

        _logger.LogInformation("DynamoDB table {TableName} created", tableName);
    }
}