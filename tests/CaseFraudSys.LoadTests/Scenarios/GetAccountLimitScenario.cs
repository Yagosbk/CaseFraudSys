using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace CaseFraudSys.LoadTests.Scenarios;

public static class GetAccountLimitScenario
{
    public static ScenarioProps Create(HttpClient httpClient, LoadTestSettings settings)
    {
        var baseUrl = settings.BaseUrl.TrimEnd('/');
        var agency = settings.GetLimit.Agency;
        var account = settings.GetLimit.Account;

        return Scenario.Create("get_account_limit", async context =>
        {
            var request = Http.CreateRequest("GET", $"{baseUrl}/api/account-limits/{agency}/{account}")
                .WithHeader("Accept", "application/json");

            return await Http.Send(httpClient, request);
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(
                rate: settings.GetLimit.Rate,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(settings.GetLimit.DurationSeconds)));
    }
}
