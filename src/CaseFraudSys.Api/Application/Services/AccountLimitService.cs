using System.Net;
using Amazon.DynamoDBv2.Model;
using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Domain.Entities;
using CaseFraudSys.Api.Domain.Exceptions;
using CaseFraudSys.Api.Domain.Repositories;
using CaseFraudSys.Api.Domain.ValueObjects;

namespace CaseFraudSys.Api.Application.Services;

public class AccountLimitService : IAccountLimitService
{
    private readonly IAccountLimitRepository _repository;

    public AccountLimitService(IAccountLimitRepository repository)
    {
        _repository = repository;
    }

    public async Task<AccountLimitResponse> CreateAsync(CreateAccountLimitRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequiredFields(request.Document, request.Agency, request.Account);

        var entity = AccountLimit.Create(
            Cpf.Create(request.Document),
            AccountKey.Create(request.Agency, request.Account),
            request.PixLimit);

        try
        {
            await _repository.CreateAsync(entity, cancellationToken);
        }
        catch (ConditionalCheckFailedException)
        {
            throw new ApiException("Conta já cadastrada.", HttpStatusCode.Conflict);
        }

        return ToResponse(entity);
    }

    public async Task<AccountLimitResponse> GetByAccountAsync(string agency, string account, CancellationToken cancellationToken = default)
    {
        var key = AccountKey.Create(agency, account);

        var entity = await _repository.GetByAccountAsync(key.Agency, key.Account, cancellationToken)
            ?? throw new ApiException("Conta não encontrada.", HttpStatusCode.NotFound);

        return ToResponse(entity);
    }

    public async Task<AccountLimitResponse> UpdateLimitAsync(
        string agency,
        string account,
        UpdateAccountLimitRequest request,
        CancellationToken cancellationToken = default)
    {
        var key = AccountKey.Create(agency, account);
        var existing = await _repository.GetByAccountAsync(key.Agency, key.Account, cancellationToken)
            ?? throw new ApiException("Conta não encontrada.", HttpStatusCode.NotFound);

        existing.UpdatePixLimit(request.PixLimit);

        try
        {
            await _repository.UpdateLimitAsync(key.Agency, key.Account, existing.PixLimit, cancellationToken);
        }
        catch (ConditionalCheckFailedException)
        {
            throw new ApiException("Conta não encontrada.", HttpStatusCode.NotFound);
        }

        return ToResponse(existing);
    }

    public async Task DeleteAsync(string agency, string account, CancellationToken cancellationToken = default)
    {
        var key = AccountKey.Create(agency, account);

        try
        {
            await _repository.DeleteAsync(key.Agency, key.Account, cancellationToken);
        }
        catch (ConditionalCheckFailedException)
        {
            throw new ApiException("Conta não encontrada.", HttpStatusCode.NotFound);
        }
    }

    private static void ValidateRequiredFields(string document, string agency, string account)
    {
        if (string.IsNullOrWhiteSpace(document)
            || string.IsNullOrWhiteSpace(agency)
            || string.IsNullOrWhiteSpace(account))
        {
            throw new ApiException("Todos os campos são obrigatórios.", HttpStatusCode.BadRequest);
        }
    }

    private static AccountLimitResponse ToResponse(AccountLimit entity) => new()
    {
        Document = entity.Document,
        Agency = entity.Agency,
        Account = entity.Account,
        PixLimit = entity.PixLimit
    };
}
