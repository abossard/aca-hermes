using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hermes.ApiService;

public sealed class ChaosMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ChaosMiddleware> _logger;
    private readonly ChaosOptions _options;

    public ChaosMiddleware(RequestDelegate next, ILogger<ChaosMiddleware> logger, IOptions<ChaosOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled || IsExcludedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Failure branch
        var failureRoll = Random.Shared.NextDouble();
        if (_options.FailurePercentage > 0 && failureRoll < _options.FailurePercentage)
        {
            _logger.LogWarning("ChaosMiddleware: Injecting failure. path={Path} roll={Roll:F3} failurePct={Pct:P1} status={Status}",
                context.Request.Path,
                failureRoll,
                _options.FailurePercentage,
                _options.FailureStatusCode);

            context.Response.StatusCode = _options.FailureStatusCode;
            await context.Response.WriteAsync($"Chaos induced failure ({_options.FailureStatusCode}).");
            return;
        }

        // Delay branch
        var delayRoll = Random.Shared.NextDouble();
        if (_options.DelayPercentage > 0 && delayRoll < _options.DelayPercentage)
        {
            var min = Math.Max(0, _options.DelayMillisecondsMin);
            var max = Math.Max(min, _options.DelayMillisecondsMax);
            var delay = Random.Shared.Next(min, max + 1);

            _logger.LogInformation("ChaosMiddleware: Injecting delay. path={Path} roll={Roll:F3} delayPct={Pct:P1} delayMs={Delay}",
                context.Request.Path,
                delayRoll,
                _options.DelayPercentage,
                delay);

            try
            {
                await Task.Delay(delay, context.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                // Request aborted, just return.
                return;
            }
        }

        await _next(context);
    }

    private bool IsExcludedPath(PathString path)
    {
        if (_options.ExcludedPathPrefixes is null || _options.ExcludedPathPrefixes.Length == 0)
        {
            return false;
        }

        var value = path.Value ?? string.Empty;
        foreach (var prefix in _options.ExcludedPathPrefixes)
        {
            if (!string.IsNullOrWhiteSpace(prefix) && value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
