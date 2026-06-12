using System.Net;
using CaseFraudSys.Api.Domain.Exceptions;
using CaseFraudSys.Api.Domain.ValueObjects;

namespace CaseFraudSys.Api.Domain.Entities;

public class AccountLimit
{
    public string Document { get; private set; } = string.Empty;
    public string Agency { get; private set; } = string.Empty;
    public string Account { get; private set; } = string.Empty;
    public decimal PixLimit { get; private set; }

    public static AccountLimit Create(Cpf document, AccountKey key, decimal pixLimit)
    {
        EnsurePositivePixLimit(pixLimit);

        return new AccountLimit
        {
            Document = document.Value,
            Agency = key.Agency,
            Account = key.Account,
            PixLimit = pixLimit
        };
    }

    public static AccountLimit Restore(string document, string agency, string account, decimal pixLimit) =>
        new()
        {
            Document = document,
            Agency = agency,
            Account = account,
            PixLimit = pixLimit
        };

    public void UpdatePixLimit(decimal newLimit)
    {
        EnsurePositivePixLimit(newLimit);
        PixLimit = newLimit;
    }

    public bool CanDebit(decimal amount) => amount > 0 && amount <= PixLimit;

    private static void EnsurePositivePixLimit(decimal pixLimit)
    {
        if (pixLimit <= 0)
            throw new ApiException("O limite PIX deve ser maior que zero.", HttpStatusCode.BadRequest);
    }
}
