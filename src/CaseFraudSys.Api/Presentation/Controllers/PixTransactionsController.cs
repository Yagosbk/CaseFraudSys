using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Application.Services;
using CaseFraudSys.Api.Infrastructure.Utils;
using Microsoft.AspNetCore.Mvc;

namespace CaseFraudSys.Api.Presentation.Controllers;

/// <summary>
/// Endpoints de processamento de transações PIX (requisito 2.5).
/// </summary>
[ApiController]
[Route("api/pix/transactions")]
[Tags("Transações PIX")]
[Produces("application/json")]
public class PixTransactionsController : ControllerBase
{
    private readonly IPixTransactionService _service;
    private readonly ILogger<PixTransactionsController> _logger;

    public PixTransactionsController(IPixTransactionService service, ILogger<PixTransactionsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Avalia e processa uma transação PIX, debitando o limite disponível quando aprovada.
    /// </summary>
    /// <remarks>
    /// Requisito 2.5 — Transação PIX com idempotência.
    ///
    /// Toda transação exige um `transactionId` único. Reenvios com o mesmo ID retornam
    /// a resposta em cache com `isDuplicate: true`, sem novo débito.
    ///
    /// **Erros possíveis:**
    /// - **400** — TransactionId é obrigatório.; Agência e conta são obrigatórios.; O valor da transação deve ser maior que zero.
    /// - **404** — Conta não encontrada.
    /// - **409** — TransactionId já utilizado com parâmetros diferentes.; Transação em processamento. Tente novamente.
    /// - **500** — Erro interno do servidor.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PixTransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PixTransactionResponse>>> Process(
        [FromBody] ProcessPixTransactionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "POST /api/pix/transactions — processando PIX agência {Agency}, conta {Account}, valor {Amount}",
            request.Agency, request.Account, request.Amount);

        var result = await _service.ProcessAsync(request, cancellationToken);

        _logger.LogInformation(
            "POST /api/pix/transactions — transação {Status} agência {Agency}, conta {Account}, limite restante {RemainingLimit}",
            result.Approved ? "aprovada" : "negada",
            request.Agency, request.Account, result.RemainingLimit);

        var message = result.Approved
            ? "Transação PIX aprovada."
            : "Transação PIX negada. Limite insuficiente.";

        return Ok(ApiResponse<PixTransactionResponse>.Ok(result, message));
    }
}
