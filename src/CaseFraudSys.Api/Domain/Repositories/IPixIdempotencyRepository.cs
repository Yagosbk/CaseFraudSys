using CaseFraudSys.Api.Application.DTOs;

namespace CaseFraudSys.Api.Domain.Repositories;

public interface IPixIdempotencyRepository
{
    Task<PixTransactionResponse?> GetCompletedAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<bool> TryAcquireAsync(
        string transactionId,
        string agency,
        string account,
        decimal amount,
        CancellationToken cancellationToken = default);
    Task<PixIdempotencyPayload?> GetPayloadAsync(string transactionId, CancellationToken cancellationToken = default);
    Task CompleteAsync(string transactionId, PixTransactionResponse response, CancellationToken cancellationToken = default);
}

public class PixIdempotencyPayload
{
    public required string Agency { get; init; }
    public required string Account { get; init; }
    public decimal Amount { get; init; }
}
