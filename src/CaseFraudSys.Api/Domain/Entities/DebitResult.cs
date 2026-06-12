namespace CaseFraudSys.Api.Domain.Entities;

public class DebitResult
{
    public bool Approved { get; init; }
    public decimal RemainingLimit { get; init; }
}
