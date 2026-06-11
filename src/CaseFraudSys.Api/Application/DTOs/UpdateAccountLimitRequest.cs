using System.ComponentModel.DataAnnotations;

namespace CaseFraudSys.Api.Application.DTOs;

/// <summary>
/// Requisição de alteração do limite PIX de uma conta.
/// </summary>
public class UpdateAccountLimitRequest
{
    /// <summary>
    /// Novo limite PIX disponível em reais.
    /// </summary>
    /// <example>3000.00</example>
    [Required(ErrorMessage = "O limite PIX é obrigatório.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O limite PIX deve ser maior que zero.")]
    public decimal PixLimit { get; set; }
}
