using System.Net;
using System.Net.Http.Json;
using CaseFraudSys.Api.IntegrationTests.Fixtures;

namespace CaseFraudSys.Api.IntegrationTests;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class HealthEndpointTests
{
    private readonly HttpClient _client;

    public HealthEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<HealthData>>();
        Assert.True(body!.Success);
        Assert.Equal("healthy", body.Data!.Status);
        Assert.Equal("CaseFraudSys", body.Data.Service);
    }

    private sealed class ApiEnvelope<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }

    private sealed class HealthData
    {
        public string Status { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
    }
}
