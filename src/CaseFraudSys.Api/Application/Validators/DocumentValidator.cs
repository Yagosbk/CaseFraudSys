using CaseFraudSys.Api.Domain.ValueObjects;

namespace CaseFraudSys.Api.Application.Validators;

public static class DocumentValidator
{
    public static bool IsValidCpf(string? document) => Cpf.TryCreate(document, out _);

    public static string Normalize(string document) => Cpf.Create(document).Value;
}
