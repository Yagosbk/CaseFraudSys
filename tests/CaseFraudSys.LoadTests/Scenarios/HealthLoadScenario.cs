using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace CaseFraudSys.LoadTests.Scenarios;

public static class HealthLoadScenario
{
    public static ScenarioProps Create(HttpClient httpClient, LoadTestSettings settings)
    {
        var baseUrl = settings.BaseUrl.TrimEnd('/');

        return Scenario.Create("health_load", async context =>
        {
            var request = Http.CreateRequest("GET", $"{baseUrl}/api/health")
                .WithHeader("Accept", "application/json");

            return await Http.Send(httpClient, request);
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(
                rate: settings.Health.Rate,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(settings.Health.DurationSeconds)));
    }
}
