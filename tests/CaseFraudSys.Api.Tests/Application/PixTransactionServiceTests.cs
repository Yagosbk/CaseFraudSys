using System.Net;
using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Application.Services;
using CaseFraudSys.Api.Application.Validators;
using CaseFraudSys.Api.Domain.Entities;
using CaseFraudSys.Api.Domain.Exceptions;
using CaseFraudSys.Api.Domain.Repositories;
using Moq;

namespace CaseFraudSys.Api.Tests.Application;

[Trait("Category", "Unit")]
public class PixTransactionServiceTests
{
    private readonly Mock<IAccountLimitRepository> _repository = new();
    private readonly Mock<IPixIdempotencyRepository> _idempotency = new();
    private readonly PixTransactionService _service;

    public PixTransactionServiceTests()
    {
        _service = new PixTransactionService(_repository.Object, _idempotency.Object);
    }

    [Fact]
    public async Task ProcessAsync_WithoutTransactionId_ThrowsBadRequest()
    {
        var request = new ProcessPixTransactionRequest
        {
            TransactionId = "",
            Agency = "0001",
            Account = "12345",
            Amount = 100
        };

        var ex = await Assert.ThrowsAsync<ApiException>(() => _service.ProcessAsync(request));
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_WhenAccountNotFound_ThrowsNotFound()
    {
        SetupIdempotencyForNewTransaction("tx-1");

        _repository
            .Setup(r => r.GetByAccountAsync("0001", "12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountLimit?)null);

        var request = CreateRequest("tx-1", 100);

        var ex = await Assert.ThrowsAsync<ApiException>(() => _service.ProcessAsync(request));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_WhenAmountWithinLimit_ReturnsApproved()
    {
        SetupIdempotencyForNewTransaction("tx-2");

        var account = AccountLimit.Restore("12345678901", "0001", "12345", 1000);

        _repository
            .Setup(r => r.GetByAccountAsync("0001", "12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _repository
            .Setup(r => r.TryDebitPixLimitAsync("0001", "12345", 300, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DebitResult { Approved = true, RemainingLimit = 700 });

        var result = await _service.ProcessAsync(CreateRequest("tx-2", 300));

        Assert.True(result.Approved);
        Assert.Equal(700, result.RemainingLimit);
        Assert.False(result.IsDuplicate);
    }

    [Fact]
    public async Task ProcessAsync_WhenAmountExceedsLimit_ReturnsDeniedWithoutDebit()
    {
        SetupIdempotencyForNewTransaction("tx-3");

        var account = AccountLimit.Restore("12345678901", "0001", "12345", 500);

        _repository
            .Setup(r => r.GetByAccountAsync("0001", "12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _repository
            .Setup(r => r.TryDebitPixLimitAsync("0001", "12345", 800, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DebitResult { Approved = false, RemainingLimit = 500 });

        var result = await _service.ProcessAsync(CreateRequest("tx-3", 800));

        Assert.False(result.Approved);
        Assert.Equal(500, result.CurrentLimit);
    }

    [Fact]
    public async Task ProcessAsync_WhenDuplicateTransactionId_ReturnsCachedResponse()
    {
        _idempotency
            .Setup(r => r.GetPayloadAsync("tx-dup", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PixIdempotencyPayload { Agency = "0001", Account = "12345", Amount = 100 });

        _idempotency
            .Setup(r => r.GetCompletedAsync("tx-dup", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PixTransactionResponse
            {
                TransactionId = "tx-dup",
                Approved = true,
                RemainingLimit = 900,
                CurrentLimit = 900,
                IsDuplicate = true
            });

        var result = await _service.ProcessAsync(CreateRequest("tx-dup", 100));

        Assert.True(result.IsDuplicate);
        Assert.Equal(900, result.RemainingLimit);
        _repository.Verify(r => r.TryDebitPixLimitAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenSameTransactionIdWithDifferentAmount_ThrowsConflict()
    {
        _idempotency
            .Setup(r => r.GetPayloadAsync("tx-conflict", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PixIdempotencyPayload { Agency = "0001", Account = "12345", Amount = 100 });

        _idempotency
            .Setup(r => r.GetCompletedAsync("tx-conflict", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PixTransactionResponse?)null);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            _service.ProcessAsync(CreateRequest("tx-conflict", 200)));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_WhenProcessingConflict_Throws409()
    {
        _idempotency
            .Setup(r => r.GetPayloadAsync("tx-processing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PixIdempotencyPayload?)null);

        _idempotency
            .Setup(r => r.GetCompletedAsync("tx-processing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PixTransactionResponse?)null);

        _idempotency
            .Setup(r => r.TryAcquireAsync("tx-processing", "0001", "12345", It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            _service.ProcessAsync(CreateRequest("tx-processing", 100)));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("Transação em processamento. Tente novamente.", ex.Message);
    }

    private void SetupIdempotencyForNewTransaction(string transactionId)
    {
        _idempotency
            .Setup(r => r.GetPayloadAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PixIdempotencyPayload?)null);

        _idempotency
            .Setup(r => r.GetCompletedAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PixTransactionResponse?)null);

        _idempotency
            .Setup(r => r.TryAcquireAsync(transactionId, "0001", "12345", It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _idempotency
            .Setup(r => r.CompleteAsync(transactionId, It.IsAny<PixTransactionResponse>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static ProcessPixTransactionRequest CreateRequest(string transactionId, decimal amount) => new()
    {
        TransactionId = transactionId,
        Agency = "0001",
        Account = "12345",
        Amount = amount
    };
}

[Trait("Category", "Unit")]
public class DocumentValidatorTests
{
    [Theory]
    [InlineData("12345678901", true)]
    [InlineData("123.456.789-01", true)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public void IsValidCpf_ValidatesFormat(string document, bool expected)
    {
        Assert.Equal(expected, DocumentValidator.IsValidCpf(document));
    }

    [Fact]
    public void Normalize_RemovesNonDigits()
    {
        Assert.Equal("12345678901", DocumentValidator.Normalize("123.456.789-01"));
    }
}
