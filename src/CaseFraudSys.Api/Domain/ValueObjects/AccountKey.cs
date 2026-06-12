using System.Net;
using CaseFraudSys.Api.Domain.Exceptions;

namespace CaseFraudSys.Api.Domain.ValueObjects;

public sealed class AccountKey : IEquatable<AccountKey>
{
    public string Agency { get; }
    public string Account { get; }

    private AccountKey(string agency, string account)
    {
        Agency = agency;
        Account = account;
    }

    public static AccountKey Create(string? agency, string? account)
    {
        if (string.IsNullOrWhiteSpace(agency) || string.IsNullOrWhiteSpace(account))
            throw new ApiException("Agência e conta são obrigatórios.", HttpStatusCode.BadRequest);

        return new AccountKey(agency.Trim(), account.Trim());
    }

    public bool Equals(AccountKey? other) =>
        other is not null && Agency == other.Agency && Account == other.Account;

    public override bool Equals(object? obj) => obj is AccountKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Agency, Account);
}
