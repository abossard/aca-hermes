namespace Hermes.Web;

public class ApiTestClient
{
    private readonly HttpClient _directClient;
    private readonly HttpClient _proxyClient;

    public ApiTestClient(IHttpClientFactory httpClientFactory)
    {
        _directClient = httpClientFactory.CreateClient("ApiServiceDirect");
        _proxyClient = httpClientFactory.CreateClient("ProxyService");
    }

    public async Task<string> CallApiDirectlyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _directClient.GetAsync("/weatherforecast", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return $"Error calling API directly: {ex.Message}";
        }
    }

    public async Task<string> CallApiViaProxyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Use service discovery URL - proxy now has reference to apiservice
            var targetUrl = "https+http://apiservice/weatherforecast";
            var proxyUrl = $"/proxy?url={Uri.EscapeDataString(targetUrl)}";
            
            var response = await _proxyClient.GetAsync(proxyUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return $"Error calling API via proxy: {ex.Message}";
        }
    }

    public async Task<string> CallExternalApiViaProxyAsync(string targetUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var proxyUrl = $"/proxy?url={Uri.EscapeDataString(targetUrl)}";
            
            var response = await _proxyClient.GetAsync(proxyUrl, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Return content regardless of status code for testing purposes
            return content;
        }
        catch (Exception ex)
        {
            return $"Error calling external API via proxy: {ex.Message}";
        }
    }
}