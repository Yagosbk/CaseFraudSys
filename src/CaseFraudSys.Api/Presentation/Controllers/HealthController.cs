using CaseFraudSys.Api.Application.DTOs;
using CaseFraudSys.Api.Infrastructure.Utils;
using Microsoft.AspNetCore.Mvc;

namespace CaseFraudSys.Api.Presentation.Controllers;

/// <summary>
/// Endpoints de verificação de saúde da API.
/// </summary>
[ApiController]
[Route("api/health")]
[Tags("Saúde da API")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Verifica se a API está ativa e respondendo.
    /// </summary>
    /// <remarks>
    /// Endpoint de health check para monitoramento e testes de carga.
    ///
    /// **Erros possíveis:**
    /// - **500** — Erro interno do servidor.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<ApiResponse<HealthStatusResponse>> Get()
    {
        _logger.LogInformation("GET /api/health — health check solicitado");

        var data = new HealthStatusResponse
        {
            Status = "healthy",
            Service = "CaseFraudSys"
        };

        var response = ApiResponse<HealthStatusResponse>.Ok(data);
        _logger.LogInformation("GET /api/health — status healthy");
        return Ok(response);
    }
}
