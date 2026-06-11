using CaseFraudSys.Api.Application.DTOs;

namespace CaseFraudSys.Api.Application.Services;

public interface IPixTransactionService
{
    Task<PixTransactionResponse> ProcessAsync(ProcessPixTransactionRequest request, CancellationToken cancellationToken = default);
}
