using Amazon.DynamoDBv2.Model;
using CaseFraudSys.Api.Domain.Entities;

namespace CaseFraudSys.Api.Infrastructure.DynamoDb;

public static class AccountLimitMapper
{
    public const string PkAttribute = "PK";
    public const string DocumentAttribute = "Document";
    public const string AgencyAttribute = "Agency";
    public const string AccountAttribute = "Account";
    public const string PixLimitAttribute = "PixLimit";

    public static string BuildPk(string agency, string account)
        => $"AGENCY#{agency}#ACCOUNT#{account}";

    public static Dictionary<string, AttributeValue> ToItem(AccountLimit entity)
    {
        return new Dictionary<string, AttributeValue>
        {
            [PkAttribute] = new(BuildPk(entity.Agency, entity.Account)),
            [DocumentAttribute] = new(entity.Document),
            [AgencyAttribute] = new(entity.Agency),
            [AccountAttribute] = new(entity.Account),
            [PixLimitAttribute] = new AttributeValue
            {
                N = entity.PixLimit.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }
        };
    }

    public static AccountLimit FromItem(Dictionary<string, AttributeValue> item) =>
        AccountLimit.Restore(
            item[DocumentAttribute].S,
            item[AgencyAttribute].S,
            item[AccountAttribute].S,
            decimal.Parse(item[PixLimitAttribute].N, System.Globalization.CultureInfo.InvariantCulture));
}
