using System.Net;
using System.Net.Http.Json;
using CaseFraudSys.Api.IntegrationTests.Fixtures;

namespace CaseFraudSys.Api.IntegrationTests;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class AccountLimitsEndpointTests
{
    private readonly HttpClient _client;

    public AccountLimitsEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_Get_Update_Delete_Flow_Works()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var agency = "0001";
        var account = $"INT{suffix}";

        var (createResponse, createBody) = await ApiTestHelper.SendJsonAsync(
            _client,
            HttpMethod.Post,
            "/api/account-limits",
            new
            {
                document = "12345678901",
                agency,
                account,
                pixLimit = 5000m
            });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.True(ApiTestHelper.IsSuccess(createBody));

        var getResponse = await _client.GetAsync($"/api/account-limits/{agency}/{account}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<ApiEnvelope<AccountLimitData>>();
        Assert.True(getBody!.Success);
        Assert.Equal(5000m, getBody.Data!.PixLimit);

        var updateResponse = await _client.PutAsJsonAsync($"/api/account-limits/{agency}/{account}", new { pixLimit = 3000m });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/api/account-limits/{agency}/{account}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateAccount_ReturnsConflict()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var agency = "0001";
        var account = $"DUP{suffix}";
        var payload = new { document = "12345678901", agency, account, pixLimit = 1000m };

        await _client.PostAsJsonAsync("/api/account-limits", payload);
        var duplicateResponse = await _client.PostAsJsonAsync("/api/account-limits", payload);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Get_WhenAccountNotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/account-limits/0001/NOTFOUND999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidBody_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/account-limits", new
        {
            document = "",
            agency = "0001",
            account = "12345",
            pixLimit = 1000m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.False(body!.Success);
        Assert.Contains("obrigat", body.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_WithInvalidCpf_ReturnsBadRequest()
    {
        var (response, body) = await ApiTestHelper.SendJsonAsync(
            _client,
            HttpMethod.Post,
            "/api/account-limits",
            new
            {
                document = "123",
                agency = "0001",
                account = $"CPF{Guid.NewGuid():N}"[..8],
                pixLimit = 1000m
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(ApiTestHelper.IsSuccess(body));
    }

    [Fact]
    public async Task Update_WhenAccountNotFound_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/account-limits/0001/NOTFOUND999", new { pixLimit = 1000m });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenAccountNotFound_Returns404()
    {
        var response = await _client.DeleteAsync("/api/account-limits/0001/NOTFOUND999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class ErrorEnvelope
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    private sealed class ApiEnvelope<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }

    private sealed class AccountLimitData
    {
        public decimal PixLimit { get; set; }
    }
}
