using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Application.Services;
using CaseFraudSys.Api.Infrastructure.Utils;
using CaseFraudSys.Api.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CaseFraudSys.Api.Tests.Presentation;

[Trait("Category", "Unit")]
public class PixTransactionsControllerTests
{
    private readonly Mock<IPixTransactionService> _service = new();
    private readonly PixTransactionsController _controller;

    public PixTransactionsControllerTests()
    {
        _controller = new PixTransactionsController(_service.Object, NullLogger<PixTransactionsController>.Instance);
    }

    [Fact]
    public async Task Process_WhenApproved_ReturnsApprovedMessage()
    {
        _service
            .Setup(s => s.ProcessAsync(It.IsAny<ProcessPixTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PixTransactionResponse
            {
                TransactionId = "tx-1",
                Approved = true,
                RemainingLimit = 700,
                CurrentLimit = 700
            });

        var result = await _controller.Process(new ProcessPixTransactionRequest
        {
            TransactionId = "tx-1",
            Agency = "0001",
            Account = "12345",
            Amount = 300
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ApiResponse<PixTransactionResponse>>(ok.Value);
        Assert.Equal("Transação PIX aprovada.", body.Message);
    }

    [Fact]
    public async Task Process_WhenDenied_ReturnsDeniedMessage()
    {
        _service
            .Setup(s => s.ProcessAsync(It.IsAny<ProcessPixTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PixTransactionResponse
            {
                TransactionId = "tx-2",
                Approved = false,
                RemainingLimit = 500,
                CurrentLimit = 500
            });

        var result = await _controller.Process(new ProcessPixTransactionRequest
        {
            TransactionId = "tx-2",
            Agency = "0001",
            Account = "12345",
            Amount = 800
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ApiResponse<PixTransactionResponse>>(ok.Value);
        Assert.Equal("Transação PIX negada. Limite insuficiente.", body.Message);
    }
}
