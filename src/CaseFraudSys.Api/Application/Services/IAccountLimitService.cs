using CaseFraudSys.Api.Application.DTOs;

namespace CaseFraudSys.Api.Application.Services;

public interface IAccountLimitService
{
    Task<AccountLimitResponse> CreateAsync(CreateAccountLimitRequest request, CancellationToken cancellationToken = default);
    Task<AccountLimitResponse> GetByAccountAsync(string agency, string account, CancellationToken cancellationToken = default);
    Task<AccountLimitResponse> UpdateLimitAsync(string agency, string account, UpdateAccountLimitRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string agency, string account, CancellationToken cancellationToken = default);
}
