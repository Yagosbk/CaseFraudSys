using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Application.Services;
using CaseFraudSys.Api.Infrastructure.Utils;
using Microsoft.AspNetCore.Mvc;

namespace CaseFraudSys.Api.Presentation.Controllers;

// MVC (Web API): Controller fino — delega casos de uso aos Application Services.
/// <summary>
/// Endpoints de gestão de limites PIX por conta (requisitos 2.1–2.4).
/// </summary>
[ApiController]
[Route("api/account-limits")]
[Tags("Gestão de Limites PIX")]
[Produces("application/json")]
public class AccountLimitsController : ControllerBase
{
    private readonly IAccountLimitService _service;
    private readonly ILogger<AccountLimitsController> _logger;

    public AccountLimitsController(IAccountLimitService service, ILogger<AccountLimitsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Cadastra um novo limite PIX para a conta informada.
    /// </summary>
    /// <remarks>
    /// Requisito 2.1 — Cadastro de limite PIX.
    ///
    /// **Erros possíveis:**
    /// - **400** — Todos os campos são obrigatórios.; CPF inválido. Informe 11 dígitos.; O limite PIX deve ser maior que zero.
    /// - **409** — Conta já cadastrada.
    /// - **500** — Erro interno do servidor.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AccountLimitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AccountLimitResponse>>> Create(
        [FromBody] CreateAccountLimitRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "POST /api/account-limits — cadastrando limite agência {Agency}, conta {Account}, limite {PixLimit}",
            request.Agency, request.Account, request.PixLimit);

        var result = await _service.CreateAsync(request, cancellationToken);

        _logger.LogInformation(
            "POST /api/account-limits — limite cadastrado agência {Agency}, conta {Account}",
            result.Agency, result.Account);

        return Ok(ApiResponse<AccountLimitResponse>.Ok(result, "Limite cadastrado com sucesso."));
    }

    /// <summary>
    /// Consulta o limite PIX de uma conta pela agência e número da conta.
    /// </summary>
    /// <remarks>
    /// Requisito 2.2 — Consulta de limite PIX.
    ///
    /// **Erros possíveis:**
    /// - **400** — Agência e conta são obrigatórios.
    /// - **404** — Conta não encontrada.
    /// - **500** — Erro interno do servidor.
    /// </remarks>
    /// <param name="agency">Código da agência bancária.</param>
    /// <param name="account">Número da conta bancária.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    [HttpGet("{agency}/{account}")]
    [ProducesResponseType(typeof(ApiResponse<AccountLimitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AccountLimitResponse>>> Get(
        string agency,
        string account,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "GET /api/account-limits/{Agency}/{Account} — consultando limite",
            agency, account);

        var result = await _service.GetByAccountAsync(agency, account, cancellationToken);

        _logger.LogInformation(
            "GET /api/account-limits/{Agency}/{Account} — limite consultado: {PixLimit}",
            agency, account, result.PixLimit);

        return Ok(ApiResponse<AccountLimitResponse>.Ok(result));
    }

    /// <summary>
    /// Altera o limite PIX de uma conta existente.
    /// </summary>
    /// <remarks>
    /// Requisito 2.3 — Alteração de limite PIX.
    ///
    /// **Erros possíveis:**
    /// - **400** — Agência e conta são obrigatórios.; O limite PIX deve ser maior que zero.
    /// - **404** — Conta não encontrada.
    /// - **500** — Erro interno do servidor.
    /// </remarks>
    /// <param name="agency">Código da agência bancária.</param>
    /// <param name="account">Número da conta bancária.</param>
    /// <param name="request">Novo limite PIX desejado.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    [HttpPut("{agency}/{account}")]
    [ProducesResponseType(typeof(ApiResponse<AccountLimitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AccountLimitResponse>>> Update(
        string agency,
        string account,
        [FromBody] UpdateAccountLimitRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "PUT /api/account-limits/{Agency}/{Account} — atualizando limite para {PixLimit}",
            agency, account, request.PixLimit);

        var result = await _service.UpdateLimitAsync(agency, account, request, cancellationToken);

        _logger.LogInformation(
            "PUT /api/account-limits/{Agency}/{Account} — limite atualizado: {PixLimit}",
            agency, account, result.PixLimit);

        return Ok(ApiResponse<AccountLimitResponse>.Ok(result, "Limite atualizado com sucesso."));
    }

    /// <summary>
    /// Remove o registro de limite PIX de uma conta.
    /// </summary>
    /// <remarks>
    /// Requisito 2.4 — Remoção de registro.
    ///
    /// **Erros possíveis:**
    /// - **400** — Agência e conta são obrigatórios.
    /// - **404** — Conta não encontrada.
    /// - **500** — Erro interno do servidor.
    /// </remarks>
    /// <param name="agency">Código da agência bancária.</param>
    /// <param name="account">Número da conta bancária.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    [HttpDelete("{agency}/{account}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteAccountLimitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DeleteAccountLimitResponse>>> Delete(
        string agency,
        string account,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DELETE /api/account-limits/{Agency}/{Account} — removendo registro",
            agency, account);

        await _service.DeleteAsync(agency, account, cancellationToken);

        _logger.LogInformation(
            "DELETE /api/account-limits/{Agency}/{Account} — registro removido",
            agency, account);

        var data = new DeleteAccountLimitResponse { Agency = agency, Account = account };
        return Ok(ApiResponse<DeleteAccountLimitResponse>.Ok(data, "Registro removido com sucesso."));
    }
}
