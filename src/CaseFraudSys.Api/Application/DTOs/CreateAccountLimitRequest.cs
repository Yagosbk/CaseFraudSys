using System.ComponentModel.DataAnnotations;

namespace CaseFraudSys.Api.Application.DTOs;

/// <summary>
/// Requisição de cadastro de limite PIX para uma conta.
/// </summary>
public class CreateAccountLimitRequest
{
    /// <summary>
    /// CPF do titular da conta (11 dígitos, apenas números).
    /// </summary>
    /// <example>12345678901</example>
    [Required(ErrorMessage = "O CPF é obrigatório.")]
    public string Document { get; set; } = string.Empty;

    /// <summary>
    /// Código da agência bancária.
    /// </summary>
    /// <example>0001</example>
    [Required(ErrorMessage = "A agência é obrigatória.")]
    public string Agency { get; set; } = string.Empty;

    /// <summary>
    /// Número da conta bancária.
    /// </summary>
    /// <example>12345</example>
    [Required(ErrorMessage = "A conta é obrigatória.")]
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// Limite PIX disponível em reais.
    /// </summary>
    /// <example>5000.00</example>
    [Required(ErrorMessage = "O limite PIX é obrigatório.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O limite PIX deve ser maior que zero.")]
    public decimal PixLimit { get; set; }
}
