namespace Hermes.ApiService;

public sealed class ChaosOptions
{
    public bool Enabled { get; init; } = true;

    public double DelayPercentage { get; init; } = 0.10; // 10%
    public int DelayMillisecondsMin { get; init; } = 200;
    public int DelayMillisecondsMax { get; init; } = 1500;

    public double FailurePercentage { get; init; } = 0.05; // 5%
    public int FailureStatusCode { get; init; } = 500;

    public string[] ExcludedPathPrefixes { get; init; } = new[] { "/health", "/alive", "/openapi" };
}
