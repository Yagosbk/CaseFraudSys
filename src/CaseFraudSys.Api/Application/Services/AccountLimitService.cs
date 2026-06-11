using System.Net;
using Amazon.DynamoDBv2.Model;
using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Application.Validators;
using CaseFraudSys.Api.Domain.Entities;
using CaseFraudSys.Api.Domain.Exceptions;
using CaseFraudSys.Api.Domain.Repositories;

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

        if (!DocumentValidator.IsValidCpf(request.Document))
            throw new ApiException("CPF inválido. Informe 11 dígitos.", HttpStatusCode.BadRequest);

        if (request.PixLimit <= 0)
            throw new ApiException("O limite PIX deve ser maior que zero.", HttpStatusCode.BadRequest);

        var entity = new AccountLimit
        {
            Document = DocumentValidator.Normalize(request.Document),
            Agency = request.Agency.Trim(),
            Account = request.Account.Trim(),
            PixLimit = request.PixLimit
        };

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
        ValidateAccountKeys(agency, account);

        var entity = await _repository.GetByAccountAsync(agency.Trim(), account.Trim(), cancellationToken)
            ?? throw new ApiException("Conta não encontrada.", HttpStatusCode.NotFound);

        return ToResponse(entity);
    }

    public async Task<AccountLimitResponse> UpdateLimitAsync(
        string agency,
        string account,
        UpdateAccountLimitRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateAccountKeys(agency, account);

        if (request.PixLimit <= 0)
            throw new ApiException("O limite PIX deve ser maior que zero.", HttpStatusCode.BadRequest);

        try
        {
            await _repository.UpdateLimitAsync(agency.Trim(), account.Trim(), request.PixLimit, cancellationToken);
        }
        catch (ConditionalCheckFailedException)
        {
            throw new ApiException("Conta não encontrada.", HttpStatusCode.NotFound);
        }

        var updated = await _repository.GetByAccountAsync(agency.Trim(), account.Trim(), cancellationToken);
        return ToResponse(updated!);
    }

    public async Task DeleteAsync(string agency, string account, CancellationToken cancellationToken = default)
    {
        ValidateAccountKeys(agency, account);

        try
        {
            await _repository.DeleteAsync(agency.Trim(), account.Trim(), cancellationToken);
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

    private static void ValidateAccountKeys(string agency, string account)
    {
        if (string.IsNullOrWhiteSpace(agency) || string.IsNullOrWhiteSpace(account))
            throw new ApiException("Agência e conta são obrigatórios.", HttpStatusCode.BadRequest);
    }

    private static AccountLimitResponse ToResponse(AccountLimit entity) => new()
    {
        Document = entity.Document,
        Agency = entity.Agency,
        Account = entity.Account,
        PixLimit = entity.PixLimit
    };
}
