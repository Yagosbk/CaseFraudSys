namespace CaseFraudSys.LoadTests;

public class LoadTestSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5067";
    public HealthSettings Health { get; set; } = new();
    public GetLimitSettings GetLimit { get; set; } = new();
    public PixConcurrentSettings PixConcurrent { get; set; } = new();
}

public class HealthSettings
{
    public int Rate { get; set; } = 50;
    public int DurationSeconds { get; set; } = 30;
}

public class GetLimitSettings
{
    public string Agency { get; set; } = "0001";
    public string Account { get; set; } = "12345";
    public int Rate { get; set; } = 100;
    public int DurationSeconds { get; set; } = 60;
}

public class PixConcurrentSettings
{
    public string Agency { get; set; } = "0001";
    public string Account { get; set; } = "99999";
    public string Document { get; set; } = "11122233344";
    public decimal InitialLimit { get; set; } = 100_000m;
    public int Copies { get; set; } = 50;
    public int Iterations { get; set; } = 200;
    public decimal Amount { get; set; } = 10m;
}
