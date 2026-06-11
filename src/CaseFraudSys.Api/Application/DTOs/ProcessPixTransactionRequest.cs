using System.ComponentModel.DataAnnotations;

namespace CaseFraudSys.Api.Application.DTOs;

/// <summary>
/// Requisição de processamento de transação PIX.
/// </summary>
public class ProcessPixTransactionRequest
{
    /// <summary>
    /// Identificador único da transação para garantir idempotência.
    /// </summary>
    /// <example>tx-001-aprovada</example>
    [Required(ErrorMessage = "O transactionId é obrigatório.")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Código da agência bancária da conta debitada.
    /// </summary>
    /// <example>0001</example>
    [Required(ErrorMessage = "A agência é obrigatória.")]
    public string Agency { get; set; } = string.Empty;

    /// <summary>
    /// Número da conta bancária debitada.
    /// </summary>
    /// <example>12345</example>
    [Required(ErrorMessage = "A conta é obrigatória.")]
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// Valor da transação PIX em reais.
    /// </summary>
    /// <example>1500.00</example>
    [Required(ErrorMessage = "O valor da transação é obrigatório.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor da transação deve ser maior que zero.")]
    public decimal Amount { get; set; }
}
