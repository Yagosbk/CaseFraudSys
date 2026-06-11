using System.Net;
using System.Net.Http.Json;
using CaseFraudSys.Api.IntegrationTests.Fixtures;

namespace CaseFraudSys.Api.IntegrationTests;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class PixTransactionsEndpointTests
{
    private readonly HttpClient _client;

    public PixTransactionsEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Pix_Approved_DebitLimit()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var agency = "0001";
        var account = $"PIX{suffix}";

        await _client.PostAsJsonAsync("/api/account-limits", new
        {
            document = "12345678901",
            agency,
            account,
            pixLimit = 1000m
        });

        var pixResponse = await _client.PostAsJsonAsync("/api/pix/transactions", new
        {
            transactionId = $"tx-{suffix}-1",
            agency,
            account,
            amount = 300m
        });

        Assert.Equal(HttpStatusCode.OK, pixResponse.StatusCode);
        var pixBody = await pixResponse.Content.ReadFromJsonAsync<ApiEnvelope<PixData>>();
        Assert.True(pixBody!.Data!.Approved);
        Assert.Equal(700m, pixBody.Data.RemainingLimit);

        var getResponse = await _client.GetAsync($"/api/account-limits/{agency}/{account}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<ApiEnvelope<AccountLimitData>>();
        Assert.Equal(700m, getBody!.Data!.PixLimit);
    }

    [Fact]
    public async Task Pix_Denied_DoesNotChangeLimit()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var agency = "0001";
        var account = $"PIX{suffix}";

        await _client.PostAsJsonAsync("/api/account-limits", new
        {
            document = "12345678901",
            agency,
            account,
            pixLimit = 500m
        });

        var pixResponse = await _client.PostAsJsonAsync("/api/pix/transactions", new
        {
            transactionId = $"tx-{suffix}-deny",
            agency,
            account,
            amount = 800m
        });

        var pixBody = await pixResponse.Content.ReadFromJsonAsync<ApiEnvelope<PixData>>();
        Assert.False(pixBody!.Data!.Approved);

        var getResponse = await _client.GetAsync($"/api/account-limits/{agency}/{account}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<ApiEnvelope<AccountLimitData>>();
        Assert.Equal(500m, getBody!.Data!.PixLimit);
    }

    [Fact]
    public async Task Pix_DuplicateTransactionId_IsIdempotent()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var agency = "0001";
        var account = $"IDP{suffix}";
        var transactionId = $"tx-idem-{suffix}";

        await _client.PostAsJsonAsync("/api/account-limits", new
        {
            document = "12345678901",
            agency,
            account,
            pixLimit = 1000m
        });

        var payload = new { transactionId, agency, account, amount = 200m };

        var first = await _client.PostAsJsonAsync("/api/pix/transactions", payload);
        var second = await _client.PostAsJsonAsync("/api/pix/transactions", payload);

        var firstBody = await first.Content.ReadFromJsonAsync<ApiEnvelope<PixData>>();
        var secondBody = await second.Content.ReadFromJsonAsync<ApiEnvelope<PixData>>();

        Assert.True(firstBody!.Data!.Approved);
        Assert.False(firstBody.Data.IsDuplicate);
        Assert.True(secondBody!.Data!.IsDuplicate);
        Assert.Equal(firstBody.Data.RemainingLimit, secondBody.Data.RemainingLimit);

        var getResponse = await _client.GetAsync($"/api/account-limits/{agency}/{account}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<ApiEnvelope<AccountLimitData>>();
        Assert.Equal(800m, getBody!.Data!.PixLimit);
    }

    [Fact]
    public async Task Pix_ConflictingTransactionIdParameters_Returns409()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var agency = "0001";
        var account = $"CNF{suffix}";
        var transactionId = $"tx-conflict-{suffix}";

        await _client.PostAsJsonAsync("/api/account-limits", new
        {
            document = "12345678901",
            agency,
            account,
            pixLimit = 1000m
        });

        await _client.PostAsJsonAsync("/api/pix/transactions", new
        {
            transactionId,
            agency,
            account,
            amount = 100m
        });

        var conflictResponse = await _client.PostAsJsonAsync("/api/pix/transactions", new
        {
            transactionId,
            agency,
            account,
            amount = 200m
        });

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
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

    private sealed class PixData
    {
        public bool Approved { get; set; }
        public bool IsDuplicate { get; set; }
        public decimal RemainingLimit { get; set; }
    }
}
