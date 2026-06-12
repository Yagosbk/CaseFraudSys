using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CaseFraudSys.Api.Domain.Entities;

namespace CaseFraudSys.Api.Infrastructure.DynamoDb;

public class DynamoDbDataSeeder
{
    private readonly IAmazonDynamoDB _client;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DynamoDbDataSeeder> _logger;

    public DynamoDbDataSeeder(
        IAmazonDynamoDB client,
        IConfiguration config,
        IHostEnvironment environment,
        ILogger<DynamoDbDataSeeder> logger)
    {
        _client = client;
        _config = config;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
            return;

        if (!_config.GetValue<bool>("SeedData:Enabled"))
        {
            _logger.LogInformation("Seed de dados desabilitado (SeedData:Enabled = false)");
            return;
        }

        var accounts = _config.GetSection("SeedData:Accounts").Get<List<SeedAccount>>() ?? [];
        if (accounts.Count == 0)
        {
            _logger.LogInformation("Nenhuma conta configurada para seed");
            return;
        }

        var tableName = _config["DynamoDb:TableName"]!;
        var inserted = 0;
        var skipped = 0;

        foreach (var seed in accounts)
        {
            var entity = AccountLimit.Restore(
                seed.Document,
                seed.Agency,
                seed.Account,
                seed.PixLimit);

            try
            {
                await _client.PutItemAsync(new PutItemRequest
                {
                    TableName = tableName,
                    Item = AccountLimitMapper.ToItem(entity),
                    ConditionExpression = "attribute_not_exists(#pk)",
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        ["#pk"] = AccountLimitMapper.PkAttribute
                    }
                }, cancellationToken);

                inserted++;
                _logger.LogInformation(
                    "Seed: conta {Agency}/{Account} inserida com limite {PixLimit}",
                    seed.Agency, seed.Account, seed.PixLimit);
            }
            catch (ConditionalCheckFailedException)
            {
                skipped++;
                _logger.LogInformation(
                    "Seed: conta {Agency}/{Account} já existe, ignorada",
                    seed.Agency, seed.Account);
            }
        }

        _logger.LogInformation("Seed concluído: {Inserted} inseridas, {Skipped} ignoradas", inserted, skipped);
    }

    private class SeedAccount
    {
        public string Document { get; set; } = string.Empty;
        public string Agency { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public decimal PixLimit { get; set; }
    }
}
