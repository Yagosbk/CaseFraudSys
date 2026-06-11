using CaseFraudSys.Api.Domain.Entities;

namespace CaseFraudSys.Api.Domain.Repositories;

public interface IAccountLimitRepository
{
    Task CreateAsync(AccountLimit accountLimit, CancellationToken cancellationToken = default);
    Task<AccountLimit?> GetByAccountAsync(string agency, string account, CancellationToken cancellationToken = default);
    Task UpdateLimitAsync(string agency, string account, decimal pixLimit, CancellationToken cancellationToken = default);
    Task DeleteAsync(string agency, string account, CancellationToken cancellationToken = default);
    Task<DebitResult> TryDebitPixLimitAsync(string agency, string account, decimal amount, CancellationToken cancellationToken = default);
}

public class DebitResult
{
    public bool Approved { get; init; }
    public decimal RemainingLimit { get; init; }
}
