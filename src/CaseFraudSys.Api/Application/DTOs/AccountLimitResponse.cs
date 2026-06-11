namespace CaseFraudSys.Api.Application.DTOs;

/// <summary>
/// Dados de limite PIX de uma conta.
/// </summary>
public class AccountLimitResponse
{
    /// <summary>
    /// CPF do titular da conta (11 dígitos).
    /// </summary>
    public string Document { get; set; } = string.Empty;

    /// <summary>
    /// Código da agência bancária.
    /// </summary>
    public string Agency { get; set; } = string.Empty;

    /// <summary>
    /// Número da conta bancária.
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// Limite PIX disponível em reais.
    /// </summary>
    public decimal PixLimit { get; set; }
}
