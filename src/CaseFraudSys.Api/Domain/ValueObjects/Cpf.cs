using System.Net;
using System.Text.RegularExpressions;
using CaseFraudSys.Api.Domain.Exceptions;

namespace CaseFraudSys.Api.Domain.ValueObjects;

public sealed partial class Cpf : IEquatable<Cpf>
{
    public string Value { get; }

    private Cpf(string value) => Value = value;

    public static Cpf Create(string? document)
    {
        if (!TryCreate(document, out var cpf))
            throw new ApiException("CPF inválido. Informe 11 dígitos.", HttpStatusCode.BadRequest);

        return cpf!;
    }

    public static bool TryCreate(string? document, out Cpf? cpf)
    {
        cpf = null;

        if (string.IsNullOrWhiteSpace(document))
            return false;

        var digits = NonDigitRegex().Replace(document, "");
        if (digits.Length != 11)
            return false;

        cpf = new Cpf(digits);
        return true;
    }

    public bool Equals(Cpf? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Cpf other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitRegex();
}
