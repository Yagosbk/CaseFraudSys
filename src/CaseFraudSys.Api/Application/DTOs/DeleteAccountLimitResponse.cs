namespace CaseFraudSys.Api.Application.DTOs;

/// <summary>
/// Confirmação de remoção de limite PIX.
/// </summary>
public class DeleteAccountLimitResponse
{
    /// <summary>
    /// Código da agência bancária removida.
    /// </summary>
    public string Agency { get; set; } = string.Empty;

    /// <summary>
    /// Número da conta removida.
    /// </summary>
    public string Account { get; set; } = string.Empty;
}
