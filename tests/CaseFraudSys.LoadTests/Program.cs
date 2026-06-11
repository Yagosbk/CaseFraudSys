using CaseFraudSys.LoadTests;
using CaseFraudSys.LoadTests.Scenarios;
using Microsoft.Extensions.Configuration;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http.CSharp;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var settings = configuration.GetSection("LoadTest").Get<LoadTestSettings>() ?? new LoadTestSettings();

var envBaseUrl = Environment.GetEnvironmentVariable("LOAD_TEST_BASE_URL");
if (!string.IsNullOrWhiteSpace(envBaseUrl))
    settings.BaseUrl = envBaseUrl;

Console.WriteLine($"Base URL: {settings.BaseUrl}");
Console.WriteLine("Certifique-se de que a API e o DynamoDB estão em execução.");
Console.WriteLine();

await PixConcurrentScenario.SetupAccountAsync(settings);

var httpClient = Http.CreateDefaultClient();

var scenarios = new[]
{
    HealthLoadScenario.Create(httpClient, settings),
    GetAccountLimitScenario.Create(httpClient, settings),
    PixConcurrentScenario.Create(httpClient, settings)
};

NBomberRunner
    .RegisterScenarios(scenarios)
    .WithReportFormats(ReportFormat.Csv, ReportFormat.Md, ReportFormat.Txt)
    .Run();

var remainingLimit = await PixConcurrentScenario.GetRemainingLimitAsync(settings);
var expectedLimit = settings.PixConcurrent.InitialLimit - (settings.PixConcurrent.Iterations * settings.PixConcurrent.Amount);

Console.WriteLine();
Console.WriteLine("=== Validação PIX concorrente ===");
Console.WriteLine($"Limite inicial:     {settings.PixConcurrent.InitialLimit}");
Console.WriteLine($"Transações:         {settings.PixConcurrent.Iterations} x {settings.PixConcurrent.Amount}");
Console.WriteLine($"Limite esperado:    {expectedLimit}");
Console.WriteLine($"Limite atual (GET): {remainingLimit?.ToString() ?? "não consultado"}");

if (remainingLimit.HasValue && remainingLimit.Value == expectedLimit)
    Console.WriteLine("Consistência do débito atômico: OK");
else if (remainingLimit.HasValue)
    Console.WriteLine("Consistência do débito atômico: verificar (pode haver negações se limite insuficiente)");

Console.WriteLine();
Console.WriteLine("Relatórios (txt/csv/md) em ./reports/");
