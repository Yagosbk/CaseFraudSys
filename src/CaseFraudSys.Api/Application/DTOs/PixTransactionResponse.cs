namespace CaseFraudSys.Api.Application.DTOs;

/// <summary>
/// Resultado do processamento de uma transação PIX.
/// </summary>
public class PixTransactionResponse
{
    /// <summary>
    /// Identificador único da transação (idempotência).
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Indica se a transação foi aprovada com base no limite disponível.
    /// </summary>
    public bool Approved { get; set; }

    /// <summary>
    /// Limite PIX restante após a transação (ou limite atual se negada).
    /// </summary>
    public decimal RemainingLimit { get; set; }

    /// <summary>
    /// Limite PIX da conta no momento da resposta.
    /// </summary>
    public decimal CurrentLimit { get; set; }

    /// <summary>
    /// Indica reenvio idempotente com o mesmo transactionId (sem novo débito).
    /// </summary>
    public bool IsDuplicate { get; set; }
}
