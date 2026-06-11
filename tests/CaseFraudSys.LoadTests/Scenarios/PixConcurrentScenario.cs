using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace CaseFraudSys.LoadTests.Scenarios;

public static class PixConcurrentScenario
{
    public static async Task SetupAccountAsync(LoadTestSettings settings)
    {
        var pix = settings.PixConcurrent;
        var baseUrl = settings.BaseUrl.TrimEnd('/');

        using var client = new HttpClient();

        var createPayload = new
        {
            document = pix.Document,
            agency = pix.Agency,
            account = pix.Account,
            pixLimit = pix.InitialLimit
        };

        var postResponse = await client.PostAsJsonAsync($"{baseUrl}/api/account-limits", createPayload);

        if (postResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var updatePayload = new { pixLimit = pix.InitialLimit };
            var putResponse = await client.PutAsJsonAsync(
                $"{baseUrl}/api/account-limits/{pix.Agency}/{pix.Account}",
                updatePayload);

            putResponse.EnsureSuccessStatusCode();
            Console.WriteLine($"Conta {pix.Agency}/{pix.Account} resetada com limite {pix.InitialLimit}");
            return;
        }

        postResponse.EnsureSuccessStatusCode();
        Console.WriteLine($"Conta {pix.Agency}/{pix.Account} criada com limite {pix.InitialLimit}");
    }

    public static ScenarioProps Create(HttpClient httpClient, LoadTestSettings settings)
    {
        var pix = settings.PixConcurrent;
        var baseUrl = settings.BaseUrl.TrimEnd('/');
        var durationSeconds = Math.Max(1, (int)Math.Ceiling((double)pix.Iterations / pix.Copies));

        return Scenario.Create("pix_concurrent", async context =>
        {
            var bodyJson = JsonSerializer.Serialize(new
            {
                transactionId = $"load-{Guid.NewGuid():N}",
                agency = pix.Agency,
                account = pix.Account,
                amount = pix.Amount
            });

            var request = Http.CreateRequest("POST", $"{baseUrl}/api/pix/transactions")
                .WithHeader("Content-Type", "application/json")
                .WithHeader("Accept", "application/json")
                .WithBody(new StringContent(bodyJson, Encoding.UTF8, "application/json"));

            return await Http.Send(httpClient, request);
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(
                rate: pix.Copies,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSeconds)));
    }

    public static async Task<decimal?> GetRemainingLimitAsync(LoadTestSettings settings)
    {
        var pix = settings.PixConcurrent;
        var baseUrl = settings.BaseUrl.TrimEnd('/');

        using var client = new HttpClient();
        var response = await client.GetAsync($"{baseUrl}/api/account-limits/{pix.Agency}/{pix.Account}");
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (json.TryGetProperty("data", out var data) && data.TryGetProperty("pixLimit", out var limit))
            return limit.GetDecimal();

        return null;
    }
}
