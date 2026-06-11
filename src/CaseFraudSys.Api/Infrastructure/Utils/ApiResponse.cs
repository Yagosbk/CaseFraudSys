namespace CaseFraudSys.Api.Infrastructure.Utils;

/// <summary>
/// Envelope padrão de resposta de erro da API.
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida (sempre false em erros).
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensagem descritiva em português.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Identificador de rastreamento da requisição (presente apenas em erros).
    /// </summary>
    public string? TraceId { get; set; }

    public static ApiResponse Fail(string message, string traceId)
        => new() { Success = false, Message = message, TraceId = traceId };
}

/// <summary>
/// Envelope padrão de resposta de sucesso da API com dados tipados.
/// </summary>
/// <typeparam name="T">Tipo do payload retornado em data.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida (sempre true em respostas de sucesso).
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensagem descritiva em português.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Dados retornados pela operação.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Identificador de rastreamento da requisição (geralmente null em sucesso).
    /// </summary>
    public string? TraceId { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Operação realizada com sucesso.")
        => new() { Success = true, Message = message, Data = data };
}
