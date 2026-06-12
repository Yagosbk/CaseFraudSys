using System.Net;
using Amazon.DynamoDBv2.Model;
using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Application.Services;
using CaseFraudSys.Api.Domain.Entities;
using CaseFraudSys.Api.Domain.Exceptions;
using CaseFraudSys.Api.Domain.Repositories;
using Moq;

namespace CaseFraudSys.Api.Tests.Application;

[Trait("Category", "Unit")]
public class AccountLimitServiceTests
{
    private readonly Mock<IAccountLimitRepository> _repository = new();
    private readonly AccountLimitService _service;

    public AccountLimitServiceTests()
    {
        _service = new AccountLimitService(_repository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyFields_ThrowsBadRequest()
    {
        var request = new CreateAccountLimitRequest
        {
            Document = "",
            Agency = "0001",
            Account = "12345",
            PixLimit = 1000
        };

        var ex = await Assert.ThrowsAsync<ApiException>(() => _service.CreateAsync(request));
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCpf_ThrowsBadRequest()
    {
        var request = new CreateAccountLimitRequest
        {
            Document = "123",
            Agency = "0001",
            Account = "12345",
            PixLimit = 1000
        };

        var ex = await Assert.ThrowsAsync<ApiException>(() => _service.CreateAsync(request));
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateAccount_ThrowsConflict()
    {
        _repository
            .Setup(r => r.CreateAsync(It.IsAny<AccountLimit>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConditionalCheckFailedException("duplicate"));

        var request = new CreateAccountLimitRequest
        {
            Document = "12345678901",
            Agency = "0001",
            Account = "12345",
            PixLimit = 1000
        };

        var ex = await Assert.ThrowsAsync<ApiException>(() => _service.CreateAsync(request));
        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsResponse()
    {
        var request = new CreateAccountLimitRequest
        {
            Document = "12345678901",
            Agency = "0001",
            Account = "12345",
            PixLimit = 1000
        };

        var result = await _service.CreateAsync(request);

        Assert.Equal("12345678901", result.Document);
        Assert.Equal("0001", result.Agency);
        Assert.Equal("12345", result.Account);
        Assert.Equal(1000, result.PixLimit);
    }

    [Fact]
    public async Task GetByAccountAsync_WhenNotFound_ThrowsNotFound()
    {
        _repository
            .Setup(r => r.GetByAccountAsync("0001", "99999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountLimit?)null);

        var ex = await Assert.ThrowsAsync<ApiException>(
            () => _service.GetByAccountAsync("0001", "99999"));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task GetByAccountAsync_WhenExists_ReturnsResponse()
    {
        _repository
            .Setup(r => r.GetByAccountAsync("0001", "12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccountLimit.Restore("12345678901", "0001", "12345", 2500));

        var result = await _service.GetByAccountAsync("0001", "12345");

        Assert.Equal(2500, result.PixLimit);
        Assert.Equal("12345678901", result.Document);
    }

    [Fact]
    public async Task UpdateLimitAsync_WhenExists_ReturnsUpdated()
    {
        _repository
            .Setup(r => r.GetByAccountAsync("0001", "12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccountLimit.Restore("12345678901", "0001", "12345", 2500));

        _repository
            .Setup(r => r.UpdateLimitAsync("0001", "12345", 3000, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateLimitAsync(
            "0001",
            "12345",
            new UpdateAccountLimitRequest { PixLimit = 3000 });

        Assert.Equal(3000, result.PixLimit);
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_Succeeds()
    {
        _repository
            .Setup(r => r.DeleteAsync("0001", "12345", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.DeleteAsync("0001", "12345");

        _repository.Verify(r => r.DeleteAsync("0001", "12345", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLimitAsync_WhenNotFound_ThrowsNotFound()
    {
        _repository
            .Setup(r => r.GetByAccountAsync("0001", "99999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountLimit?)null);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            _service.UpdateLimitAsync("0001", "99999", new UpdateAccountLimitRequest { PixLimit = 500 }));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ThrowsNotFound()
    {
        _repository
            .Setup(r => r.DeleteAsync("0001", "99999", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConditionalCheckFailedException("not found"));

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            _service.DeleteAsync("0001", "99999"));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }
}
