using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CaseFraudSys.Api.IntegrationTests.Fixtures;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}

public static class ApiTestHelper
{
    public static async Task<(HttpResponseMessage Response, JsonElement Body)> SendJsonAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? payload = null)
    {
        HttpResponseMessage response;

        if (payload is null)
        {
            response = await client.SendAsync(new HttpRequestMessage(method, url));
        }
        else
        {
            response = method == HttpMethod.Post
                ? await client.PostAsJsonAsync(url, payload)
                : method == HttpMethod.Put
                    ? await client.PutAsJsonAsync(url, payload)
                    : throw new NotSupportedException($"Método {method} não suportado com payload.");
        }

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (response, body);
    }

    public static bool IsSuccess(JsonElement body) =>
        body.TryGetProperty("success", out var success) && success.GetBoolean();

    public static JsonElement GetData(JsonElement body) => body.GetProperty("data");
}

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<ApiWebApplicationFactory>;
