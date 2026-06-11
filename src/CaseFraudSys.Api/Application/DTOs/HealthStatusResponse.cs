namespace CaseFraudSys.Api.Application.DTOs;

/// <summary>
/// Status de saúde do serviço.
/// </summary>
public class HealthStatusResponse
{
    /// <summary>
    /// Estado atual do serviço (ex.: healthy).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Nome do serviço.
    /// </summary>
    public string Service { get; set; } = string.Empty;
}
