namespace CaseFraudSys.Api.Domain.Entities;

public class AccountLimit
{
    public required string Document { get; init; }
    public required string Agency { get; init; }
    public required string Account { get; init; }
    public decimal PixLimit { get; set; }
}
