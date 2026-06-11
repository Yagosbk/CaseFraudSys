using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Application.Services;
using CaseFraudSys.Api.Infrastructure.Utils;
using CaseFraudSys.Api.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CaseFraudSys.Api.Tests.Presentation;

[Trait("Category", "Unit")]
public class AccountLimitsControllerTests
{
    private readonly Mock<IAccountLimitService> _service = new();
    private readonly AccountLimitsController _controller;

    public AccountLimitsControllerTests()
    {
        _controller = new AccountLimitsController(_service.Object, NullLogger<AccountLimitsController>.Instance);
    }

    [Fact]
    public async Task Create_ReturnsOkWithResponse()
    {
        _service
            .Setup(s => s.CreateAsync(It.IsAny<CreateAccountLimitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountLimitResponse
            {
                Document = "12345678901",
                Agency = "0001",
                Account = "12345",
                PixLimit = 1000
            });

        var result = await _controller.Create(new CreateAccountLimitRequest
        {
            Document = "12345678901",
            Agency = "0001",
            Account = "12345",
            PixLimit = 1000
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ApiResponse<AccountLimitResponse>>(ok.Value);
        Assert.True(body.Success);
        Assert.Equal(1000, body.Data!.PixLimit);
    }

    [Fact]
    public async Task Get_ReturnsOkWithResponse()
    {
        _service
            .Setup(s => s.GetByAccountAsync("0001", "12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountLimitResponse { Agency = "0001", Account = "12345", PixLimit = 500 });

        var result = await _controller.Get("0001", "12345", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<ApiResponse<AccountLimitResponse>>(ok.Value);
    }

    [Fact]
    public async Task Update_ReturnsOkWithResponse()
    {
        _service
            .Setup(s => s.UpdateLimitAsync("0001", "12345", It.IsAny<UpdateAccountLimitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountLimitResponse { Agency = "0001", Account = "12345", PixLimit = 3000 });

        var result = await _controller.Update("0001", "12345", new UpdateAccountLimitRequest { PixLimit = 3000 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ApiResponse<AccountLimitResponse>>(ok.Value);
        Assert.Equal("Limite atualizado com sucesso.", body.Message);
    }

    [Fact]
    public async Task Delete_ReturnsOkWithResponse()
    {
        _service
            .Setup(s => s.DeleteAsync("0001", "12345", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete("0001", "12345", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ApiResponse<DeleteAccountLimitResponse>>(ok.Value);
        Assert.Equal("0001", body.Data!.Agency);
        Assert.Equal("12345", body.Data.Account);
    }
}
