using System.Text.RegularExpressions;

namespace CaseFraudSys.Api.Application.Validators;

public static partial class DocumentValidator
{
    public static bool IsValidCpf(string? document)
    {
        if (string.IsNullOrWhiteSpace(document))
            return false;

        var digits = NonDigitRegex().Replace(document, "");
        return digits.Length == 11;
    }

    public static string Normalize(string document)
        => NonDigitRegex().Replace(document, "");

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitRegex();
}
