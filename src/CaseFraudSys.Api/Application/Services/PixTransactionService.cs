using System.Net;
using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Domain.Exceptions;
using CaseFraudSys.Api.Domain.Repositories;

namespace CaseFraudSys.Api.Application.Services;

public class PixTransactionService : IPixTransactionService
{
    private const int MaxPollAttempts = 10;
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(100);

    private readonly IAccountLimitRepository _repository;
    private readonly IPixIdempotencyRepository _idempotencyRepository;

    public PixTransactionService(
        IAccountLimitRepository repository,
        IPixIdempotencyRepository idempotencyRepository)
    {
        _repository = repository;
        _idempotencyRepository = idempotencyRepository;
    }

    public async Task<PixTransactionResponse> ProcessAsync(
        ProcessPixTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TransactionId))
            throw new ApiException("TransactionId é obrigatório.", HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(request.Agency) || string.IsNullOrWhiteSpace(request.Account))
            throw new ApiException("Agência e conta são obrigatórios.", HttpStatusCode.BadRequest);

        if (request.Amount <= 0)
            throw new ApiException("O valor da transação deve ser maior que zero.", HttpStatusCode.BadRequest);

        var transactionId = request.TransactionId.Trim();
        var agency = request.Agency.Trim();
        var account = request.Account.Trim();

        await ValidateIdempotencyPayloadAsync(transactionId, agency, account, request.Amount, cancellationToken);

        var cached = await _idempotencyRepository.GetCompletedAsync(transactionId, cancellationToken);
        if (cached is not null)
            return cached;

        var acquired = await _idempotencyRepository.TryAcquireAsync(
            transactionId, agency, account, request.Amount, cancellationToken);

        if (!acquired)
        {
            cached = await WaitForCompletedAsync(transactionId, cancellationToken);
            if (cached is not null)
                return cached;

            throw new ApiException("Transação em processamento. Tente novamente.", HttpStatusCode.Conflict);
        }

        var existing = await _repository.GetByAccountAsync(agency, account, cancellationToken)
            ?? throw new ApiException("Conta não encontrada.", HttpStatusCode.NotFound);

        var result = await _repository.TryDebitPixLimitAsync(agency, account, request.Amount, cancellationToken);

        var response = new PixTransactionResponse
        {
            TransactionId = transactionId,
            Approved = result.Approved,
            RemainingLimit = result.Approved ? result.RemainingLimit : existing.PixLimit,
            CurrentLimit = result.Approved ? result.RemainingLimit : existing.PixLimit,
            IsDuplicate = false
        };

        await _idempotencyRepository.CompleteAsync(transactionId, response, cancellationToken);
        return response;
    }

    private async Task ValidateIdempotencyPayloadAsync(
        string transactionId,
        string agency,
        string account,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var payload = await _idempotencyRepository.GetPayloadAsync(transactionId, cancellationToken);
        if (payload is null)
            return;

        if (payload.Agency != agency || payload.Account != account || payload.Amount != amount)
        {
            throw new ApiException(
                "TransactionId já utilizado com parâmetros diferentes.",
                HttpStatusCode.Conflict);
        }
    }

    private async Task<PixTransactionResponse?> WaitForCompletedAsync(
        string transactionId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxPollAttempts; attempt++)
        {
            var cached = await _idempotencyRepository.GetCompletedAsync(transactionId, cancellationToken);
            if (cached is not null)
                return cached;

            await Task.Delay(PollDelay, cancellationToken);
        }

        return null;
    }
}
