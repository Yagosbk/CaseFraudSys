using Amazon.DynamoDBv2.Model;
using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Domain.Repositories;

namespace CaseFraudSys.Api.Infrastructure.DynamoDb;

public static class PixIdempotencyMapper
{
    public const string StatusAttribute = "Status";
    public const string ApprovedAttribute = "Approved";
    public const string RemainingLimitAttribute = "RemainingLimit";
    public const string CurrentLimitAttribute = "CurrentLimit";
    public const string AmountAttribute = "Amount";

    public const string StatusProcessing = "PROCESSING";
    public const string StatusCompleted = "COMPLETED";

    public static string BuildPk(string transactionId) => $"TX#{transactionId}";

    public static Dictionary<string, AttributeValue> ToProcessingItem(
        string transactionId,
        string agency,
        string account,
        decimal amount)
    {
        return new Dictionary<string, AttributeValue>
        {
            [AccountLimitMapper.PkAttribute] = new(BuildPk(transactionId)),
            [AccountLimitMapper.AgencyAttribute] = new(agency),
            [AccountLimitMapper.AccountAttribute] = new(account),
            [AmountAttribute] = new AttributeValue
            {
                N = amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            [StatusAttribute] = new(StatusProcessing)
        };
    }

    public static PixIdempotencyPayload ToPayload(Dictionary<string, AttributeValue> item) => new()
    {
        Agency = item[AccountLimitMapper.AgencyAttribute].S,
        Account = item[AccountLimitMapper.AccountAttribute].S,
        Amount = decimal.Parse(item[AmountAttribute].N, System.Globalization.CultureInfo.InvariantCulture)
    };

    public static PixTransactionResponse ToResponse(Dictionary<string, AttributeValue> item, string transactionId) => new()
    {
        TransactionId = transactionId,
        Approved = item[PixIdempotencyMapper.ApprovedAttribute].BOOL ?? false,
        RemainingLimit = decimal.Parse(item[RemainingLimitAttribute].N, System.Globalization.CultureInfo.InvariantCulture),
        CurrentLimit = decimal.Parse(item[CurrentLimitAttribute].N, System.Globalization.CultureInfo.InvariantCulture),
        IsDuplicate = true
    };
}
